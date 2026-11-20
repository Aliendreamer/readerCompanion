using System.Globalization;
using System.Text;

namespace ReaderCompanion.Core.Library;

/// <summary>How two records are recognised as the same book: by ISBN, or by title and first author compared loosely.
/// Shared by duplicate finding, Goodreads import and scans, so they always agree.</summary>
public static class MatchKeys
{
    /// <summary>A book's keys in the order they are trusted: ISBN first, then title and first author.</summary>
    public static IReadOnlyList<string> For(string? isbn, string title, string? firstAuthor) =>
        [.. new[] { Isbn(isbn) is { } i ? "isbn:" + i : null, TitleAuthor(title, firstAuthor) is { } t ? "title:" + t : null }.OfType<string>()];

    /// <summary>The title-and-first-author key, or null without an author (a bare title says too little).</summary>
    public static string? TitleAuthor(string title, string? firstAuthor) =>
        Fold(title) is { Length: > 0 } t && Fold(firstAuthor ?? "") is { Length: > 0 } a ? t + "|" + a : null;

    /// <summary>The ISBN's digits (and a final X), or null when it is too short to be one.</summary>
    public static string? Isbn(string? isbn)
    {
        var digits = new string((isbn ?? "").Where(c => char.IsAsciiDigit(c) || c is 'X' or 'x').ToArray()).ToUpperInvariant();
        return digits.Length >= 10 ? digits : null;
    }

    /// <summary>Lower case, accents removed, only letters and digits separated by single spaces.</summary>
    public static string Fold(string text)
    {
        var builder = new StringBuilder(text.Length);
        var space = false;
        foreach (var c in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(c))
            {
                if (space && builder.Length > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(char.ToLowerInvariant(c));
                space = false;
            }
            else
            {
                space = true;
            }
        }

        return builder.ToString();
    }
}
