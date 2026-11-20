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

    public List<BookAuthor> Authors { get; set; } = [];

    public List<BookGenre> Genres { get; set; } = [];

    public List<BookFile> Files { get; set; } = [];
}
