using System.Globalization;
using ReaderCompanion.Core.Library;

namespace ReaderCompanion.UI.Library;

/// <summary>How a book's details read on screen, shared by every view.</summary>
public static class BookText
{
    public const string UnknownAuthor = "Unknown author";

    public static string Authors(BookListItem book) =>
        book.Authors.Count > 0 ? string.Join(", ", book.Authors) : UnknownAuthor;

    public static string? Series(BookListItem book) => book.Series is null ? null
        : book.SeriesIndex is { } index ? $"{book.Series} #{SeriesNumber(index)}"
        : book.Series;

    /// <summary>A series position as people write it: 4, 2.5.</summary>
    public static string SeriesNumber(decimal index) => index.ToString("0.##", CultureInfo.InvariantCulture);
}
