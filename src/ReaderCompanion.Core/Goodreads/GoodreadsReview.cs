using ReaderCompanion.Core.Library;

namespace ReaderCompanion.Core.Goodreads;

/// <summary>An export row held for the user to decide on, kept between imports and restarts.</summary>
public sealed class GoodreadsPending
{
    public int Id { get; set; }

    public string GoodreadsId { get; set; } = "";

    public string Title { get; set; } = "";

    /// <summary>One author per line.</summary>
    public string Authors { get; set; } = "";

    public string? Isbn { get; set; }

    public int? Rating { get; set; }

    public DateOnly? DateRead { get; set; }

    public DateOnly? DateAdded { get; set; }

    public ReadStatus? Status { get; set; }

    /// <summary>One shelf per line.</summary>
    public string Shelves { get; set; } = "";

    public string? Series { get; set; }

    public decimal? SeriesIndex { get; set; }

    /// <summary>The book the row might be; null if that book was removed since.</summary>
    public int? BookId { get; set; }

    public static GoodreadsPending From(GoodreadsRow row, int bookId) => new()
    {
        GoodreadsId = row.GoodreadsId,
        Title = row.Title,
        Authors = string.Join('\n', row.Authors),
        Isbn = row.Isbn,
        Rating = row.Rating,
        DateRead = row.DateRead,
        DateAdded = row.DateAdded,
        Status = row.Status,
        Shelves = string.Join('\n', row.Shelves),
        Series = row.Series,
        SeriesIndex = row.SeriesIndex,
        BookId = bookId,
    };

    public IReadOnlyList<string> AuthorList => Lines(Authors);

    public GoodreadsRow ToRow() =>
        new(GoodreadsId, Title, AuthorList, Isbn, Rating, DateRead, DateAdded, Status, Lines(Shelves))
        {
            Series = Series,
            SeriesIndex = SeriesIndex,
        };

    private static string[] Lines(string text) => text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
}

/// <summary>A row waiting for review, shown next to the book it might be.</summary>
public sealed record PendingMatch(int Id, string Title, IReadOnlyList<string> Authors, string? Isbn, int? BookId, string? BookTitle, string? BookIsbn);

public static class GoodreadsReminder
{
    /// <summary>True when an import happened and is at least <paramref name="months"/> old.</summary>
    public static bool IsDue(DateTime? lastImportUtc, int months, DateTime nowUtc) =>
        lastImportUtc is { } last && last.AddMonths(months) <= nowUtc;
}
