namespace ReaderCompanion.Core.Library;

public sealed class Book
{
    public int Id { get; set; }

    public string Title { get; set; } = "";

    public string? Language { get; set; }

    public string? Isbn { get; set; }

    public int? SeriesId { get; set; }

    public Collection? Series { get; set; }

    public decimal? SeriesIndex { get; set; }

    /// <summary>File name inside the covers folder, or null when the book has no cover.</summary>
    public string? CoverFileName { get; set; }

    /// <summary>When the book entered the library: the time of the scan that first found it.</summary>
    public DateTime DateAddedUtc { get; set; }

    /// <summary>When the user last saved the book's details; null if never. Scans leave an edited book's details
    /// (title, authors, series, genres, language, ISBN) alone.</summary>
    public DateTime? EditedUtc { get; set; }

    public List<BookAuthor> Authors { get; set; } = [];

    public List<BookGenre> Genres { get; set; } = [];

    public List<BookFile> Files { get; set; } = [];

    /// <summary>The user's own collections this book is in; scans never change them.</summary>
    public List<BookCollection> Collections { get; set; } = [];
}
