using ReaderCompanion.Core.Library;

namespace ReaderCompanion.Core.Tests;

public class ReadingStatsTests
{
    private static StatsBook Book(
        string title, string[]? authors = null, string? series = null, bool owned = true,
        ReadStatus status = ReadStatus.NotStarted, decimal? index = null, params DateOnly[] reads) =>
        new(title, authors ?? [], series, owned, status, reads) { SeriesIndex = index };

    [Fact]
    public void ASeries_KnowsItsBooks_InSeriesOrder_AndWhichAreRead()
    {
        var stats = ReadingStats.From(
        [
            Book("Reaper Man", ["Terry Pratchett"], "Discworld", index: 11),
            Book("Mort", ["Terry Pratchett"], "Discworld", status: ReadStatus.Read, index: 4),
            Book("Small Gods", ["Terry Pratchett"], "Discworld", owned: false, status: ReadStatus.WantToRead),
        ], 2026);

        var discworld = Assert.Single(stats.Series);
        Assert.Equal(
            [("Mort", 4m, true, true), ("Reaper Man", 11m, false, true), ("Small Gods", (decimal?)null, false, false)],
            discworld.Books.Select(b => (b.Title, b.Number, b.IsRead, b.Owned)));
    }

    [Fact]
    public void ThisYear_CountsEveryRead_AndListsTheBooks_ByYearNewestFirst()
    {
        var stats = ReadingStats.From(
            [Book("Mort", reads: [new(2026, 1, 10), new(2026, 3, 2)]), Book("Dune", reads: [new(2025, 6, 1)])],
            2026);

        Assert.Equal(2, stats.ThisYear);
        Assert.Equal([("Mort", 2)], stats.ThisYearBooks.Select(b => (b.Title, b.Reads)));
        Assert.Equal([(2026, 2), (2025, 1)], stats.ByYear.Select(y => (y.Year, y.Reads)));
    }

    [Fact]
    public void MarkedReadWithoutDates_CountsAsRead_ButNotInAnyYear()
    {
        var stats = ReadingStats.From([Book("Mort", status: ReadStatus.Read)], 2026);

        Assert.Equal(1, stats.Glance.OwnedRead);
        Assert.Equal(0, stats.ThisYear);
        Assert.Empty(stats.ByYear);
    }

    [Fact]
    public void Glance_SplitsOwnedAndWantedByRead()
    {
        var stats = ReadingStats.From(
            [Book("A", reads: [new(2026, 1, 1)]), Book("B"), Book("C", owned: false),
             Book("D", owned: false, status: ReadStatus.Read)],
            2026);

        Assert.Equal(new Glance(OwnedRead: 1, OwnedUnread: 1, Wanted: 1, ReadNotOwned: 1), stats.Glance);
    }

    [Fact]
    public void Authors_MostReadFirst_AndSeriesWithTwoOrMoreBooks()
    {
        var books = Enumerable.Range(1, 7)
            .Select(i => Book($"Discworld {i}", ["Terry Pratchett"], "Discworld", reads: i <= 4 ? [new(2026, 1, i)] : []))
            .Append(Book("Dune", ["Frank Herbert"], "Dune Chronicles", reads: [new(2025, 1, 1)]))
            .Append(Book("Good Omens", ["Terry Pratchett", "Neil Gaiman"]))
            .ToList();

        var stats = ReadingStats.From(books, 2026);

        Assert.Equal(
            [("Terry Pratchett", 4, 8), ("Frank Herbert", 1, 1), ("Neil Gaiman", 0, 1)],
            stats.Authors.Select(a => (a.Name, a.Read, a.Total)));
        Assert.Equal([("Discworld", 4, 7)], stats.Series.Select(s => (s.Name, s.Read, s.Total)));
    }

    [Fact]
    public void AnAuthorListedTwiceOnABook_CountsOnce()
    {
        var stats = ReadingStats.From([Book("Mort", ["Terry Pratchett", "terry pratchett"], status: ReadStatus.Read)], 2026);

        Assert.Equal(("Terry Pratchett", 1, 1), stats.Authors.Select(a => (a.Name, a.Read, a.Total)).Single());
    }

    [Fact]
    public void Abandoned_IsNotARead()
    {
        var stats = ReadingStats.From([Book("Mort", status: ReadStatus.Abandoned)], 2026);

        Assert.Equal(new Glance(0, 1, 0, 0), stats.Glance);
    }
}
