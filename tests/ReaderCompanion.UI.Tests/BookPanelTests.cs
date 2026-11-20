using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Preferences;
using ReaderCompanion.UI.Pages;

namespace ReaderCompanion.UI.Tests;

/// <summary>The book details panel, opened from the Library page.</summary>
public class BookPanelTests : BunitContext
{
    private readonly List<BookListItem> _books =
    [
        new(1, "Dune", ["Frank Herbert"], null, null, false) { Genres = ["Science Fiction"] },
        new(2, "Mort", ["Terry Pratchett"], "Discworld", 4, false) { Genres = ["Fantasy"], DateAddedUtc = new DateTime(2026, 3, 4) },
    ];

    private readonly FakeBooks _libraryBooks;
    private readonly FakeLauncher _launcher = new();
    private readonly FakePreferences _preferences = new();

    public BookPanelTests()
    {
        _libraryBooks = new FakeBooks(_books);
        _libraryBooks.Files[2] =
        [
            new BookFileInfo("/books/mort.epub", BookFormat.Epub, 1_500_000, Present: true),
            new BookFileInfo("/old/mort.pdf", BookFormat.Pdf, 900, Present: false),
        ];
        Services.AddAppFakes(preferences: _preferences, libraryBooks: _libraryBooks, launcher: _launcher);
    }

