using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Preferences;
using ReaderCompanion.UI.Pages;

namespace ReaderCompanion.UI.Tests;

/// <summary>The Library page's search, filters, grouping and sort, driven through the page.</summary>
public class BrowseTests : BunitContext
{
    private readonly List<BookListItem> _books =
    [
        new(1, "Dune", ["Frank Herbert"], null, null, false) { Genres = ["Science Fiction"], Formats = [BookFormat.Epub] },
        new(2, "Mort", ["Terry Pratchett"], "Discworld", 4, false) { Genres = ["Fantasy"], Formats = [BookFormat.Pdf] },
        new(3, "Good Omens", ["Terry Pratchett", "Neil Gaiman"], null, null, true) { Genres = ["Fantasy"], Formats = [BookFormat.Epub] },
    ];

    private readonly FakePreferences _preferences = new();

    public BrowseTests() => Services.AddAppFakes(_books, _preferences);

    private IRenderedComponent<Home> RenderHome()
    {
        var cut = Render<Home>();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".book-title")));
        return cut;
    }

    private static IReadOnlyList<string> Titles(IRenderedComponent<Home> cut) =>
        [.. cut.FindAll(".book-title").Select(e => e.TextContent)];

    // ---------- 3.1 bar ----------

    [Fact]
    public void PickingAGenre_Narrows_AndRemovingTheChip_RestoresAll()
    {
        var cut = RenderHome();

        cut.Find("#filter-genre").Change("Science Fiction");

        Assert.Equal(["Dune"], Titles(cut));
        var chip = cut.Find(".chip");
        Assert.Contains("Science Fiction", chip.TextContent);

        chip.Click();

        Assert.Equal(3, Titles(cut).Count);
        Assert.Empty(cut.FindAll(".chip"));
    }

    [Fact]
    public void PickedValue_LeavesThePickerList()
    {
        var cut = RenderHome();

        cut.Find("#filter-author").Change("Neil Gaiman");

        Assert.DoesNotContain("Neil Gaiman", cut.FindAll("#filter-author option").Select(o => o.TextContent));
        Assert.Equal(["Good Omens"], Titles(cut));
    }

    [Fact]
    public void Search_NarrowsAsYouType()
    {
        var cut = RenderHome();

        cut.Find("#browse-search").Input("pratch");

        Assert.Equal(["Good Omens", "Mort"], Titles(cut));
    }

    [Fact]
    public void CollectionFormatAndMissingFilters_Narrow()
    {
        var cut = RenderHome();

        cut.Find("#filter-collection").Change("Discworld");
        Assert.Equal(["Mort"], Titles(cut));
        cut.Find("#clear-filters").Click();

        cut.Find("#filter-format").Change("EPUB");
        Assert.Equal(["Dune", "Good Omens"], Titles(cut));

        cut.Find("#browse-missing").Change(true);
        Assert.Equal(["Good Omens"], Titles(cut));
    }

    [Fact]
    public void StatusFilter_AndBadge()
    {
        _books[0] = _books[0] with { ReadStatus = ReadStatus.Read };
        var cut = RenderHome();

        Assert.Equal("Read", cut.Find(".status-badge").TextContent);

        cut.Find("#filter-status").Change("Read");

        Assert.Equal(["Dune"], Titles(cut));
    }

    // ---------- 3.2 sections ----------

    [Theory]
    [InlineData(LibraryView.Grid)]
    [InlineData(LibraryView.Table)]
    public void GroupByAuthor_ShowsCollapsibleSectionsWithCounts(LibraryView view)
    {
        _preferences.Save(new AppPreferences(LibraryView: view));
        var cut = RenderHome();

        cut.Find("#browse-group").Change(nameof(LibraryGroup.Author));

        var sections = cut.FindAll("details.book-section");
        Assert.Equal(
            ["Frank Herbert (1)", "Neil Gaiman (1)", "Terry Pratchett (2)"],
            sections.Select(s => s.QuerySelector("summary")!.TextContent.Trim()));
        Assert.All(sections, s => Assert.True(s.HasAttribute("open")));
        Assert.Equal(4, Titles(cut).Count); // Good Omens under both of its authors
    }

    [Fact]
    public void GroupByCollection_DisablesSort_SinceSeriesOrderApplies()
    {
        var cut = RenderHome();
        Assert.False(cut.Find("#browse-sort").HasAttribute("disabled"));

        cut.Find("#browse-group").Change(nameof(LibraryGroup.Collection));

        Assert.True(cut.Find("#browse-sort").HasAttribute("disabled"));
    }

    [Fact]
    public void GenreNamedLikeTheCatchAllSection_DoesNotClash()
    {
        _books.Add(new BookListItem(4, "Odd", [], null, null, false) { Genres = ["No genre"] });
        _books.Add(new BookListItem(5, "Bare", [], null, null, false));
        var cut = RenderHome();

        cut.Find("#browse-group").Change(nameof(LibraryGroup.Genre));

        Assert.Equal(
            ["Fantasy (2)", "No genre (1)", "Science Fiction (1)", "No genre (1)"],
            cut.FindAll("details.book-section summary").Select(s => s.TextContent.Trim()));
    }

    [Fact]
    public void Ungrouped_HasNoSectionHeadings()
    {
        var cut = RenderHome();

        Assert.Empty(cut.FindAll("details.book-section"));
    }

    // ---------- 3.3 page ----------

    [Fact]
    public void Count_ShowsNOfMWhileNarrowed()
    {
        var cut = RenderHome();
        Assert.Equal("3 books", cut.Find(".book-count").TextContent);

        cut.Find("#filter-genre").Change("Fantasy");

        Assert.Equal("2 of 3 books", cut.Find(".book-count").TextContent);
    }

    [Fact]
    public void GroupAndSort_AreSaved_FiltersAreNot()
    {
        var cut = RenderHome();

        cut.Find("#browse-group").Change(nameof(LibraryGroup.Genre));
        cut.Find("#browse-sort").Change(nameof(LibrarySort.DateAdded));
        var saves = _preferences.Saves;
        cut.Find("#filter-genre").Change("Fantasy");
        cut.Find("#browse-search").Input("mort");

        Assert.Equal(LibraryGroup.Genre, _preferences.Current.LibraryGroup);
        Assert.Equal(LibrarySort.DateAdded, _preferences.Current.LibrarySort);
        Assert.Equal(saves, _preferences.Saves);
    }

    [Fact]
    public void StoredGroupAndSort_AreUsedOnOpen()
    {
        _preferences.Save(new AppPreferences(LibraryGroup: LibraryGroup.Collection, LibrarySort: LibrarySort.Author));

        var cut = RenderHome();

        Assert.Equal(nameof(LibraryGroup.Collection), cut.Find("#browse-group").GetAttribute("value"));
        Assert.Equal(nameof(LibrarySort.Author), cut.Find("#browse-sort").GetAttribute("value"));
        Assert.Equal(
            ["Discworld (1)", "No collection (2)"],
            cut.FindAll("details.book-section summary").Select(s => s.TextContent.Trim()));
    }

    [Fact]
    public void NothingMatches_SaysSo_AndClearFiltersRestoresAll()
    {
        var cut = RenderHome();

        cut.Find("#browse-search").Input("zzz");

        Assert.Contains("No books match", cut.Markup);
        Assert.Equal("0 of 3 books", cut.Find(".book-count").TextContent);

        cut.Find("#clear-filters").Click();

        Assert.Equal(3, Titles(cut).Count);
        Assert.Equal("", cut.Find("#browse-search").GetAttribute("value"));
    }

    [Fact]
    public void EmptyLibrary_ShowsNoBrowseControls()
    {
        _books.Clear();

        var cut = Render<Home>();

        cut.WaitForAssertion(() => Assert.Contains("Your library is empty.", cut.Markup));
        Assert.Empty(cut.FindAll("#browse-search"));
        Assert.Empty(cut.FindAll("#browse-group"));
    }
}
