using ReaderCompanion.Core.Organising;
using ReaderCompanion.UI.Pages;

namespace ReaderCompanion.UI.Tests;

public class OrganisePageTests : BunitContext
{
    private readonly FakeOrganiser _organiser = new();
    private readonly FakeFolders _folders = new();
    private readonly FakeDriveAccount _drive = new();
    private readonly FakeDriveLibrary _driveLibrary = new();

    public OrganisePageTests()
    {
        _folders.Linked.Add("/books");
        _driveLibrary.Sources.Add(new ReaderCompanion.Core.Drive.DriveSource
        {
            Id = 1,
            DriveId = "books",
            Name = "Books",
            Kind = ReaderCompanion.Core.Drive.DriveSourceKind.Folder,
        });
        Services.AddAppFakes(folders: _folders, organiser: _organiser, drive: _drive, driveLibrary: _driveLibrary);
    }

    [Fact]
    public void DriveFolders_AreOffered_WithReadAndWriteAccess_AndPreviewedRelativeToTheFolder()
    {
        _drive.Link = new ReaderCompanion.Core.Drive.DriveLink(ReaderCompanion.Core.Drive.DriveAccess.WholeDrive, null, DateTime.UtcNow);
        _organiser.Planned.Add(new Move(1, "gdrive://f1/Books/hobbit.epub", "gdrive://f1/Books/Fantasy/The Hobbit.epub"));
        var cut = RenderPage();

        Assert.Equal(["/books", "Google Drive › Books"], cut.FindAll("#organise-folder option").Select(o => o.TextContent));
        cut.Find("#organise-folder").Change("gdrive://books/Books");
        cut.Find("#organise-preview").Click();

        cut.WaitForAssertion(() => Assert.Equal("Fantasy", cut.Find(".move-folder").TextContent));
        Assert.Equal("The Hobbit.epub", cut.Find(".move-name").TextContent);
        Assert.Equal("now: hobbit.epub", cut.Find(".move-from").TextContent);
    }

    [Fact]
    public void DriveFolders_AreNotOffered_WithoutWriteAccessToTheWholeDrive()
    {
        _drive.Link = new ReaderCompanion.Core.Drive.DriveLink(ReaderCompanion.Core.Drive.DriveAccess.WholeDriveReadOnly, null, DateTime.UtcNow);

        var cut = RenderPage();

        Assert.Equal(["/books"], cut.FindAll("#organise-folder option").Select(o => o.TextContent));
    }

    private IRenderedComponent<OrganisePage> RenderPage()
    {
        var cut = Render<OrganisePage>();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("#organise-preview")));
        return cut;
    }

    [Fact]
    public void NoLinkedFolder_PointsToSources()
    {
        _folders.Linked.Clear();

        var cut = Render<OrganisePage>();

        cut.WaitForAssertion(() => Assert.Contains("Link a folder", cut.Markup));
    }

    [Fact]
    public void Preview_ShowsWhereEachFileGoes_AndWhereItIsNow_RelativeToTheFolder()
    {
        _organiser.Planned.Add(new Move(1, "/books/downloads/hobbit.epub", "/books/Fantasy/J.R.R. Tolkien/The Hobbit.epub"));
        var cut = RenderPage();
        Assert.Empty(cut.FindAll("#organise-layout")); // one layout: nothing to choose

        cut.Find("#organise-preview").Click();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".organise-moves tbody tr")));
        Assert.Equal("Fantasy › J.R.R. Tolkien", cut.Find(".move-folder").TextContent);
        Assert.Equal("The Hobbit.epub", cut.Find(".move-name").TextContent);
        Assert.Equal("now: downloads/hobbit.epub", cut.Find(".move-from").TextContent.Replace('\\', '/'));
        Assert.DoesNotContain("/books/", cut.Find(".organise-moves tbody").TextContent);
    }

    [Fact]
    public void NothingToMove_SaysSo()
    {
        var cut = RenderPage();

        cut.Find("#organise-preview").Click();

        cut.WaitForAssertion(() => Assert.Contains("Nothing needs to move", cut.Markup));
    }

    [Fact]
    public void Apply_MovesOnlyTheTickedFiles_AndOffersUndo()
    {
        _organiser.Planned.Add(new Move(1, "/books/a.epub", "/books/A/a.epub"));
        _organiser.Planned.Add(new Move(2, "/books/b.epub", "/books/B/b.epub"));
        var cut = RenderPage();
        cut.Find("#organise-preview").Click();

        cut.FindAll(".move-include")[1].Change(false);
        Assert.Contains("Move 1 file", cut.Find("#organise-apply").TextContent);
        cut.Find("#organise-apply").Click();

        cut.WaitForAssertion(() => Assert.Contains("Moved 1 file", cut.Find(".organise-result").TextContent));
        Assert.Equal([1], Assert.Single(_organiser.Applied).Select(m => m.FileId));
        Assert.NotNull(cut.Find("#organise-undo"));
    }

    [Fact]
    public void Undo_ReportsSkippedFiles()
    {
        _organiser.Last = new LastOrganise(DateTime.UtcNow, "/books", 2);
        var cut = RenderPage();

        cut.Find("#organise-undo").Click();

        cut.WaitForAssertion(() => Assert.Contains("Moved 1 file back", cut.Find(".organise-result").TextContent));
        Assert.Contains("A.epub", cut.Find(".organise-failures").TextContent);
        Assert.Empty(cut.FindAll("#organise-undo"));
    }
}
