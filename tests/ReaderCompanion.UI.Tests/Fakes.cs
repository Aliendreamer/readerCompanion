using ReaderCompanion.Core.Scanning;

namespace ReaderCompanion.UI.Tests;

internal sealed class FakePicker(params string?[] answers) : IFolderPicker
{
    private readonly Queue<string?> _answers = new(answers);

    public int Calls { get; private set; }

    public Task<string?> PickFolderAsync()
    {
        Calls++;
        return Task.FromResult(_answers.Count > 0 ? _answers.Dequeue() : null);
    }
}

internal sealed class FakeFolders : ILibraryFolders
{
    public List<string> Linked { get; } = [];

    public LinkFolderResult? NextResult { get; set; }

    public int LinkCalls { get; private set; }

    public Task<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>([.. Linked]);

    public Task<LinkFolderResult> LinkAsync(string path, CancellationToken cancellationToken = default)
    {
        LinkCalls++;
        var result = NextResult ?? new LinkFolderResult(true);
        if (result.Linked)
        {
            Linked.Add(path);
        }

        return Task.FromResult(result);
    }

    public Task UnlinkAsync(string path, CancellationToken cancellationToken = default)
    {
        Linked.Remove(path);
        return Task.CompletedTask;
    }
}

/// <summary>A scanner the test drives by hand: report progress, then finish.</summary>
internal sealed class FakeScanner : ILibraryScanner
{
    private TaskCompletionSource<ScanSummary>? _running;
    private IProgress<ScanProgress>? _progress;

    public bool IsScanning => _running is not null;

    public Task<ScanSummary> ScanAsync(IProgress<ScanProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        _progress = progress;
        _running = new TaskCompletionSource<ScanSummary>();
        return _running.Task;
    }

    public void Report(int processed, int found) => _progress!.Report(new ScanProgress(processed, found));

    public void Finish(ScanSummary summary)
    {
        var running = _running!;
        _running = null;
        running.SetResult(summary);
    }
}
