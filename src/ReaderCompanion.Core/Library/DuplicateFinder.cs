using System.Globalization;
using System.Text;

namespace ReaderCompanion.Core.Library;

/// <summary>What duplicate matching looks at for one book.</summary>
public sealed record DuplicateCandidate(int Id, string Title, string? FirstAuthor, string? Isbn);

/// <summary>Groups books that are likely the same: the same ISBN, or the same title and first author compared
/// loosely (case, accents, punctuation and spacing ignored). Books without an author match only by ISBN, and two
/// books the user called "not duplicates" never share a group.</summary>
public static class DuplicateFinder
{
    /// <param name="dismissed">Pairs the user said are not duplicates, smaller id first.</param>
    /// <returns>Groups of two or more book ids, ids ascending, groups ordered by their first book's title.</returns>
    public static IReadOnlyList<IReadOnlyList<int>> Find(
        IReadOnlyList<DuplicateCandidate> books, IReadOnlySet<(int, int)> dismissed)
    {
        var parent = books.ToDictionary(b => b.Id, b => b.Id);
        var members = books.ToDictionary(b => b.Id, b => new List<int> { b.Id });
        int Root(int id) => parent[id] == id ? id : parent[id] = Root(parent[id]);

        // Join two groups unless that would put a dismissed pair together (also through a third book).
        void Join(int a, int b)
        {
            var (ra, rb) = (Root(a), Root(b));
            if (ra == rb || members[ra].Any(x => members[rb].Any(y => dismissed.Contains(x < y ? (x, y) : (y, x)))))
            {
                return;
            }

            parent[rb] = ra;
            members[ra].AddRange(members[rb]);
        }

        foreach (var sameKey in books.SelectMany(b => Keys(b).Select(k => (Key: k, b.Id))).GroupBy(x => x.Key))
        {
            foreach (var (a, b) in Pairs([.. sameKey.Select(x => x.Id)]))
            {
                Join(a, b);
            }
        }

        return [.. books.GroupBy(b => Root(b.Id))
            .Where(g => g.Count() > 1)
            .Select(g => g.OrderBy(b => b.Id).ToList())
            .OrderBy(g => g[0].Title, StringComparer.InvariantCultureIgnoreCase)
            .ThenBy(g => g[0].Id)
            .Select(g => (IReadOnlyList<int>)[.. g.Select(b => b.Id)])];
    }

    /// <summary>Every pair of the given ids, smaller id first, as dismissals store them.</summary>
    public static IEnumerable<(int, int)> Pairs(IEnumerable<int> ids)
    {
        var sorted = ids.Distinct().Order().ToList();
        for (var i = 0; i < sorted.Count; i++)
        {
            for (var j = i + 1; j < sorted.Count; j++)
            {
                yield return (sorted[i], sorted[j]);
            }
        }
    }

    private static IEnumerable<string> Keys(DuplicateCandidate book)
    {
        if (IsbnKey(book.Isbn) is { } isbn)
        {
            yield return "isbn:" + isbn;
        }

        // Without an author, a title alone ("Untitled", "Notes") says too little.
        if (Fold(book.Title) is { Length: > 0 } title && Fold(book.FirstAuthor ?? "") is { Length: > 0 } author)
        {
            yield return "title:" + title + "|" + author;
        }
    }

    /// <summary>The ISBN's digits (and a final X), or null when it is too short to be one.</summary>
    private static string? IsbnKey(string? isbn)
    {
        var digits = new string((isbn ?? "").Where(c => char.IsAsciiDigit(c) || c is 'X' or 'x').ToArray()).ToUpperInvariant();
        return digits.Length >= 10 ? digits : null;
    }

    /// <summary>Lower case, accents removed, only letters and digits separated by single spaces.</summary>
    private static string Fold(string text)
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
