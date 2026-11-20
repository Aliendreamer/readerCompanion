using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using ReaderCompanion.Core.Library;

namespace ReaderCompanion.UI.Library;

/// <summary>The edit form's text fields for one book, and their conversion back to a <see cref="BookEdit"/>. It
/// only checks what the form can get wrong (a blank title, a series number that is not a number); cleaning names
/// is left to the library.</summary>
public sealed class BookForm
{
    public string Title { get; set; } = "";

    /// <summary>One author per line.</summary>
    public string Authors { get; set; } = "";

    public string Series { get; set; } = "";

    public string SeriesIndex { get; set; } = "";

    /// <summary>One genre per line (genre names can contain commas).</summary>
    public string Genres { get; set; } = "";

    public string Language { get; set; } = "";

    public string Isbn { get; set; } = "";

    public static BookForm From(BookDetails details) => new()
    {
        Title = details.Book.Title,
        Authors = string.Join("\n", details.Book.Authors),
        Series = details.Book.Series ?? "",
        SeriesIndex = details.Book.SeriesIndex is { } index ? BookText.SeriesNumber(index) : "",
        Genres = string.Join("\n", details.Book.Genres),
        Language = details.Language ?? "",
        Isbn = details.Isbn ?? "",
    };

    /// <summary>The edit, or the reason the form cannot be saved.</summary>
    public bool TryToEdit([NotNullWhen(true)] out BookEdit? edit, [NotNullWhen(false)] out string? error)
    {
        edit = null;
        if (string.IsNullOrWhiteSpace(Title))
        {
            error = "A book needs a title.";
            return false;
        }

        decimal? index = null;
        if (!string.IsNullOrWhiteSpace(SeriesIndex))
        {
            if (!decimal.TryParse(SeriesIndex.Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            {
                error = "The series number must be a number, like 4 or 2.5.";
                return false;
            }

            index = parsed;
        }

        error = null;
        edit = new BookEdit(Title, Authors.Split('\n'), Series, index, Genres.Split('\n'), Language, Isbn);
        return true;
    }
}
