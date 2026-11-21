namespace ReaderCompanion.Core.Scanning;

/// <summary>What a scan is doing. Local scans only read; a Drive scan first looks through Drive, then checks files Drive
/// has no checksum for.</summary>
public enum ScanStage
{
    Reading,
    Listing,
    Checking,
}

/// <summary>How far a scan has got: <see cref="FilesProcessed"/> of <see cref="FilesFound"/> in the current stage
/// (while listing, the files found so far).</summary>
public sealed record ScanProgress(int FilesProcessed, int FilesFound)
{
    public ScanStage Stage { get; init; } = ScanStage.Reading;

    /// <summary>Drive folders looked through so far (while listing).</summary>
    public int FoldersListed { get; init; }
}

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
