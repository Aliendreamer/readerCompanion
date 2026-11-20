using System.Globalization;
using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Preferences;

namespace ReaderCompanion.UI.Library;

/// <summary>Whether a book is on the user's shelf (has files) or only wanted.</summary>
public enum Shelf
{
    Owned,
    Wanted,
}

/// <summary>What the user is looking for on the Library page. Several values in one filter match any of them;
/// every active filter and the search must match.</summary>
public sealed record BrowseQuery
{
    public string Search { get; init; } = "";

    public IReadOnlyCollection<string> Genres { get; init; } = [];

    public IReadOnlyCollection<string> Authors { get; init; } = [];

    public IReadOnlyCollection<string> Collections { get; init; } = [];

    public IReadOnlyCollection<BookFormat> Formats { get; init; } = [];

    public IReadOnlyCollection<ReadStatus> Statuses { get; init; } = [];

    public IReadOnlyCollection<Shelf> Shelves { get; init; } = [];

    public bool MissingOnly { get; init; }

    public LibraryGroup Group { get; init; }

    public LibrarySort Sort { get; init; }

    /// <summary>True when a filter or the search hides books.</summary>
    public bool IsNarrowed => !string.IsNullOrWhiteSpace(Search)
        || Genres.Count > 0 || Authors.Count > 0 || Collections.Count > 0 || Formats.Count > 0 || MissingOnly
        || Statuses.Count > 0 || Shelves.Count > 0;
}

/// <summary>A run of books under one heading; <see cref="Name"/> is null when the books are not grouped.
/// <see cref="IsCatchAll"/> marks the last section, for books without an author, genre or collection; its name can
/// equal a real one (a genre called "No genre").</summary>
public sealed record BookSection(string? Name, IReadOnlyList<BookListItem> Books, bool IsCatchAll = false);

/// <summary><see cref="Shown"/> counts each matching book once, even when it appears in several sections.</summary>
public sealed record BrowseResult(int Total, int Shown, IReadOnlyList<BookSection> Sections);

/// <summary>The filter values present in the library.</summary>
public sealed record BrowseOptions(
    IReadOnlyList<string> Genres,
    IReadOnlyList<string> Authors,
    IReadOnlyList<string> Collections,
    IReadOnlyList<BookFormat> Formats)
{
    public static readonly BrowseOptions Empty = new([], [], [], []);
}

/// <summary>Filters, searches, sorts and groups the library's books for the Library page.</summary>
public static class LibraryBrowser
{
    private static readonly StringComparer Names = StringComparer.InvariantCultureIgnoreCase;
    private static readonly CompareInfo Compare = CultureInfo.InvariantCulture.CompareInfo;

    public static BrowseOptions Options(IReadOnlyList<BookListItem> books) => new(
        Distinct(books.SelectMany(b => b.Genres)),
        Distinct(books.SelectMany(b => b.Authors)),
        Distinct(books.SelectMany(Collections)),
        [.. books.SelectMany(b => b.Formats).Distinct().Order()]);

    /// <summary>The user's own collections present in the library (series not included), sorted.</summary>
    public static IReadOnlyList<string> UserCollections(IReadOnlyList<BookListItem> books) =>
        Distinct(books.SelectMany(b => b.Collections));

    public static BrowseResult Browse(IReadOnlyList<BookListItem> books, BrowseQuery query)
    {
        // A collection always reads in series order, so grouping by collection overrides the chosen sort.
        var sort = query.Group == LibraryGroup.Collection ? LibrarySort.Series : query.Sort;
        var shown = Sorted(books.Where(b => Matches(b, query)), sort).ToList();
        IReadOnlyList<BookSection> sections = query.Group switch
        {
            LibraryGroup.Author => Group(shown, b => b.Authors, BookText.UnknownAuthor),
            LibraryGroup.Genre => Group(shown, b => b.Genres, "No genre"),
            LibraryGroup.Collection => Group(shown, Collections, "No collection"),
            _ => shown.Count > 0 ? [new BookSection(null, shown)] : [],
        };
        return new BrowseResult(books.Count, shown.Count, sections);
    }

    private static bool Matches(BookListItem book, BrowseQuery query) =>
        AnyOf(query.Genres, book.Genres)
        && AnyOf(query.Authors, book.Authors)
        && AnyOf(query.Collections, Collections(book))
        && (query.Formats.Count == 0 || book.Formats.Any(query.Formats.Contains))
        && (!query.MissingOnly || book.FileMissing)
        && (query.Statuses.Count == 0 || query.Statuses.Contains(book.ReadStatus))
        && (query.Shelves.Count == 0 || query.Shelves.Contains(book.Owned ? Shelf.Owned : Shelf.Wanted))
        && MatchesSearch(book, query.Search.Trim());

    private static bool AnyOf(IReadOnlyCollection<string> picked, IEnumerable<string> values) =>
        picked.Count == 0 || values.Any(v => picked.Contains(v, Names));

    private static bool MatchesSearch(BookListItem book, string text) =>
        text.Length == 0 || Contains(book.Title, text) || book.Authors.Any(a => Contains(a, text));

    private static bool Contains(string value, string text) =>
        Compare.IndexOf(value, text, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;

    private static IEnumerable<BookListItem> Sorted(IEnumerable<BookListItem> books, LibrarySort sort) => sort switch
    {
        LibrarySort.Author => books
            .OrderBy(b => b.Authors.Count == 0)
            .ThenBy(b => b.Authors.FirstOrDefault() ?? "", Names)
            .ThenBy(b => b.Title, Names),
        LibrarySort.Series => books
            .OrderBy(b => b.Series is null)
            .ThenBy(b => b.Series ?? "", Names)
            .ThenBy(b => b.SeriesIndex is null)
            .ThenBy(b => b.SeriesIndex)
            .ThenBy(b => b.Title, Names),
        LibrarySort.DateAdded => books.OrderByDescending(b => b.DateAddedUtc).ThenBy(b => b.Title, Names),
        _ => books.OrderBy(b => b.Title, Names),
    };

    /// <summary>The collections a book belongs to: its series and the user's own collections.</summary>
    private static IReadOnlyList<string> Collections(BookListItem book) =>
        book.Series is { } series ? [series, .. book.Collections] : book.Collections;

    /// <summary>One section per key, ordered by name, with books lacking a key in a last <paramref name="none"/>
    /// section. A book with several keys appears in each; books keep their order within a section.</summary>
    private static List<BookSection> Group(
        List<BookListItem> books, Func<BookListItem, IReadOnlyList<string>> keys, string none)
    {
        var named = books
            .SelectMany(b => keys(b).Distinct(Names).Select(k => (Key: k, Book: b)))
            .GroupBy(x => x.Key, Names)
            .OrderBy(g => g.Key, Names)
            .Select(g => new BookSection(g.Key, [.. g.Select(x => x.Book)]))
            .ToList();
        var without = books.Where(b => keys(b).Count == 0).ToList();
        if (without.Count > 0)
        {
            named.Add(new BookSection(none, without, IsCatchAll: true));
        }

        return named;
    }

    private static List<string> Distinct(IEnumerable<string> values) => [.. values.Distinct(Names).Order(Names)];
}
