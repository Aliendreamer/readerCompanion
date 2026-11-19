namespace ReaderCompanion.Core.Scanning;

public sealed record ScanProgress(int FilesProcessed, int FilesFound);

public sealed record UnreadableFile(string Path, string Reason);

public sealed record ScanSummary
{
    public int Added { get; init; }

    public int Moved { get; init; }

    public int Updated { get; init; }

    public int MarkedMissing { get; init; }

    /// <summary>Files whose content was read this scan (new or changed files); unchanged files are not read.</summary>
    public int FilesRead { get; init; }

    public IReadOnlyList<UnreadableFile> Unreadable { get; init; } = [];

    public IReadOnlyList<string> UnavailableFolders { get; init; } = [];
}

public interface ILibraryScanner
{
    bool IsScanning { get; }

    /// <summary>Scans every library folder. Throws <see cref="InvalidOperationException"/> if a scan is already running.</summary>
    Task<ScanSummary> ScanAsync(IProgress<ScanProgress>? progress = null, CancellationToken cancellationToken = default);
}
