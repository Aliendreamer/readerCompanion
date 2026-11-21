using System.Globalization;
using ReaderCompanion.Core.Goodreads;
using ReaderCompanion.Core.Library;

namespace ReaderCompanion.Core.Organising;

/// <summary>A present file of a book, with the details its new path is built from.</summary>
public sealed record PlannedFile(
    int FileId,
    string Path,
    string Title,
    IReadOnlyList<string> Authors,
    string? Series,
    decimal? SeriesIndex,
    IReadOnlyList<string> Genres,
    BookFormat Format);

public sealed record Move(int FileId, string From, string To);

/// <summary>Works out where each file goes, without touching the disk. One layout:
/// <c>Group/Author/Series/NN - Title</c>, where the group is the same for all of an author's books, the author is written
/// as in the book, and the series folder and number appear only when the book has them.</summary>
public static class OrganisePlanner
{
    private const int MaxNameLength = 100;

    // Not allowed in Windows file names (Linux only forbids '/' and NUL, so this set is safe on both).
    private static readonly char[] Invalid = ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];

    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    /// <summary>The moves that put <paramref name="files"/> in place under <paramref name="root"/>. Files already in
    /// place, and books without a usable author, are left out; a path that is taken, on disk (<paramref name="exists"/>)
    /// or by another move, gets " (2)", " (3)" and so on.</summary>
    public static IReadOnlyList<Move> Plan(string root, IReadOnlyList<PlannedFile> files, Func<string, bool> exists)
    {
        var paths = StringComparer.OrdinalIgnoreCase;
        var authored = files.Select(f => (File: f, Author: AuthorOf(f))).Where(x => x.Author is not null).ToList();
        // A top folder is a collection (a possible group) only when it holds books of several authors.
        var collections = authored
            .Select(x => (Folder: TopFolder(root, x.File.Path), x.Author))
            .Where(x => x.Folder is not null)
            .GroupBy(x => x.Folder!, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Select(x => x.Author).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
            .Select(g => g.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var groups = authored.GroupBy(x => x.Author!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => GroupOf(root, g.Key, [.. g.Select(x => x.File)], collections), StringComparer.OrdinalIgnoreCase);
        var wanted = authored
            .Select(x => (x.File, Target: Path.Combine([root, .. Segments(x.File, x.Author!, groups[x.Author!])])))
            .ToList();
        var taken = new HashSet<string>(wanted.Where(x => paths.Equals(x.Target, x.File.Path)).Select(x => x.Target), paths);

        var moves = new List<Move>();
        foreach (var (file, target) in wanted.Where(x => !paths.Equals(x.Target, x.File.Path)))
        {
            var to = target;
            for (var n = 2; taken.Contains(to) || exists(to); n++)
            {
                to = Path.Combine(Path.GetDirectoryName(target)!, $"{Path.GetFileNameWithoutExtension(target)} ({n}){Path.GetExtension(target)}");
            }

            taken.Add(to);
            moves.Add(new Move(file.FileId, file.Path, to));
        }

        return moves;
    }

    /// <summary>The first author's first person, given name first (an author field can hold several people), or null
    /// when there is none worth a folder ("Unknown", a web address).</summary>
    private static string? AuthorOf(PlannedFile file) =>
        file.Authors is [var first, ..]
            && GoodreadsMatch.Names(first).FirstOrDefault(n => GoodreadsMatch.Surnames([n]).Count > 0) is { } person
                ? person.Trim()
                : null;

    /// <summary>The author's group: the genre most of their books have (ties: alphabetical); without genres, the
    /// collection folder most of their files are in, unless it names the author; otherwise no group.</summary>
    private static string? GroupOf(string root, string author, IReadOnlyList<PlannedFile> books, HashSet<string> collections)
    {
        if (MostCommon(books.SelectMany(b => b.Genres.Distinct(StringComparer.OrdinalIgnoreCase))) is { } genre)
        {
            return genre;
        }

        return MostCommon(books.Select(b => TopFolder(root, b.Path)).OfType<string>().Where(collections.Contains)) is { } folder
            && !GoodreadsMatch.ShareAnAuthor([folder], [author])
                ? folder
                : null;
    }

    private static string? MostCommon(IEnumerable<string> names) => names
        .GroupBy(n => n, StringComparer.OrdinalIgnoreCase)
        .OrderByDescending(g => g.Count())
        .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
        .Select(g => g.Key)
        .FirstOrDefault();

    /// <summary>The first folder under the root a file is in, or null for a file right in the root.</summary>
    private static string? TopFolder(string root, string path) =>
        Path.GetRelativePath(root, path).Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) is [var top, _, ..]
            ? top
            : null;

    private static IEnumerable<string> Segments(PlannedFile file, string author, string? group)
    {
        if (group is not null)
        {
            yield return Safe(group);
        }

        yield return Safe(author);
        if (file.Series is not null)
        {
            yield return Safe(file.Series);
        }

        var extension = file.Format == BookFormat.Pdf ? ".pdf" : ".epub";
        yield return Safe(file.Series is not null && file.SeriesIndex is { } index
            ? $"{index.ToString("00.##", CultureInfo.InvariantCulture)} - {file.Title}"
            : file.Title) + extension;
    }

    /// <summary>One path segment that is valid on Windows and Linux.</summary>
    private static string Safe(string name)
    {
        var chars = name.Select(c => char.IsControl(c) || Invalid.Contains(c) ? '_' : c).ToArray();
        var safe = new string(chars).Trim();
        if (safe.Length > MaxNameLength)
        {
            safe = safe[..MaxNameLength];
        }

        safe = safe.TrimEnd('.', ' ');
        if (safe.Length == 0)
        {
            return "_";
        }

        return Reserved.Contains(safe) ? safe + "_" : safe;
    }
}
