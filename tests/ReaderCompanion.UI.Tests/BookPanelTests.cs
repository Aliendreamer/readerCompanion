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
    private readonly FakeReading _reading;
    private readonly FakeTrash _trash = new();
    private readonly FakeDriveLibrary _drive = new();
    private readonly FakeWebBrowser _browser = new();
    private readonly FakeDriveAccount _account = new()
    {
        Link = new ReaderCompanion.Core.Drive.DriveLink(ReaderCompanion.Core.Drive.DriveAccess.WholeDrive, null, DateTime.UtcNow),
    };

    public BookPanelTests()
    {
        _libraryBooks = new FakeBooks(_books);
        _reading = new FakeReading(_books);
        _libraryBooks.Files[2] =
        [
            new BookFileInfo("/books/mort.epub", BookFormat.Epub, 1_500_000, Present: true, Id: 21),
            new BookFileInfo("/old/mort.pdf", BookFormat.Pdf, 900, Present: false, Id: 22),
        ];
        _libraryBooks.Files[1] =
        [
            new BookFileInfo("gdrive://d1/Books/SF/dune.epub", BookFormat.Epub, 1000, Present: true, Id: 11) { DriveFileId = "d1" },
            new BookFileInfo("/books/dune.pdf", BookFormat.Pdf, 2000, Present: true, Id: 12),
        ];
        Services.AddAppFakes(preferences: _preferences, libraryBooks: _libraryBooks, launcher: _launcher, reading: _reading, trash: _trash,
            driveLibrary: _drive, browser: _browser, drive: _account);
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

    // ---------- the details pane ----------

    [Fact]
    public void DetailsPane_IsAlwaysThere_AndCloseClearsTheChoice()
    {
        var cut = RenderHome();
        Assert.Equal("Select a book to see its details", cut.Find(".workspace-details .details-empty").TextContent.Trim());

        OpenBook(cut, "Mort");
        Assert.Empty(cut.FindAll(".details-empty"));
        cut.Find("#panel-close").Click();

        Assert.Empty(cut.FindAll(".book-panel"));
        Assert.Single(cut.FindAll(".workspace-details .details-empty"));
    }

    [Fact]
    public void ClickingACover_OpensItsDetails_AndTheChosenBookIsMarked()
    {
        var cut = RenderHome();
        Assert.Empty(cut.FindAll(".book-card.selected"));

        cut.FindAll(".book-card").Single(c => c.QuerySelector(".book-title")!.TextContent == "Mort").QuerySelector(".book-card-cover")!.Click();

        cut.WaitForAssertion(() => Assert.Equal("Mort", cut.Find(".book-panel .panel-title").TextContent));
        Assert.Equal("Mort", Assert.Single(cut.FindAll(".book-card.selected")).QuerySelector(".book-title")!.TextContent);
        cut.Find("#panel-close").Click();
        Assert.Empty(cut.FindAll(".book-card.selected"));
    }

    [Fact]
    public void AChosenBook_StaysShown_WhenAFilterHidesIt()
    {
        var cut = RenderHome();
        OpenBook(cut, "Dune");

        FilterSteps.Pick(cut, "filter-genre", "Fantasy");

        Assert.DoesNotContain("Dune", cut.FindAll(".book-title").Select(t => t.TextContent).Where(t => t != cut.Find(".panel-title").TextContent));
        Assert.Equal("Dune", cut.Find(".book-panel .panel-title").TextContent);
    }

    // ---------- 3.1 view ----------

    [Fact]
    public void ADriveFile_IsShownAsOnDrive_OpensThroughADownload_AndShowsInDrive()
    {
        var cut = RenderHome();
        OpenBook(cut, "Dune");

        var drive = cut.FindAll(".file-list li")[0];
        Assert.Equal("dune.epub", drive.QuerySelector(".file-name")!.TextContent);
        Assert.Equal("Google Drive › Books/SF", drive.QuerySelector(".file-folder-path")!.TextContent);
        drive.QuerySelector(".file-open")!.Click();
        cut.WaitForAssertion(() => Assert.Equal(["/cache/11.epub"], _launcher.Opened));
        Assert.Equal([11], _drive.Downloaded);

        cut.FindAll(".file-list li")[0].QuerySelector(".file-folder")!.Click();
        Assert.Equal("https://drive.google.com/file/d/d1/view", Assert.Single(_browser.Opened).AbsoluteUri);
    }

    [Fact]
    public void ADriveDownload_ShowsItsPercentage()
    {
        _drive.DownloadHeld = new TaskCompletionSource<string>();
        var cut = RenderHome();
        OpenBook(cut, "Dune");

        cut.FindAll(".file-list li")[0].QuerySelector(".file-open")!.Click();

        cut.WaitForAssertion(() => Assert.Equal("Downloading… 45%", cut.FindAll(".file-list li")[0].QuerySelector(".file-open")!.TextContent));
        cut.InvokeAsync(() => _drive.DownloadHeld.SetResult("/cache/11.epub"));
        cut.WaitForAssertion(() => Assert.Equal(["/cache/11.epub"], _launcher.Opened));
        Assert.Equal("Open", cut.FindAll(".file-list li")[0].QuerySelector(".file-open")!.TextContent);
    }

    [Fact]
    public void ADriveFile_GoesToDrivesTrash_OrSaysAccessIsReadOnly()
    {
        var cut = RenderHome();
        OpenBook(cut, "Dune");

        cut.FindAll(".file-list li")[0].QuerySelector(".file-trash")!.Click();
        Assert.Contains("Google Drive trash", cut.Find(".file-trash-question").TextContent);
        _libraryBooks.TrashAnswer = TrashResult.ReadOnly;
        cut.Find(".file-trash-confirm").Click();

        cut.WaitForAssertion(() => Assert.Contains("read only", cut.Find(".book-panel [role=alert]").TextContent));
    }

    [Fact]
    public void ADriveFile_OffersNoTrash_WithReadOnlyAccess()
    {
        _account.Link = new ReaderCompanion.Core.Drive.DriveLink(ReaderCompanion.Core.Drive.DriveAccess.WholeDriveReadOnly, null, DateTime.UtcNow);
        var cut = RenderHome();
        OpenBook(cut, "Dune");

        Assert.Null(cut.FindAll(".file-list li")[0].QuerySelector(".file-trash"));
        Assert.NotNull(cut.FindAll(".file-list li")[1].QuerySelector(".file-trash")); // the local copy still can
    }

    [Fact]
    public void ADriveFile_ThatCannotBeDownloaded_SaysWhy()
    {
        _drive.DownloadFails = new ReaderCompanion.Core.Drive.DriveLinkLostException();
        var cut = RenderHome();
        OpenBook(cut, "Dune");

        cut.FindAll(".file-list li")[0].QuerySelector(".file-open")!.Click();

        cut.WaitForAssertion(() => Assert.Contains("needs linking again", cut.Find(".book-panel [role=alert]").TextContent));
        Assert.Empty(_launcher.Opened);
    }

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
        FilterSteps.Pick(cut, "filter-genre", "Fantasy");
        OpenBook(cut, "Mort");

        cut.Find("#panel-close").Click();

        Assert.Empty(cut.FindAll(".book-panel"));
        Assert.Equal(["Mort"], cut.FindAll(".book-title").Select(e => e.TextContent));
        Assert.Equal(["Fantasy"], FilterSteps.Ticked(cut, "filter-genre"));
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

    // ---------- trash ----------

    [Fact]
    public void ExtraFile_CanBeTrashed_AfterConfirming()
    {
        _libraryBooks.Files[2] =
        [
            new BookFileInfo("/books/mort.epub", BookFormat.Epub, 10, Present: true, Id: 21),
            new BookFileInfo("/books/mort.pdf", BookFormat.Pdf, 10, Present: true, Id: 22),
        ];
        var cut = RenderHome();
        OpenBook(cut, "Mort");

        cut.FindAll(".file-trash")[1].Click();
        Assert.Contains("Move mort.pdf to the trash?", cut.Find(".file-list").TextContent);
        Assert.Empty(_trash.Trashed);
        cut.Find(".file-trash-confirm").Click();

        Assert.Equal(["/books/mort.pdf"], _trash.Trashed);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".file-list li")));
        Assert.Empty(cut.FindAll(".file-trash")); // the last file cannot be trashed
    }

    [Fact]
    public void SingleFile_OffersNoTrash()
    {
        _libraryBooks.Files[2] = [new BookFileInfo("/books/mort.epub", BookFormat.Epub, 10, Present: true, Id: 21)];
        var cut = RenderHome();
        OpenBook(cut, "Mort");

        Assert.Empty(cut.FindAll(".file-trash"));
    }

    [Fact]
    public void TrashFailure_IsShown_AndTheFileStays()
    {
        _trash.Works = false;
        _libraryBooks.Files[2] =
        [
            new BookFileInfo("/books/mort.epub", BookFormat.Epub, 10, Present: true, Id: 21),
            new BookFileInfo("/books/mort.pdf", BookFormat.Pdf, 10, Present: true, Id: 22),
        ];
        var cut = RenderHome();
        OpenBook(cut, "Mort");

        cut.FindAll(".file-trash")[1].Click();
        cut.Find(".file-trash-confirm").Click();

        Assert.Contains("could not be moved to the trash", cut.Find(".panel-error").TextContent);
        Assert.Equal(2, cut.FindAll(".file-list li").Count);
    }

    // ---------- reading ----------

    [Fact]
    public void Status_IsSet_FromThePanel_AndShowsInTheList()
    {
        var cut = RenderHome();
        OpenBook(cut, "Mort");
        Assert.Equal("true", cut.Find("#status-NotStarted").GetAttribute("aria-pressed"));

        cut.Find("#status-Reading").Click();

        cut.WaitForAssertion(() => Assert.Equal("true", cut.Find("#status-Reading").GetAttribute("aria-pressed")));
        Assert.Equal(DateOnly.FromDateTime(DateTime.Now), _reading.LastToday);
        Assert.Single(cut.FindAll(".session-list li"));
        cut.WaitForAssertion(() => Assert.Contains("Reading", cut.FindAll(".status-badge").Select(b => b.TextContent)));
    }

    [Fact]
    public void Rating_StarsAndClear()
    {
        var cut = RenderHome();
        OpenBook(cut, "Mort");

        cut.Find("#star-4").Click();
        cut.WaitForAssertion(() => Assert.Equal("true", cut.Find("#star-4").GetAttribute("aria-pressed")));
        Assert.Equal(4, _books[1].Rating);

        cut.Find("#rating-clear").Click();
        cut.WaitForAssertion(() => Assert.Null(_books[1].Rating));
    }

    [Fact]
    public async Task Notes_AreSavedOnChange()
    {
        var cut = RenderHome();
        OpenBook(cut, "Mort");

        cut.Find("#reading-notes").Change("Death takes an apprentice.");

        cut.WaitForAssertion(() => Assert.Equal("Death takes an apprentice.", cut.Find("#reading-notes").GetAttribute("value")));
        Assert.Equal("Death takes an apprentice.", (await _reading.GetAsync(2, Xunit.TestContext.Current.CancellationToken))!.Notes);
    }

    [Fact]
    public void Sessions_CanBeCorrected_Refused_AndDeleted()
    {
        _reading.AddSession(2, new DateOnly(2026, 2, 1), new DateOnly(2026, 3, 5));
        var cut = RenderHome();
        OpenBook(cut, "Mort");

        cut.Find(".session-finished").Change("2026-02-12");
        cut.WaitForAssertion(() => Assert.Equal(new DateOnly(2026, 2, 12), _reading.GetAsync(2, Xunit.TestContext.Current.CancellationToken).Result!.Sessions[0].FinishedOn));

        cut.Find(".session-finished").Change("2026-01-01");
        Assert.Contains("cannot finish before", cut.Find(".panel-error").TextContent);
        Assert.Equal("2026-02-12", cut.Find(".session-finished").GetAttribute("value"));

        cut.Find(".session-delete").Click();
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".session-list li")));
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
        cut.WaitForAssertion(() => Assert.Contains("Book club", FilterSteps.Options(cut, "filter-collection")));
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
