using ReaderCompanion.Core.Scanning;

namespace ReaderCompanion.UI;

/// <summary>The app's scans, kept beyond any page: leaving Sources and coming back shows a running scan's progress,
/// or the last one's summary or failure.</summary>
public sealed class ScanStatus
{
    public TrackedScan Library { get; } = new();

    public TrackedScan Drive { get; } = new();
}

/// <summary>One kind of scan: runs one at a time and remembers how it is going and how it ended.</summary>
public sealed class TrackedScan
{
    public bool IsRunning { get; private set; }

    public ScanProgress? Progress { get; private set; }

    public ScanSummary? Summary { get; private set; }

    public Exception? Failure { get; private set; }

    /// <summary>Raised on any thread when the scan starts, moves on or ends; pages re-render through their dispatcher.</summary>
    public event Action? Changed;

    /// <summary>Runs <paramref name="scan"/> unless one is running already; its failure is kept, not thrown.</summary>
    public async Task RunAsync(Func<IProgress<ScanProgress>, Task<ScanSummary>> scan)
    {
        if (IsRunning)
        {
            return;
        }

        IsRunning = true;
        Progress = null;
        Summary = null;
        Failure = null;
        Changed?.Invoke();
        try
        {
            Summary = await scan(new Reporter(this));
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            Failure = e;
        }
        finally
        {
            IsRunning = false;
            Progress = null;
            Changed?.Invoke();
        }
    }

    /// <summary>Forgets the last scan's summary and failure (e.g. after unlinking what was scanned).</summary>
    public void Clear()
    {
        if (!IsRunning)
        {
            Summary = null;
            Failure = null;
            Changed?.Invoke();
        }
    }

    private sealed class Reporter(TrackedScan scan) : IProgress<ScanProgress>
    {
        public void Report(ScanProgress value)
        {
            if (scan.IsRunning)
            {
                scan.Progress = value;
                scan.Changed?.Invoke();
            }
        }
    }
}
