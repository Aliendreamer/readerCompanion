using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Preferences;
using ReaderCompanion.UI.Pages;

namespace ReaderCompanion.UI.Tests;

public class HomeTests : BunitContext
{
    private readonly List<BookListItem> _books = [];
    private readonly FakePreferences _preferences = new();

    public HomeTests() => Services.AddAppFakes(_books, _preferences);

    [Fact]
    public void EmptyLibrary_ShowsEmptyMessage_LinkToSources_AndNoToggle()
    {
        var cut = Render<Home>();

        Assert.Equal("Library", cut.Find("h1.visually-hidden").TextContent);
        cut.WaitForAssertion(() => Assert.Contains("Your library is empty.", cut.Markup));
        Assert.NotNull(cut.Find("a[href='sources']"));
        Assert.Empty(cut.FindAll(".view-toggle"));
        Assert.Empty(cut.FindAll(".page-header"));
    }

    [Fact]
    public void Routes_AtRoot_RendersHomeInsideLayout()
    {
        var cut = Render<Routes>();

        Assert.Equal("Library", cut.Find("main h1").TextContent);
    }

    [Fact]
    public void Header_ShowsBookCount()
    {
        _books.Add(Book(1, "A"));
        _books.Add(Book(2, "B"));

        var cut = Render<Home>();

        cut.WaitForAssertion(() => Assert.Equal("2 books", Services.GetRequiredService<LibraryStatus>().Count));
    }

    [Fact]
    public void DefaultsToGrid()
    {
        _books.Add(Book(1, "Mort"));

        var cut = Render<Home>();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".book-grid .book-card")));
        Assert.Empty(cut.FindAll(".book-table"));
        Assert.Equal("true", cut.Find("#view-grid").GetAttribute("aria-pressed"));
    }

    [Fact]
    public void StoredTableView_ShowsTable()
    {
        _preferences.Save(new AppPreferences(LibraryView: LibraryView.Table));
        _books.Add(Book(1, "Mort"));

        var cut = Render<Home>();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".book-table tbody tr")));
        Assert.Empty(cut.FindAll(".book-grid"));
    }

    [Fact]
    public void Toggle_SwitchesView_AndRemembersIt()
    {
        _books.Add(Book(1, "Mort"));
        var cut = Render<Home>();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".book-card")));

        cut.Find("#view-table").Click();

        Assert.Single(cut.FindAll(".book-table tbody tr"));
        Assert.Equal(LibraryView.Table, _preferences.Current.LibraryView);

        cut.Find("#view-grid").Click();

        Assert.Single(cut.FindAll(".book-card"));
        Assert.Equal(LibraryView.Grid, _preferences.Current.LibraryView);
    }

    [Theory]
    [InlineData(LibraryView.Grid)]
    [InlineData(LibraryView.Table)]
    public void Book_ShowsTitleAuthorsSeriesAndCover(LibraryView view)
    {
        _preferences.Save(new AppPreferences(LibraryView: view));
        _books.Add(new BookListItem(1, "Mort", ["Terry Pratchett", "Neil Gaiman"], "Discworld", 4, false, "m.jpg"));

        var cut = Render<Home>();

        cut.WaitForAssertion(() => Assert.Equal("Mort", cut.Find(".book-title").TextContent));
        Assert.Equal("Terry Pratchett, Neil Gaiman", cut.Find(".book-authors").TextContent);
        Assert.Equal("Discworld #4", cut.Find(".book-series").TextContent);
        Assert.Equal("test-cover://m.jpg", cut.Find("img.cover-image").GetAttribute("src"));
    }

    [Theory]
    [InlineData(LibraryView.Grid)]
    [InlineData(LibraryView.Table)]
    public void BookWithoutAuthors_ShowsUnknownAuthor(LibraryView view)
    {
        _preferences.Save(new AppPreferences(LibraryView: view));
        _books.Add(Book(1, "Mystery"));

        var cut = Render<Home>();

        cut.WaitForAssertion(() => Assert.Equal("Unknown author", cut.Find(".book-authors").TextContent));
    }

    [Theory]
    [InlineData(1.0, "Discworld #1")]
    [InlineData(2.5, "Discworld #2.5")]
    [InlineData(null, "Discworld")]
    public void BookInSeries_ShowsSeriesAndNumber(double? index, string expected)
    {
        _books.Add(new BookListItem(1, "Book", ["A"], "Discworld", (decimal?)index, false));

        var cut = Render<Home>();

        cut.WaitForAssertion(() => Assert.Equal(expected, cut.Find(".book-series").TextContent));
    }

    [Theory]
    [InlineData(LibraryView.Grid, ".book-card")]
    [InlineData(LibraryView.Table, "tr")]
    public void BookWithMissingFile_IsMarked(LibraryView view, string item)
    {
        _preferences.Save(new AppPreferences(LibraryView: view));
        _books.Add(new BookListItem(1, "Gone", ["A"], null, null, true));
        _books.Add(new BookListItem(2, "Here", ["A"], null, null, false));

        var cut = Render<Home>();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".book-missing")));
        Assert.Contains("Gone", cut.Find(".book-missing").Closest(item)!.TextContent);
    }

    [Fact]
    public void GoodreadsReminder_ShowsWhenDue_AndCanBeDismissed()
    {
        _books.Add(Book(1, "Mort"));
        _preferences.Save(_preferences.Current with { GoodreadsLastImportUtc = DateTime.UtcNow.AddMonths(-4) });

        var cut = Render<Home>();

        cut.WaitForAssertion(() => Assert.Contains("Goodreads", cut.Find(".goodreads-reminder").TextContent));
        Assert.NotNull(cut.Find(".goodreads-reminder a[href='sources']"));
        cut.Find(".goodreads-reminder button").Click();
        Assert.Empty(cut.FindAll(".goodreads-reminder"));
    }

    [Fact]
    public void GoodreadsReminder_NotShown_WhenRecentOrNeverImported()
    {
        _books.Add(Book(1, "Mort"));
        var cut = Render<Home>();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".book-title")));

        Assert.Empty(cut.FindAll(".goodreads-reminder"));
    }

    private static BookListItem Book(int id, string title) => new(id, title, [], null, null, false);
}
