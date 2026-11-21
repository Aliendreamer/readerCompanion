namespace ReaderCompanion.Core.Library;

/// <summary>A book as shown in a list. <see cref="FileMissing"/> is true when it has files and none is present (a
/// wanted book has none);
/// <see cref="CoverFileName"/> names its cover in the library's covers folder, if it has one.</summary>
public sealed record BookListItem(
    int Id,
    string Title,
    IReadOnlyList<string> Authors,
    string? Series,
    decimal? SeriesIndex,
    bool FileMissing,
    string? CoverFileName = null)
{
    /// <summary>The book's genres, ordered by name.</summary>
    public IReadOnlyList<string> Genres { get; init; } = [];

    /// <summary>The user's own collections the book is in, ordered by name (its series is <see cref="Series"/>).</summary>
    public IReadOnlyList<string> Collections { get; init; } = [];

    /// <summary>The distinct formats of the book's files, in enum order.</summary>
    public IReadOnlyList<BookFormat> Formats { get; init; } = [];

    public DateTime DateAddedUtc { get; init; }

    public ReadStatus ReadStatus { get; init; }

    public int? Rating { get; init; }

    /// <summary>True when the book has at least one file; wanted books (from M6's import) have none.</summary>
    public bool Owned { get; init; } = true;
}

/// <summary>One file of a book. <see cref="Present"/> is false when the last scan did not find it.</summary>
public sealed record BookFileInfo(string Path, BookFormat Format, long SizeBytes, bool Present, int Id = 0)
{
    /// <summary>Set for a file on Google Drive.</summary>
    public string? DriveFileId { get; init; }
}

/// <summary>Everything about one book. <see cref="Edited"/> is true once the user saved its details.</summary>
public sealed record BookDetails(
    BookListItem Book,
    string? Language,
    string? Isbn,
    bool Edited,
    IReadOnlyList<BookFileInfo> Files);

/// <summary>The details the user can change. Names are cleaned; blanks and repeats are dropped; a blank series
/// clears the series and its number.</summary>
public sealed record BookEdit(
    string Title,
    IReadOnlyList<string> Authors,
    string? Series,
    decimal? SeriesIndex,
    IReadOnlyList<string> Genres,
    string? Language,
    string? Isbn);

public interface ILibraryBooks
{
    /// <summary>All books, ordered by title.</summary>
    Task<IReadOnlyList<BookListItem>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>One book with its files, or null if there is no such book.</summary>
    Task<BookDetails?> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>The books that exist among <paramref name="ids"/>, with their files, in id order.</summary>
    Task<IReadOnlyList<BookDetails>> GetManyAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default);

    /// <summary>Saves the user's details and marks the book edited, so scans keep them. Returns false if there is
    /// no such book.</summary>
    /// <exception cref="ArgumentException">The title is blank.</exception>
    Task<bool> UpdateAsync(int id, BookEdit edit, CancellationToken cancellationToken = default);

    /// <summary>Sends one file of a book that has another file to the system trash, then forgets it. A book's last
    /// file is never trashed; if the trash refuses, nothing changes.</summary>
    Task<TrashResult> TrashFileAsync(int fileId, CancellationToken cancellationToken = default);

    /// <summary>Puts the book in the user collection called <paramref name="name"/>, creating it if needed (matched
    /// ignoring case and extra spaces). Returns false if there is no such book.</summary>
    /// <exception cref="ArgumentException">The name is blank.</exception>
    Task<bool> AddToCollectionAsync(int id, string name, CancellationToken cancellationToken = default);

    /// <summary>Takes the book out of that user collection, deleting the collection once no book is in it. Returns
    /// false if there is no such book.</summary>
    Task<bool> RemoveFromCollectionAsync(int id, string name, CancellationToken cancellationToken = default);
}
