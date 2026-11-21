using ReaderCompanion.Core.Scanning;

namespace ReaderCompanion.Core.Drive;

public enum DriveSourceKind
{
    /// <summary>A Drive folder, with its subfolders (whole-Drive access).</summary>
    Folder,

    /// <summary>One file picked in Google's picker.</summary>
    File,
}

/// <summary>A place on Drive the library takes books from.</summary>
public sealed class DriveSource
{
    public int Id { get; set; }

    public string DriveId { get; set; } = "";

    /// <summary>A folder's path from My Drive ("Books/Fantasy"), or a picked file's name.</summary>
    public string Name { get; set; } = "";

    public DriveSourceKind Kind { get; set; }

    public DateTime AddedUtc { get; set; }
}

/// <summary>A folder on Drive, while browsing for one to add.</summary>
public sealed record DriveFolder(string Id, string Name);

/// <summary>The library's Drive side: where books come from on Drive, scanning them, and opening them.</summary>
public interface IDriveLibrary
{
    Task<IReadOnlyList<DriveSource>> ListSourcesAsync(CancellationToken cancellationToken = default);

    /// <summary>The folders inside <paramref name="parentId"/>, or inside My Drive when null, by name.</summary>
    Task<IReadOnlyList<DriveFolder>> ListFoldersAsync(string? parentId, CancellationToken cancellationToken = default);

    /// <summary>Adds a Drive folder by id, known to the user as <paramref name="path"/> ("Books/Fantasy").</summary>
    Task<LinkFolderResult> AddFolderAsync(string folderId, string path, CancellationToken cancellationToken = default);

    /// <summary>Opens Google's picker and adds the files picked; returns how many were new.</summary>
    Task<int> AddPickedFilesAsync(CancellationToken cancellationToken = default);

    /// <summary>Removes the source; its books stay, with their Drive files marked missing.</summary>
    Task RemoveSourceAsync(int sourceId, CancellationToken cancellationToken = default);

    /// <summary>Brings the library in line with the sources. Throws <see cref="InvalidOperationException"/> while
    /// another scan or file change runs, and <see cref="DriveUnavailableException"/> when a source cannot be read
    /// (nothing is changed then).</summary>
    Task<ScanSummary> ScanAsync(IProgress<ScanProgress>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>A local copy of the Drive file to open, downloaded unless the cached copy is current.</summary>
    Task<string> DownloadAsync(int bookFileId, CancellationToken cancellationToken = default);
}

/// <summary>Drive could not be read; the message says what, for the user.</summary>
public sealed class DriveUnavailableException(string message, Exception? inner = null) : Exception(message, inner);
