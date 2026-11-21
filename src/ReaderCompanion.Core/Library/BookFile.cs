namespace ReaderCompanion.Core.Library;

public enum BookFormat
{
    Epub,
    Pdf,
}

public enum FileStatus
{
    Present,
    Missing,
}

/// <summary>One location of a book's file on disk.</summary>
public sealed class BookFile
{
    public int Id { get; set; }

    public int BookId { get; set; }

    public Book Book { get; set; } = null!;

    /// <summary>A local path, or for a Drive file a <see cref="Drive.DrivePath"/>.</summary>
    public string Path { get; set; } = "";

    /// <summary>The Google Drive file id, for a file on Drive; null for a local file.</summary>
    public string? DriveFileId { get; set; }

    public BookFormat Format { get; set; }

    public long SizeBytes { get; set; }

    public DateTime LastWriteUtc { get; set; }

    /// <summary>SHA-256 of the file content, lower-case hex.</summary>
    public string ContentHash { get; set; } = "";

    public FileStatus Status { get; set; }

    /// <summary>True when the file could not be read (damaged, encrypted).</summary>
    public bool Unreadable { get; set; }

    public DateTime LastSeenUtc { get; set; }
}