    private IRenderedComponent<Home> RenderHome()
    {
        var cut = Render<Home>();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".book-title")));
        return cut;
    }

    private static void OpenBook(IRenderedComponent<Home> cut, string title)
    {
        cut.FindAll("button.book-title").Single(b => b.TextContent == title).Click();
        cut.WaitForAssertion(() => Assert.Equal(title, cut.Find(".book-panel .panel-title").TextContent));
    }

    // ---------- 3.1 view ----------

    [Theory]
    [InlineData(LibraryView.Grid)]
    [InlineData(LibraryView.Table)]
    public void PickingABook_ShowsItsDetails(LibraryView view)
    {
        _preferences.Save(new AppPreferences(LibraryView: view));
        var cut = RenderHome();

        OpenBook(cut, "Mort");

        var panel = cut.Find(".book-panel");
        Assert.Contains("Terry Pratchett", panel.TextContent);
        Assert.Contains("Discworld #4", panel.TextContent);
        Assert.Contains("Fantasy", panel.TextContent);
        Assert.Contains("978-0", panel.TextContent);
        Assert.Contains("4 Mar 2026", panel.TextContent);
    }

    [Fact]
    public void Closing_KeepsTheFilters()
    {
        var cut = RenderHome();
        cut.Find("#filter-genre").Change("Fantasy");
        OpenBook(cut, "Mort");

        cut.Find("#panel-close").Click();

        Assert.Empty(cut.FindAll(".book-panel"));
        Assert.Equal(["Mort"], cut.FindAll(".book-title").Select(e => e.TextContent));
        Assert.Contains("Fantasy", cut.Find(".chip").TextContent);
    }

    [Fact]
    public void Escape_Closes()
    {
        var cut = RenderHome();
        OpenBook(cut, "Mort");

        cut.Find(".book-panel").KeyDown("Escape");

        Assert.Empty(cut.FindAll(".book-panel"));
    }

    [Fact]
    public void Files_AreListed_MissingOnesMarked_AndOnlyPresentOnesOpen()
    {
        var cut = RenderHome();
        OpenBook(cut, "Mort");

        var files = cut.FindAll(".file-list li");
        Assert.Equal(2, files.Count);
        Assert.Contains("mort.epub", files[0].TextContent);
        Assert.Contains("EPUB", files[0].TextContent);
        Assert.Contains("1.4 MB", files[0].TextContent);
        Assert.NotNull(files[0].QuerySelector(".file-open"));
        Assert.Contains("missing", files[1].TextContent);
        Assert.Null(files[1].QuerySelector(".file-open"));
        Assert.Null(files[1].QuerySelector(".file-folder"));
    }

    [Fact]
    public void Open_AndShowInFolder_UseTheLauncher()
    {
        var cut = RenderHome();
        OpenBook(cut, "Mort");

        cut.Find(".file-open").Click();
        cut.Find(".file-folder").Click();

        Assert.Equal(["/books/mort.epub"], _launcher.Opened);
        Assert.Equal(["/books/mort.epub"], _launcher.Shown);
        Assert.Empty(cut.FindAll(".panel-error"));
    }

    [Fact]
    public void LaunchFailure_IsShown()
    {
        _launcher.Works = false;
        var cut = RenderHome();
        OpenBook(cut, "Mort");

        cut.Find(".file-open").Click();

        Assert.Contains("Could not open mort.epub", cut.Find(".panel-error").TextContent);
    }

    // ---------- collections ----------

    [Fact]
    public void Collections_AreListed_AndExistingOnesSuggested()
    {
        _books[0] = _books[0] with { Collections = ["Favourites"] };
        _books[1] = _books[1] with { Collections = ["Book club"] };
        var cut = RenderHome();
        OpenBook(cut, "Mort");

        Assert.Equal(["Book club"], cut.FindAll(".panel-collections .chip").Select(c => c.TextContent.Replace("×", "").Trim()));
        Assert.Equal(["Book club", "Favourites"], cut.FindAll("#collection-suggestions option").Select(o => o.GetAttribute("value")));
    }

    [Fact]
    public void AddingACollection_ShowsIt_AndTheLibraryCanFilterByIt()
    {
        var cut = RenderHome();
        OpenBook(cut, "Mort");

        cut.Find("#collection-name").Input("Book club");
        cut.Find("#collection-add").Click();

        cut.WaitForAssertion(() => Assert.Contains("Book club", cut.Find(".panel-collections").TextContent));
        Assert.Equal("", cut.Find("#collection-name").GetAttribute("value"));
        cut.WaitForAssertion(() => Assert.Contains("Book club", cut.FindAll("#filter-collection option").Select(o => o.TextContent)));
    }

    [Fact]
    public void Enter_AddsTheCollection_BlankIsIgnored()
    {
        var cut = RenderHome();
        OpenBook(cut, "Mort");

        cut.Find("#collection-name").Input("   ");
        cut.Find("#collection-name").KeyDown("Enter");
        Assert.Empty(cut.FindAll(".panel-collections .chip"));

        cut.Find("#collection-name").Input("Favourites");
        cut.Find("#collection-name").KeyDown("Enter");
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".panel-collections .chip")));
    }

    [Fact]
    public void AddingACollection_Failure_IsShown()
    {
        _libraryBooks.FailUpdates = true;
        var cut = RenderHome();
        OpenBook(cut, "Mort");

        cut.Find("#collection-name").Input("Book club");
        cut.Find("#collection-add").Click();

        Assert.Contains("could not be changed", cut.Find(".panel-error").TextContent);
        Assert.Empty(cut.FindAll(".panel-collections .chip"));
    }

    [Fact]
    public void RemovingACollection_TakesTheBookOut()
    {
        _books[1] = _books[1] with { Collections = ["Book club"] };
        var cut = RenderHome();
        OpenBook(cut, "Mort");

        cut.Find(".panel-collections .chip").Click();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".panel-collections .chip")));
        Assert.Empty(_books[1].Collections);
    }

    // ---------- 3.2 edit ----------

    [Fact]
    public void Edit_Save_UpdatesPanelAndList()
    {
        var cut = RenderHome();
        OpenBook(cut, "Dune");

        cut.Find("#panel-edit").Click();
        cut.Find("#edit-authors").Change("Frank Herbert\nBrian Herbert");
        cut.Find("#edit-series").Change("Dune Chronicles");
        cut.Find("#edit-series-index").Change("1");
        cut.Find("#edit-genres").Change("Science Fiction\nClassics");
        cut.Find("form.edit-form").Submit();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("form.edit-form")));
        var (id, edit) = Assert.Single(_libraryBooks.Updates);
        Assert.Equal(1, id);
        Assert.Equal(["Frank Herbert", "Brian Herbert"], edit.Authors.Select(a => a.Trim()));
        Assert.Equal(["Science Fiction", "Classics"], edit.Genres.Select(g => g.Trim()));
        Assert.Equal(1m, edit.SeriesIndex);
        Assert.Contains("Dune Chronicles #1", cut.Find(".book-panel").TextContent);
        Assert.Contains("Edited by you", cut.Find(".book-panel").TextContent);
        cut.WaitForAssertion(() => Assert.Contains(
            "Frank Herbert, Brian Herbert", cut.FindAll(".book-authors").Select(e => e.TextContent)));
    }

    [Fact]
    public void Edit_EmptyTitle_IsRefused()
    {
        var cut = RenderHome();
        OpenBook(cut, "Dune");

        cut.Find("#panel-edit").Click();
        cut.Find("#edit-title").Change("   ");
        cut.Find("form.edit-form").Submit();

        Assert.Contains("needs a title", cut.Find(".form-error").TextContent);
        Assert.Empty(_libraryBooks.Updates);
    }

    [Fact]
    public void Edit_BadSeriesNumber_IsRefused()
    {
        var cut = RenderHome();
        OpenBook(cut, "Mort");

        cut.Find("#panel-edit").Click();
        cut.Find("#edit-series-index").Change("four");
        cut.Find("form.edit-form").Submit();

        Assert.Contains("number", cut.Find(".form-error").TextContent);
        Assert.Empty(_libraryBooks.Updates);
    }

    [Fact]
    public void Edit_GenreWithAComma_StaysOneGenre()
    {
        _books[1] = _books[1] with { Genres = ["Fiction, Historical"] };
        var cut = RenderHome();
        OpenBook(cut, "Mort");

        cut.Find("#panel-edit").Click();
        cut.Find("form.edit-form").Submit();

        Assert.Equal(["Fiction, Historical"], Assert.Single(_libraryBooks.Updates).Edit.Genres);
    }

    [Fact]
    public void Edit_SaveFails_KeepsTheFormAndSaysWhy()
    {
        _libraryBooks.FailUpdates = true;
        var cut = RenderHome();
        OpenBook(cut, "Mort");

        cut.Find("#panel-edit").Click();
        cut.Find("#edit-title").Change("Mort!");
        cut.Find("form.edit-form").Submit();

        Assert.Contains("could not be saved", cut.Find(".form-error").TextContent);
        Assert.Equal("Mort!", cut.Find("#edit-title").GetAttribute("value"));
    }

    [Fact]
    public void EscapeWhileEditing_CancelsTheEdit_NotThePanel()
    {
        var cut = RenderHome();
        OpenBook(cut, "Mort");

        cut.Find("#panel-edit").Click();
        cut.Find(".book-panel").KeyDown("Escape");

        Assert.Empty(cut.FindAll("form.edit-form"));
        Assert.Equal("Mort", cut.Find(".panel-title").TextContent);
    }

    [Fact]
    public void BookGone_SaysSo()
    {
        var cut = RenderHome();
        _books.RemoveAt(1); // e.g. a merge removed it after the list loaded

        cut.FindAll("button.book-title").Single(b => b.TextContent == "Mort").Click();

        cut.WaitForAssertion(() => Assert.Contains("no longer in the library", cut.Find(".book-panel").TextContent));
    }

    [Fact]
    public void Edit_Cancel_LeavesTheBookAlone()
    {
        var cut = RenderHome();
        OpenBook(cut, "Mort");

        cut.Find("#panel-edit").Click();
        Assert.Equal("Mort", cut.Find("#edit-title").GetAttribute("value"));
        Assert.Equal("Terry Pratchett", cut.Find("#edit-authors").GetAttribute("value"));
        Assert.Equal("4", cut.Find("#edit-series-index").GetAttribute("value"));
        cut.Find("#edit-title").Change("Changed");
        cut.Find("#edit-cancel").Click();

        Assert.Empty(_libraryBooks.Updates);
        Assert.Equal("Mort", cut.Find(".panel-title").TextContent);
    }
}
