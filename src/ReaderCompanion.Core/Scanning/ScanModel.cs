using ReaderCompanion.Core.Library;

namespace ReaderCompanion.Core.Scanning;

/// <summary>A file location the library already knows about.</summary>
/// <summary>A file the library has. <see cref="Unreadable"/>: its details could not be read last time, so the next
/// scan reads it again.</summary>
public sealed record KnownFile(int FileId, int BookId, string Path, long SizeBytes, DateTime LastWriteUtc, string ContentHash, FileStatus Status)
{
    public bool Unreadable { get; init; }
}

/// <summary>A supported file found during this scan, on disk or (with <paramref name="DriveFileId"/>) on Drive.</summary>
public sealed record FoundFile(string Path, long SizeBytes, DateTime LastWriteUtc, string ContentHash, string? DriveFileId = null);

/// <summary>What a scan must do to bring the library in line with the disk.</summary>
public abstract record ScanAction;

/// <summary>Present at the same path with the same content; nothing to change except "last seen".</summary>
public sealed record KeepFile(KnownFile File) : ScanAction;

/// <summary>Was missing, found again at the same path with the same content.</summary>
public sealed record Reappear(KnownFile File) : ScanAction;

/// <summary>Same content at a new path (moved, renamed, or a missing file found elsewhere).</summary>
public sealed record MoveFile(KnownFile File, FoundFile To) : ScanAction;

/// <summary>Same path, new content, and no other copy of the book remains: re-read and update the book.</summary>
public sealed record UpdateContent(KnownFile File, FoundFile Now) : ScanAction;

/// <summary>Same path, new content, while other copies of the old content remain: the file becomes a new book.</summary>
public sealed record SplitToNewBook(KnownFile File, FoundFile Now) : ScanAction;

/// <summary>Same path, new content identical to another book's: the file now belongs to that book.</summary>
public sealed record ReassignFile(KnownFile File, int BookId, FoundFile Now) : ScanAction;

/// <summary>A new path whose content belongs to an existing book (an extra copy).</summary>
public sealed record JoinBook(int BookId, FoundFile File) : ScanAction;

/// <summary>New content: one book with one or more identical files.</summary>
public sealed record AddBook(IReadOnlyList<FoundFile> Files) : ScanAction;

/// <summary>A present file that is no longer on disk in a reachable folder.</summary>
public sealed record MarkMissing(KnownFile File) : ScanAction;

public sealed record ScanPlan(IReadOnlyList<ScanAction> Actions);
