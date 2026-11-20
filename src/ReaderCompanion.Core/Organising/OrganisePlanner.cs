using System.Globalization;
using ReaderCompanion.Core.Library;

namespace ReaderCompanion.Core.Organising;

/// <summary>Folder layouts the files can be organised into.</summary>
public enum OrganiseLayout
{
    /// <summary><c>Genre/Author/Title</c>.</summary>
    GenreAuthorTitle,

    /// <summary><c>Author/Series/NN - Title</c>.</summary>
    AuthorSeriesTitle,

    /// <summary><c>Author/Title</c>.</summary>
    AuthorTitle,
}

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

/// <summary>Works out where each file goes in a layout, without touching the disk.</summary>
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

    /// <summary>The moves that put <paramref name="files"/> into <paramref name="layout"/> under
    /// <paramref name="root"/>. Files already in place are left out; a path that is taken, on disk
    /// (<paramref name="exists"/>) or by another move, gets " (2)", " (3)" and so on.</summary>
    public static IReadOnlyList<Move> Plan(
        string root, OrganiseLayout layout, IReadOnlyList<PlannedFile> files, Func<string, bool> exists)
    {
        var paths = StringComparer.OrdinalIgnoreCase;
        var wanted = files.Select(f => (File: f, Target: Path.Combine([root, .. Segments(f, layout)]))).ToList();
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

    private static IEnumerable<string> Segments(PlannedFile file, OrganiseLayout layout)
    {
        var author = Safe(file.Authors.Count > 0 ? LastFirst(file.Authors[0]) : "Unknown author");
        var extension = file.Format == BookFormat.Pdf ? ".pdf" : ".epub";
        var fileName = Safe(layout == OrganiseLayout.AuthorSeriesTitle && file.Series is not null && file.SeriesIndex is { } index
            ? $"{index.ToString("00.##", CultureInfo.InvariantCulture)} - {file.Title}"
            : file.Title) + extension;
        if (layout == OrganiseLayout.GenreAuthorTitle)
        {
            yield return Safe(file.Genres.Count > 0 ? file.Genres[0] : "Unknown genre");
        }

        yield return author;
        if (layout == OrganiseLayout.AuthorSeriesTitle)
        {
            yield return Safe(file.Series ?? "No series");
        }

        yield return fileName;
    }

    /// <summary>"J.R.R. Tolkien" → "Tolkien, J.R.R."; a name with a comma, or a single word, is kept.</summary>
    private static string LastFirst(string name)
    {
        var trimmed = name.Trim();
        var space = trimmed.LastIndexOf(' ');
        return trimmed.Contains(',', StringComparison.Ordinal) || space < 0
            ? trimmed
            : $"{trimmed[(space + 1)..]}, {trimmed[..space].TrimEnd()}";
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
