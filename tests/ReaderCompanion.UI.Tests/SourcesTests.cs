using Microsoft.Extensions.DependencyInjection;
using ReaderCompanion.Core.Scanning;
using ReaderCompanion.UI.Pages;

namespace ReaderCompanion.UI.Tests;

public class SourcesTests : BunitContext
{
    private readonly FakeFolders _folders = new();
    private readonly FakeScanner _scanner = new();

    private IRenderedComponent<Sources> RenderWith(FakePicker picker)
    {
        Services.AddSingleton<ILibraryFolders>(_folders);
        Services.AddSingleton<ILibraryScanner>(_scanner);
        Services.AddSingleton<IFolderPicker>(picker);
        Services.AddSingleton<ReaderCompanion.Core.Goodreads.IGoodreadsImport>(new FakeGoodreads());
        Services.AddSingleton<IFilePicker>(new FakeFilePicker());
        Services.AddSingleton<ReaderCompanion.Core.Preferences.IAppPreferences>(new FakePreferences());
        Services.AddSingleton<ReaderCompanion.Core.Drive.IDriveAccount>(new FakeDriveAccount());
        Services.AddSingleton<ReaderCompanion.Core.Drive.IDriveLibrary>(new FakeDriveLibrary());
        Services.AddSingleton<ScanStatus>();
        Services.AddSingleton<IInView>(new FakeInView());
        return Render<Sources>();
    }

    [Fact]
    public void Title_IsForScreenReadersOnly()
    {
        var cut = RenderWith(new FakePicker());

        Assert.Equal("Sources", cut.Find("h1.visually-hidden").TextContent);
        Assert.Empty(cut.FindAll(".page-header"));
    }

    [Fact]
    public void LinkedFolders_AreListed()
    {
        _folders.Linked.AddRange(["/books", "/more"]);

        var cut = RenderWith(new FakePicker());

        Assert.Equal(["/books", "/more"], cut.FindAll(".folder-path").Select(e => e.TextContent));
    }

    [Fact]
    public void LinkFolder_OpensPicker_AndListsChosenFolder()
    {
        var picker = new FakePicker("/books");
        var cut = RenderWith(picker);

        cut.Find("#link-folder").Click();

        cut.WaitForAssertion(() => Assert.Equal(["/books"], cut.FindAll(".folder-path").Select(e => e.TextContent)));
        Assert.Equal(1, picker.Calls);

        // The new folder is scanned at once.
        cut.WaitForAssertion(() => Assert.True(_scanner.IsScanning));
        Assert.NotEmpty(cut.FindAll(".progress"));
        cut.InvokeAsync(() => _scanner.Finish(new ScanSummary { Added = 2 }));
        cut.WaitForAssertion(() => Assert.Contains("2 added", cut.Find(".summary").TextContent));
    }

    [Fact]
    public void LinkFolder_PickerCancelled_ChangesNothing()
    {
        var cut = RenderWith(new FakePicker((string?)null));

        cut.Find("#link-folder").Click();

        Assert.Equal(0, _folders.LinkCalls);
        Assert.Empty(cut.FindAll(".folder-path"));
        Assert.Empty(cut.FindAll(".message"));
    }

    [Fact]
    public void LinkFolder_Refused_ShowsReason()
    {
        _folders.NextResult = new LinkFolderResult(false, "This folder is already linked.");
        var cut = RenderWith(new FakePicker("/books"));

        cut.Find("#link-folder").Click();

        cut.WaitForAssertion(() => Assert.Contains("This folder is already linked.", cut.Find(".message").TextContent));
        Assert.False(_scanner.IsScanning);
    }

    [Fact]
    public void Unlink_RemovesFolder()
    {
        _folders.Linked.Add("/books");
        var cut = RenderWith(new FakePicker());

        cut.Find(".unlink").Click();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".folder-path")));
    }

    [Fact]
    public void Scan_ShowsProgress_ThenSummary_AndIsDisabledWhileRunning()
    {
        _folders.Linked.Add("/books");
        var cut = RenderWith(new FakePicker());

        cut.Find("#scan").Click();
        cut.InvokeAsync(() => _scanner.Report(1, 2));

        cut.WaitForAssertion(() => Assert.Contains("1 of 2 files", cut.Find(".progress").TextContent));
        Assert.True(cut.Find("#scan").HasAttribute("disabled"));

        cut.InvokeAsync(() => _scanner.Finish(new ScanSummary
        {
            Added = 3,
            Moved = 1,
            MarkedMissing = 2,
            Unreadable = [new UnreadableFile("/books/broken.epub", "bad zip")],
            UnavailableFolders = ["/usb/books"],
        }));

        cut.WaitForAssertion(() =>
        {
            var summary = cut.Find(".summary").TextContent;
            Assert.Contains("3 added", summary);
            Assert.Contains("1 moved", summary);
            Assert.Contains("2 marked missing", summary);
            Assert.Contains("/books/broken.epub", summary);
            Assert.Contains("bad zip", summary); // why it could not be read
            Assert.Contains("/usb/books", summary);
        });
        Assert.False(cut.Find("#scan").HasAttribute("disabled"));
        Assert.Empty(cut.FindAll(".progress"));
    }

    [Fact]
    public void LeavingAndComingBack_DuringAScan_ShowsItsProgress_ThenItsSummary()
    {
        _folders.Linked.Add("/books");
        var first = RenderWith(new FakePicker());
        first.Find("#scan").Click();
        first.InvokeAsync(() => _scanner.Report(12, 40));
        first.Dispose(); // the user opens another page

        var back = Render<Sources>();

        back.WaitForAssertion(() => Assert.Contains("12 of 40 files", back.Find(".progress").TextContent));
        Assert.True(back.Find("#scan").HasAttribute("disabled"));
        back.InvokeAsync(() => _scanner.Finish(new ScanSummary { Added = 5 }));
        back.WaitForAssertion(() => Assert.Contains("5 added", back.Find(".summary").TextContent));
    }
}
