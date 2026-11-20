namespace ReaderCompanion.Core.Library;

/// <summary>What the stats need to know about one book. <see cref="FinishedReads"/> are the finish dates of its
/// reads that were not abandoned.</summary>
public sealed record StatsBook(
    string Title,
    IReadOnlyList<string> Authors,
    string? Series,
    bool Owned,
    ReadStatus Status,
    IReadOnlyList<DateOnly> FinishedReads)
{
    /// <summary>Read to the end at least once, or marked read.</summary>
    public bool IsRead => FinishedReads.Count > 0 || Status == ReadStatus.Read;
}

public sealed record BookReads(string Title, int Reads);

public sealed record YearReads(int Year, int Reads);

public sealed record Glance(int OwnedRead, int OwnedUnread, int Wanted, int ReadNotOwned);

/// <summary>Books read out of the books in the library for one author or series.</summary>
public sealed record Progress(string Name, int Read, int Total);

public sealed record ReadingStats(
    int ThisYear,
    IReadOnlyList<BookReads> ThisYearBooks,
    IReadOnlyList<YearReads> ByYear,
    Glance Glance,
    IReadOnlyList<Progress> Authors,
    IReadOnlyList<Progress> Series)
{
    public static ReadingStats From(IReadOnlyList<StatsBook> books, int year)
    {
        var names = StringComparer.InvariantCultureIgnoreCase;
        var thisYear = books
            .Select(b => new BookReads(b.Title, b.FinishedReads.Count(d => d.Year == year)))
            .Where(b => b.Reads > 0)
            .OrderBy(b => b.Title, names)
            .ToList();
        var byYear = books
            .SelectMany(b => b.FinishedReads)
            .GroupBy(d => d.Year)
            .OrderByDescending(g => g.Key)
            .Select(g => new YearReads(g.Key, g.Count()))
            .ToList();
        var glance = new Glance(
            books.Count(b => b.Owned && b.IsRead),
            books.Count(b => b.Owned && !b.IsRead),
            books.Count(b => !b.Owned && !b.IsRead),
            books.Count(b => !b.Owned && b.IsRead));

        static Progress Of(IGrouping<string, StatsBook> g) => new(g.Key, g.Count(b => b.IsRead), g.Count());

        var authors = books
            .SelectMany(b => b.Authors.Distinct(names), (book, author) => (book, author))
            .GroupBy(x => x.author, x => x.book, names)
            .Select(Of)
            .OrderByDescending(p => p.Read)
            .ThenBy(p => p.Name, names)
            .ToList();
        var series = books
            .Where(b => b.Series is not null)
            .GroupBy(b => b.Series!, names)
            .Where(g => g.Count() > 1)
            .Select(Of)
            .OrderBy(p => p.Name, names)
            .ToList();

        return new ReadingStats(thisYear.Sum(b => b.Reads), thisYear, byYear, glance, authors, series);
    }
}
