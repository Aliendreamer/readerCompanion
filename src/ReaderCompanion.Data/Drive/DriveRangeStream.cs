namespace ReaderCompanion.Data;

/// <summary>A Drive file read in place: seekable, fetching only the pieces that are read (in
/// <c>chunkSize</c> byte ranges, the last few kept), so a book's details can be read without downloading it whole.
/// Reads block on the network: use it off the UI thread.</summary>
internal sealed class DriveRangeStream(
    GoogleDriveFiles files, string id, long length, CancellationToken cancellationToken, int chunkSize = DriveRangeStream.DefaultChunkSize)
    : Stream
{
    /// <summary>64 KB holds an EPUB's ZIP directory or package file, or a PDF's trailer and cross-reference table,
    /// in one request for most books; much larger would fetch bytes no reader looks at.</summary>
    public const int DefaultChunkSize = 64 * 1024;

    /// <summary>Pieces kept (1 MB): enough for a reader that goes back to a part it read, as ZIP and PDF readers do.</summary>
    private const int KeptChunks = 16;

    private readonly Dictionary<long, byte[]> _chunks = [];
    private readonly Queue<long> _kept = new();
    private long _position;

    /// <summary>Why a piece could not be fetched. Readers may swallow the <see cref="IOException"/> the stream
    /// throws; this tells a failed network apart from a broken book.</summary>
    public Exception? Failure { get; private set; }

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length => length;

    public override long Position
    {
        get => _position;
        set => _position = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
    }

    /// <summary>Fetches the first and/or last piece at the same time, before a reader asks for them one by one.</summary>
    public async Task PrefetchAsync(bool start, bool end)
    {
        var last = length <= 0 ? 0 : (length - 1) / chunkSize;
        var indexes = new[] { start ? 0 : -1, end ? last : -1 }.Where(i => i >= 0).Distinct().Where(i => !_chunks.ContainsKey(i)).ToList();
        try
        {
            var pieces = await Task.WhenAll(indexes.Select(i =>
                files.DownloadRangeAsync(id, i * chunkSize, Math.Min(length, (i + 1) * chunkSize) - 1, cancellationToken)));
            for (var i = 0; i < indexes.Count; i++)
            {
                Keep(indexes[i], pieces[i]);
            }
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            Failure ??= e;
            throw new IOException(e.Message, e);
        }
    }

    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> buffer)
    {
        if (_position >= length || buffer.IsEmpty)
        {
            return 0;
        }

        var index = _position / chunkSize;
        var chunk = Chunk(index);
        var start = (int)(_position - (index * chunkSize));
        var count = Math.Min(buffer.Length, chunk.Length - start);
        if (count <= 0)
        {
            return 0;
        }

        chunk.AsSpan(start, count).CopyTo(buffer);
        _position += count;
        return count;
    }

    public override long Seek(long offset, SeekOrigin origin) => Position = origin switch
    {
        SeekOrigin.Begin => offset,
        SeekOrigin.Current => _position + offset,
        _ => length + offset,
    };

    public override void Flush()
    {
    }

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    private byte[] Chunk(long index)
    {
        if (_chunks.TryGetValue(index, out var chunk))
        {
            return chunk;
        }

        var from = index * chunkSize;
        try
        {
            chunk = files.DownloadRangeAsync(id, from, Math.Min(length, from + chunkSize) - 1, cancellationToken).GetAwaiter().GetResult();
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            Failure ??= e;
            throw new IOException(e.Message, e);
        }

        Keep(index, chunk);
        return chunk;
    }

    private void Keep(long index, byte[] chunk)
    {
        if (_kept.Count == KeptChunks)
        {
            _chunks.Remove(_kept.Dequeue());
        }

        _chunks[index] = chunk;
        _kept.Enqueue(index);
    }
}
