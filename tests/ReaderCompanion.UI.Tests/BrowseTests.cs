using Microsoft.Extensions.DependencyInjection;
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
    public void PickingAGenre_Narrows_AndUntickingIt_RestoresAll()
    {
        var cut = RenderHome();

        FilterSteps.Pick(cut, "filter-genre", "Science Fiction");

        Assert.Equal(["Dune"], Titles(cut));
        Assert.Contains("1", cut.Find("#filter-genre .filter-count").TextContent);
        Assert.Empty(cut.FindAll(".chip")); // the choice stays inside the dropdown

        FilterSteps.Pick(cut, "filter-genre", "Science Fiction", on: false);

        Assert.Equal(3, Titles(cut).Count);
        Assert.Empty(cut.FindAll("#filter-genre .filter-count"));
    }

    [Fact]
    public void PickedValues_StayTickedInTheList_WhichClosesOnEscapeOrOutside()
    {
        var cut = RenderHome();

        FilterSteps.Pick(cut, "filter-author", "Neil Gaiman");
        FilterSteps.Pick(cut, "filter-author", "Frank Herbert"); // still open: several can be ticked

        Assert.Equal(["Frank Herbert", "Neil Gaiman", "Terry Pratchett"], FilterSteps.Options(cut, "filter-author"));
        Assert.Equal(["Frank Herbert", "Neil Gaiman"], FilterSteps.Ticked(cut, "filter-author"));
        Assert.Equal(["Dune", "Good Omens"], Titles(cut));

        cut.Find("#filter-author-menu").KeyDown("Escape");
        Assert.Empty(cut.FindAll("#filter-author-menu"));

        FilterSteps.Open(cut, "filter-author");
        cut.Find(".filter-backdrop").Click();
        Assert.Empty(cut.FindAll("#filter-author-menu"));
    }

    [Fact]
    public void FilterMenu_ClearUnticksAll()
    {
        var cut = RenderHome();
        FilterSteps.Pick(cut, "filter-genre", "Fantasy");

        cut.Find("#filter-genre-menu .filter-clear").Click();

        Assert.Equal(3, Titles(cut).Count);
        Assert.Empty(FilterSteps.Ticked(cut, "filter-genre"));
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

        FilterSteps.Pick(cut, "filter-collection", "Discworld");
        Assert.Equal(["Mort"], Titles(cut));
        cut.Find("#clear-filters").Click();

        FilterSteps.Pick(cut, "filter-format", "EPUB");
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

        FilterSteps.Pick(cut, "filter-status", "Read");

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
        Assert.Equal("3 books", Services.GetRequiredService<LibraryStatus>().Count);

        FilterSteps.Pick(cut, "filter-genre", "Fantasy");

        Assert.Equal("2 of 3 books", Services.GetRequiredService<LibraryStatus>().Count);
    }

    [Fact]
    public void GroupAndSort_AreSaved_FiltersAreNot()
    {
        var cut = RenderHome();

        cut.Find("#browse-group").Change(nameof(LibraryGroup.Genre));
        cut.Find("#browse-sort").Change(nameof(LibrarySort.DateAdded));
        var saves = _preferences.Saves;
        FilterSteps.Pick(cut, "filter-genre", "Fantasy");
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
        Assert.Equal("0 of 3 books", Services.GetRequiredService<LibraryStatus>().Count);

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
