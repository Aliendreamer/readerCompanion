using System.Text.RegularExpressions;
using ReaderCompanion.Core.Library;

namespace ReaderCompanion.Core.Goodreads;

/// <summary>Looser comparisons for matching Goodreads rows to books whose files carry the title and authors in other
/// forms: a subtitle more or less, "Surname, Given", several authors in one name, junk from download sites.</summary>
public static partial class GoodreadsMatch
{
    // Names that say nothing about who wrote the book.
    private static readonly HashSet<string> NoAuthor = new(StringComparer.Ordinal) { "unknown", "anonymous", "various", "n a" };

    /// <summary>The title before its subtitle, without bracketed notes or a file extension, folded as in
    /// <see cref="MatchKeys.Fold"/>; empty when nothing is left.</summary>
    public static string MainTitle(string title)
    {
        var text = FileExtension().Replace(title, "");
        text = Notes().Replace(text, " ");
        var colon = text.IndexOf(':', StringComparison.Ordinal);
        return MatchKeys.Fold(colon > 0 ? text[..colon] : text);
    }

    /// <summary>The authors' surnames, folded, in order and without repeats. Names are split on ";", "&amp;", "and"
    /// and commas; "Surname, Given" is one name. Placeholders such as "Unknown" and web addresses give none.</summary>
    public static IReadOnlyList<string> Surnames(IEnumerable<string> authors)
    {
        var surnames = new List<string>();
        foreach (var name in authors.SelectMany(Names))
        {
            if (name.Contains("www.", StringComparison.OrdinalIgnoreCase) || name.Contains(".com", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var folded = MatchKeys.Fold(name);
            if (folded.Length == 0 || NoAuthor.Contains(folded))
            {
                continue;
            }

            var surname = folded[(folded.LastIndexOf(' ') + 1)..];
            if (!surnames.Contains(surname))
            {
                surnames.Add(surname);
            }
        }

        return surnames;
    }

    /// <summary>Whether any author of one is an author of the other, by surname.</summary>
    public static bool ShareAnAuthor(IEnumerable<string> a, IEnumerable<string> b) => Surnames(a).Intersect(Surnames(b)).Any();

    /// <summary>One author field as separate people, given name first: "Kleppmann, Martin" is "Martin Kleppmann",
    /// "Mario Casciaro &amp; Luciano Mammino" two people, a leading "By " dropped.</summary>
    public static IEnumerable<string> Names(string author)
    {
        foreach (var part in Separators().Split(ByPrefix().Replace(author.Trim(), "")))
        {
            var commas = part.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (commas is [var surname, var given] && !surname.Contains(' ', StringComparison.Ordinal))
            {
                yield return $"{given} {surname}"; // "Kleppmann, Martin"
                continue;
            }

            foreach (var name in commas)
            {
                yield return name;
            }
        }
    }

    [GeneratedRegex(@"\.(epub|pdf|mobi|azw3?)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex FileExtension();

    // "( PDFDrive.com )", "\( PDFDrive.com \)", "(Addison-Wesley Series)", "[Special Edition]"
    [GeneratedRegex(@"\\?[\(\[][^()\[\]]*\\?[\)\]]")]
    private static partial Regex Notes();

    [GeneratedRegex(@"\s*(?:;|&|\band\b|,\s*and\b)\s*", RegexOptions.IgnoreCase)]
    private static partial Regex Separators();

    [GeneratedRegex(@"^by\s+", RegexOptions.IgnoreCase)]
    private static partial Regex ByPrefix();
}
