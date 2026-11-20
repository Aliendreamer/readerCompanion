using System.Globalization;
using ReaderCompanion.Core.Library;

namespace ReaderCompanion.UI.Library;

/// <summary>How a book's details read on screen, shared by every view.</summary>
public static class BookText
{
    public static string Authors(BookListItem book) =>
        book.Authors.Count > 0 ? string.Join(", ", book.Authors) : "Unknown author";

    public static string? Series(BookListItem book) => book.Series is null ? null
        : book.SeriesIndex is { } index
            ? $"{book.Series} #{index.ToString("0.##", CultureInfo.InvariantCulture)}"
            : book.Series;
}
