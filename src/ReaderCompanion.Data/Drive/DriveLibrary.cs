using Microsoft.EntityFrameworkCore;
using ReaderCompanion.Core.Drive;
using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Metadata;
using ReaderCompanion.Core.Preferences;
using ReaderCompanion.Core.Scanning;

namespace ReaderCompanion.Data;

/// <summary>The library's Drive side. A Drive scan works like a local one: the same reconciler and catalog writer,
/// with Drive's checksums for content and downloads only for files that must be read.</summary>
internal sealed class DriveLibrary(
    IDbContextFactory<LibraryDbContext> contexts,
    LibraryDataPaths paths,
    LibraryWriteGate gate,
    IDriveAccount account,
    GoogleDriveFiles files,
    IAppPreferences preferences) : IDriveLibrary
{
    /// <summary>Books read at once. Each takes 2-4 range requests of ~0.2-0.5 s, so 10 slots make roughly 1,500-3,000
    /// requests a minute: well within Google's 12,000 per user, with room for the user's other Drive use.</summary>
    private const int ReadsAtOnce = 10;

    /// <summary>Books read but not yet written, at most. A slow book then holds back only its own slot while up to 10x
    /// the slots' worth of finished books wait; their covers (typically under 300 KB) stay within ~30 MB.</summary>
    private const int ReadAhead = 100;

    /// <summary>Drive folders listed at once. A listing takes ~0.2-0.3 s, so 10 make about 2,000-3,000 requests a
    /// minute: well within Google's 12,000 per user.</summary>
    private const int ListingsAtOnce = 10;

    public async Task<IReadOnlyList<DriveSource>> ListSourcesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        return await db.DriveSources.AsNoTracking().OrderBy(s => s.Kind).ThenBy(s => s.Name).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DriveFolder>> ListFoldersAsync(string? parentId, CancellationToken cancellationToken = default) =>
        [.. (await files.ListChildrenAsync(parentId ?? "root", foldersOnly: true, cancellationToken)).Select(f => new DriveFolder(f.Id, f.Name))];

    public async Task<LinkFolderResult> AddFolderAsync(string folderId, string path, CancellationToken cancellationToken = default)
    {
        var name = path.Trim('/');
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var folders = await db.DriveSources.Where(s => s.Kind == DriveSourceKind.Folder).ToListAsync(cancellationToken);
        if (folders.Any(f => f.DriveId == folderId))
        {
            return new LinkFolderResult(false, "This Drive folder is already added.");
        }

        if (folders.FirstOrDefault(f => DrivePath.IsSameOrUnder(name, f.Name)) is { } covering)
        {
            return new LinkFolderResult(false, $"Already covered by the Drive folder {covering.Name}.");
        }

        var replaced = folders.Where(f => DrivePath.IsSameOrUnder(f.Name, name)).ToList();
        db.DriveSources.RemoveRange(replaced);
        db.DriveSources.Add(new DriveSource { DriveId = folderId, Name = name, Kind = DriveSourceKind.Folder, AddedUtc = DateTime.UtcNow });
        await db.SaveChangesAsync(cancellationToken);
        return new LinkFolderResult(true) { Replaced = [.. replaced.Select(f => f.Name)] };
    }

    public async Task<int> AddPickedFilesAsync(CancellationToken cancellationToken = default)
    {
        var picked = await account.PickFilesAsync(cancellationToken);
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var known = (await db.DriveSources.Select(s => s.DriveId).ToListAsync(cancellationToken)).ToHashSet();
        var added = 0;
        foreach (var id in picked.Where(known.Add))
        {
            var name = (await files.GetAsync(id, cancellationToken))?.Name ?? id;
            db.DriveSources.Add(new DriveSource { DriveId = id, Name = name, Kind = DriveSourceKind.File, AddedUtc = DateTime.UtcNow });
            added++;
        }

        await db.SaveChangesAsync(cancellationToken);
        return added;
    }

    public async Task RemoveSourceAsync(int sourceId, CancellationToken cancellationToken = default)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        if (await db.DriveSources.FindAsync([sourceId], cancellationToken) is not { } source)
        {
            return;
        }

        var others = await db.DriveSources.Where(s => s.Id != source.Id).ToListAsync(cancellationToken);
        var present = await db.BookFiles.Where(f => f.DriveFileId != null && f.Status == FileStatus.Present).ToListAsync(cancellationToken);
        foreach (var file in present.Where(f => Covers(source, f) && !others.Any(o => Covers(o, f))))
        {
            file.Status = FileStatus.Missing;
        }

        db.DriveSources.Remove(source);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ScanSummary> ScanAsync(IProgress<ScanProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        if (!gate.TryEnter(LibraryWork.Scan))
        {
            throw new InvalidOperationException(gate.BusyMessage);
        }

        var temp = Directory.CreateTempSubdirectory("rc-drive-");
        try
        {
            // Off the caller's thread: the catalog is written synchronously, like a local scan.
            var summary = await Task.Run(() => ScanCoreAsync(temp.FullName, progress, cancellationToken), cancellationToken);
            preferences.Save(preferences.Current with { DriveLastScanUtc = DateTime.UtcNow });
            return summary;
        }
        finally
        {
            gate.Exit();
            temp.Delete(recursive: true);
        }
    }

    public async Task<string> DownloadAsync(int bookFileId, IProgress<int>? percent = null, CancellationToken cancellationToken = default)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var file = await db.BookFiles.AsNoTracking().FirstOrDefaultAsync(f => f.Id == bookFileId, cancellationToken);
        if (file?.DriveFileId is not { } driveId)
        {
            throw new InvalidOperationException("This file is not on Google Drive.");
        }

        if (!driveId.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
        {
            throw new DriveUnavailableException("This Drive file has an id the app does not accept."); // it names a local file
        }

        // The cached copy is named after the file's current version on Drive, so an edit there is never missed.
        var item = await files.GetAsync(driveId, cancellationToken)
            ?? throw new DriveUnavailableException($"{Path.GetFileName(DrivePath.Display(file.Path))} is no longer on Google Drive.");
        var version = item.Sha256 is { Length: >= 16 } sha ? sha[..16] : $"{item.ModifiedUtc.Ticks:x}-{item.Size:x}";
        var cache = Path.Combine(paths.DataFolder, "drive-cache");
        // The extension comes from the book's known format, never from the name on Drive (which could have been
        // renamed to something the system would run).
        var extension = file.Format == BookFormat.Pdf ? ".pdf" : ".epub";
        var local = Path.Combine(cache, $"{driveId}-{version}{extension}");
        if (File.Exists(local))
        {
            return local;
        }

        Directory.CreateDirectory(cache);
        foreach (var old in Directory.EnumerateFiles(cache, driveId + "-*"))
        {
            TryDelete(old); // earlier versions of this file
        }

        await DownloadToAsync(driveId, local, cancellationToken, percent is null ? null : new Percent(item.Size, percent));
        percent?.Report(100);
        return local;
    }

    private async Task<ScanSummary> ScanCoreAsync(string temp, IProgress<ScanProgress>? progress, CancellationToken cancellationToken)
    {
        List<DriveSource> sources;
        List<KnownFile> known, local;
        await using (var db = await contexts.CreateDbContextAsync(cancellationToken))
        {
            sources = await db.DriveSources.AsNoTracking().ToListAsync(cancellationToken);
            (local, known) = KnownFiles.Load(db);
        }

        // Everything is listed before anything changes: a source that cannot be read stops the scan here.
        var listed = await ListAsync(sources, progress, cancellationToken);
        var knownByPath = known.ToDictionary(k => k.Path, StringComparer.Ordinal);
        var found = new List<FoundFile>(listed.Count);
        var filesRead = 0;
        string? KnownHash(DriveItem item, string path) =>
            item.Sha256 ?? (knownByPath.TryGetValue(path, out var k) && ScanReconciler.IsUnchanged(k, item.Size, item.ModifiedUtc) ? k.ContentHash : null);
        var toCheck = listed.Values.Count(l => KnownHash(l.Item, DrivePath.Make(l.Item.Id, l.Display)) is null);
        var checkedFiles = 0;
        foreach (var (item, display) in listed.Values)
        {
            var path = DrivePath.Make(item.Id, display);
            var hash = KnownHash(item, path);
            if (hash is null)
            {
                progress?.Report(new ScanProgress(checkedFiles++, toCheck) { Stage = ScanStage.Checking });
                // No checksum from Drive: download to hash, and delete at once (it is downloaded again if it is read).
                var copy = await DownloadToTempAsync(item, temp, cancellationToken);
                await using (var stream = File.OpenRead(copy))
                {
                    hash = ContentHash.Of(stream);
                }

                File.Delete(copy);
                filesRead++;
            }

            found.Add(new FoundFile(path, item.Size, item.ModifiedUtc, hash, item.Id));
        }

        if (toCheck > 0)
        {
            progress?.Report(new ScanProgress(toCheck, toCheck) { Stage = ScanStage.Checking });
        }

        var plan = ScanReconciler.Plan(known, found, [], local);
        var toRead = plan.Actions.Count(a => CatalogWriter.FileToRead(a) is not null);
        IReadOnlyList<UnreadableFile> unreadable = [];
        if (plan.Actions.Any(a => a is not KeepFile))
        {
            // Books are read in place (only the parts their details live in), up to ReadsAtOnce at a time: each slot takes
            // the next book as soon as it is free. The catalog is changed in the plan's order as reads complete, with at
            // most ReadAhead books read and waiting, so their covers stay few.
            await using var db = await contexts.CreateDbContextAsync(cancellationToken);
            var readAhead = new Dictionary<string, MetadataResult>(StringComparer.Ordinal);
            var writer = new CatalogWriter(db, paths.CoversFolder, DateTime.UtcNow, readAhead: readAhead);
            using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            using var slots = new SemaphoreSlim(ReadsAtOnce);
            var waiting = new Queue<(ScanAction Action, FoundFile? File, Task<MetadataResult>? Read)>();
            var read = 0;

            async Task<MetadataResult> ReadInSlotAsync(FoundFile file)
            {
                try
                {
                    var result = await ReadInPlaceAsync(listed[file.DriveFileId!].Item, file.Path, temp, stop.Token);
                    progress?.Report(new ScanProgress(Interlocked.Increment(ref read), toRead));
                    return result;
                }
                finally
                {
                    slots.Release();
                }
            }

            async Task WriteNextAsync()
            {
                var (action, file, reading) = waiting.Dequeue();
                if (file is not null)
                {
                    readAhead[file.Path] = await reading!;
                }

                writer.Apply(action);
                if (file is not null)
                {
                    readAhead.Remove(file.Path);
                }
            }

            try
            {
                foreach (var action in plan.Actions)
                {
                    var file = CatalogWriter.FileToRead(action);
                    Task<MetadataResult>? reading = null;
                    if (file is not null)
                    {
                        await slots.WaitAsync(stop.Token);
                        reading = ReadInSlotAsync(file);
                    }

                    waiting.Enqueue((action, file, reading));
                    while (waiting.TryPeek(out var next) && (next.Read is null || next.Read.IsCompleted || waiting.Count > ReadAhead))
                    {
                        await WriteNextAsync();
                    }
                }

                while (waiting.Count > 0)
                {
                    await WriteNextAsync();
                }
            }
            catch
            {
                await stop.CancelAsync(); // the other reads are not needed any more
                throw;
            }

            filesRead += read;
            await db.SaveChangesAsync(cancellationToken);
            unreadable = writer.Unreadable;
            progress?.Report(new ScanProgress(toRead, toRead));
        }

        int Count<T>() => plan.Actions.OfType<T>().Count();
        return new ScanSummary
        {
            Added = Count<AddBook>() + Count<SplitToNewBook>(),
            Moved = Count<MoveFile>(),
            Updated = Count<UpdateContent>() + Count<ReassignFile>(),
            MarkedMissing = Count<MarkMissing>(),
            FilesRead = filesRead,
            Unreadable = unreadable,
        };
    }

    /// <summary>Every supported file in the sources, once, by Drive id, with its display path.</summary>
    private async Task<Dictionary<string, (DriveItem Item, string Display)>> ListAsync(
        List<DriveSource> sources, IProgress<ScanProgress>? progress, CancellationToken cancellationToken)
    {
        var listed = new Dictionary<string, (DriveItem, string)>(StringComparer.Ordinal);
        var folders = 0;
        void Listed(bool folder)
        {
            folders += folder ? 1 : 0;
            progress?.Report(new ScanProgress(0, listed.Count) { Stage = ScanStage.Listing, FoldersListed = folders });
        }

        foreach (var source in sources)
        {
            try
            {
                if (source.Kind == DriveSourceKind.File)
                {
                    await ListPickedFileAsync(source, listed, cancellationToken);
                    Listed(folder: false);
                }
                else
                {
                    await ListFolderAsync(source, listed, () => Listed(folder: true), cancellationToken);
                }
            }
            catch (DriveUnavailableException e)
            {
                throw new DriveUnavailableException($"“{source.Name}” could not be read on Google Drive, so nothing was changed. {e.Message}", e);
            }
        }

        return listed;
    }

    // A picked file gone from Drive is simply not found (and so marked missing).
    private async Task ListPickedFileAsync(
        DriveSource source, Dictionary<string, (DriveItem, string)> listed, CancellationToken cancellationToken)
    {
        if (await files.GetAsync(source.DriveId, cancellationToken) is { IsFolder: false } item && BookMetadataReader.IsSupported(item.Name))
        {
            listed.TryAdd(item.Id, (item, item.Name));
        }
    }

    /// <summary>Lists a folder tree, up to <see cref="ListingsAtOnce"/> folders at a time: each free slot takes the
    /// next waiting folder, so a deep tree keeps them all busy. Results are merged here, one folder at a time.</summary>
    private async Task ListFolderAsync(
        DriveSource source, Dictionary<string, (DriveItem, string)> listed, Action folderListed, CancellationToken cancellationToken)
    {
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var waiting = new Queue<(string Id, string Display)>([(source.DriveId, source.Name)]);
        var running = new List<Task<((string Id, string Display) Folder, IReadOnlyList<DriveItem> Children)>>();
        try
        {
            while (waiting.Count > 0 || running.Count > 0)
            {
                while (waiting.Count > 0 && running.Count < ListingsAtOnce)
                {
                    running.Add(ListOneAsync(waiting.Dequeue(), stop.Token));
                }

                var done = await Task.WhenAny(running);
                running.Remove(done);
                var (folder, children) = await done;
                foreach (var child in children)
                {
                    var display = $"{folder.Display}/{child.Name}";
                    if (child.IsFolder)
                    {
                        waiting.Enqueue((child.Id, display));
                    }
                    else if (BookMetadataReader.IsSupported(child.Name)
                        && (!listed.TryGetValue(child.Id, out var seen) || string.CompareOrdinal(display, seen.Item2) < 0))
                    {
                        listed[child.Id] = (child, display); // a file in two folders: the same path whatever the timing
                    }
                }

                folderListed();
            }
        }
        catch
        {
            await stop.CancelAsync(); // the others are not needed any more
            throw;
        }
    }

    private async Task<((string Id, string Display) Folder, IReadOnlyList<DriveItem> Children)> ListOneAsync(
        (string Id, string Display) folder, CancellationToken cancellationToken) =>
        (folder, await files.ListChildrenAsync(folder.Id, foldersOnly: false, cancellationToken));

    private static bool Covers(DriveSource source, BookFile file) => source.Kind == DriveSourceKind.File
        ? file.DriveFileId == source.DriveId
        : DrivePath.IsUnder(file.Path, source.Name);

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Still open in a reader app: left for next time.
        }
    }

    /// <summary>Reads a book's details through the bytes it needs, not a full download; a PDF this cannot follow is
    /// downloaded whole and read as before. A failed network stops the scan (a reader would otherwise report the book
    /// as broken).</summary>
    private Task<MetadataResult> ReadInPlaceAsync(DriveItem item, string path, string temp, CancellationToken cancellationToken) =>
        Task.Run(
            async () =>
            {
                MetadataResult? result;
                using (var stream = new DriveRangeStream(files, item.Id, item.Size, cancellationToken))
                {
                    try
                    {
                        // An EPUB is read from its ZIP directory at the end, then its package file near the start: one
                        // round trip for both.
                        await stream.PrefetchAsync(start: BookMetadataReader.FormatOf(path) == BookFormat.Epub, end: true);
                        result = BookMetadataReader.ReadInPlace(stream, path);
                    }
                    catch (IOException) when (stream.Failure is not null)
                    {
                        result = null;
                    }

                    if (stream.Failure is { } failure)
                    {
                        throw failure as DriveUnavailableException
                            ?? new DriveUnavailableException($"{item.Name} could not be read from Google Drive.", failure);
                    }
                }

                if (result is not null)
                {
                    return result;
                }

                var copy = await DownloadToTempAsync(item, temp, cancellationToken);
                try
                {
                    await using var whole = File.OpenRead(copy);
                    return BookMetadataReader.Read(whole, path);
                }
                finally
                {
                    File.Delete(copy);
                }
            },
            cancellationToken);

    private async Task<string> DownloadToTempAsync(DriveItem item, string temp, CancellationToken cancellationToken)
    {
        var local = Path.Combine(temp, item.Id + ".download"); // read by the app only, never opened by the system
        await DownloadToAsync(item.Id, local, cancellationToken);
        return local;
    }

    /// <summary>Downloads beside <paramref name="local"/> and moves it in place, so a broken download never looks
    /// complete.</summary>
    private async Task DownloadToAsync(string driveId, string local, CancellationToken cancellationToken, Percent? percent = null)
    {
        var partial = $"{local}.{Guid.NewGuid():N}.partial"; // two opens of one file never share a partial
        try
        {
            await using (var file = File.Create(partial))
            await using (Stream stream = percent is null ? file : new CountingStream(file, percent.Written))
            {
                await files.DownloadAsync(driveId, stream, cancellationToken);
            }

            File.Move(partial, local, overwrite: true);
        }
        finally
        {
            File.Delete(partial);
        }
    }

    /// <summary>Turns bytes written into a percentage of <paramref name="size"/>, reported only when it changes.</summary>
    private sealed class Percent(long size, IProgress<int> report)
    {
        private int _last = -1;

        public void Written(long bytes)
        {
            var percent = size <= 0 ? 0 : (int)Math.Min(99, bytes * 100 / size); // 100 once the file is in place
            if (percent != _last)
            {
                _last = percent;
                report.Report(percent);
            }
        }
    }

    /// <summary>Passes writes through and says how many bytes have been written so far.</summary>
    private sealed class CountingStream(Stream inner, Action<long> written) : Stream
    {
        private long _total;

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => inner.Length;

        public override long Position
        {
            get => inner.Position;
            set => throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            inner.Write(buffer, offset, count);
            written(_total += count);
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await inner.WriteAsync(buffer, cancellationToken);
            written(_total += buffer.Length);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override void Flush() => inner.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken) => inner.FlushAsync(cancellationToken);

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
