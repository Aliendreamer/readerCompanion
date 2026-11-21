using Microsoft.Extensions.DependencyInjection;
using ReaderCompanion.Core.Drive;
using ReaderCompanion.Core.Preferences;
using ReaderCompanion.UI.Library;

namespace ReaderCompanion.UI.Tests;

public class GoogleDriveSourceTests : BunitContext
{
    private readonly FakeDriveAccount _drive = new();
    private readonly FakePreferences _preferences = new(new AppPreferences(DriveAccess: DriveAccess.WholeDrive));
    private readonly FakeDriveLibrary _library = new();

    public GoogleDriveSourceTests() => Services.AddAppFakes(preferences: _preferences, drive: _drive, driveLibrary: _library);

    private IRenderedComponent<GoogleDriveSource> RenderLinked(DriveAccess access)
    {
        _drive.Link = new DriveLink(access, "Ana", DateTime.UtcNow);
        return Render<GoogleDriveSource>();
    }

    [Fact]
    public void WholeDrive_AFolderIsBrowsedTo_AndAdded()
    {
        var cut = RenderLinked(DriveAccess.WholeDrive);
        Assert.Empty(cut.FindAll("#drive-pick"));

        cut.Find("#drive-add-folder").Click();
        cut.WaitForAssertion(() => Assert.True(cut.Find("#drive-choose").HasAttribute("disabled"))); // not My Drive itself
        cut.FindAll(".drive-folder").Single(b => b.TextContent == "Books").Click();
        cut.WaitForAssertion(() => Assert.Contains("My Drive / Books", cut.Find(".drive-browser").TextContent));
        cut.Find("#drive-choose").Click();

        cut.WaitForAssertion(() => Assert.Equal(["Books"], cut.FindAll(".drive-source-name").Select(e => e.TextContent)));
        Assert.Equal("books", _library.Sources.Single().DriveId);
        Assert.Empty(cut.FindAll(".drive-browser"));
        cut.WaitForAssertion(() => Assert.Contains("1 added", cut.Find(".drive-source .summary").TextContent)); // scanned at once
        Assert.Equal(1, _library.Scans);
    }

    [Fact]
    public void RefusedFolder_SaysWhy_AndDoesNotScan()
    {
        _library.RefuseAdd = "This Drive folder is already added.";
        var cut = RenderLinked(DriveAccess.WholeDrive);

        cut.Find("#drive-add-folder").Click();
        cut.WaitForAssertion(() => cut.FindAll(".drive-folder").Single(b => b.TextContent == "Books").Click());
        cut.WaitForAssertion(() => Assert.False(cut.Find("#drive-choose").HasAttribute("disabled")));
        cut.Find("#drive-choose").Click();

        cut.WaitForAssertion(() => Assert.Contains("already added", cut.Find(".drive-source [role=alert]").TextContent));
        Assert.Equal(0, _library.Scans);
    }

    [Fact]
    public void PickedFiles_AreAddedWithGooglesPicker_AndRemovable()
    {
        var cut = RenderLinked(DriveAccess.PickedFiles);
        Assert.Empty(cut.FindAll("#drive-add-folder"));

        cut.Find("#drive-pick").Click();
        cut.WaitForAssertion(() => Assert.Equal(["Picked.epub"], cut.FindAll(".drive-source-name").Select(e => e.TextContent)));
        cut.WaitForAssertion(() => Assert.Contains("1 added", cut.Find(".drive-source .summary").TextContent)); // scanned at once
        cut.Find(".drive-source-remove").Click();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".drive-source-name")));
        Assert.Empty(_library.Sources);
    }

    [Fact]
    public void ScanDrive_ShowsTheSummary_OrWhyItStopped()
    {
        _library.Sources.Add(new DriveSource { Id = 1, DriveId = "books", Name = "Books", Kind = DriveSourceKind.Folder });
        var cut = RenderLinked(DriveAccess.WholeDrive);

        cut.Find("#drive-scan").Click();
        cut.WaitForAssertion(() => Assert.Contains("1 added", cut.Find(".drive-source .summary").TextContent));

        _library.ScanFails = new DriveUnavailableException("“Books” could not be read on Google Drive, so nothing was changed.");
        cut.Find("#drive-scan").Click();
        cut.WaitForAssertion(() => Assert.Contains("“Books” could not be read", cut.Find(".drive-source [role=alert]").TextContent));
    }

    [Fact]
    public void ScanDrive_NeedsASource()
    {
        var cut = RenderLinked(DriveAccess.WholeDrive);

        Assert.True(cut.Find("#drive-scan").HasAttribute("disabled"));
    }

    [Fact]
    public void NotSetUp_SaysSo_AndOffersNoLink()
    {
        _drive.IsConfigured = false;

        var cut = Render<GoogleDriveSource>();

        Assert.Contains("not set up in this build", cut.Find(".drive-source").TextContent);
        Assert.Empty(cut.FindAll("#drive-link"));
    }

    [Fact]
    public void Link_UsesTheChosenAccess_AndShowsWhoIsLinked()
    {
        var cut = Render<GoogleDriveSource>();
        Assert.Contains("Whole Drive, read and write", cut.Find(".drive-source").TextContent);

        cut.Find("#drive-link").Click();

        cut.WaitForAssertion(() => Assert.Contains("Linked as Ana (ana@example.com)", cut.Find(".drive-source").TextContent));
        Assert.Equal([DriveAccess.WholeDrive], _drive.LinkedWith);
        Assert.Empty(cut.FindAll("#drive-link"));
    }

    [Fact]
    public void WhileSigningIn_TheUserCanCancel()
    {
        _drive.NextLink = new TaskCompletionSource<DriveLink>();
        var cut = Render<GoogleDriveSource>();

        cut.Find("#drive-link").Click();
        Assert.Contains("Finish signing in in your browser", cut.Find(".drive-source").TextContent);
        cut.Find("#drive-cancel").Click();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("#drive-link")));
        Assert.Empty(cut.FindAll("[role=alert]"));
        Assert.Null(_drive.Link);
    }

    [Fact]
    public void Refused_SaysWhy()
    {
        _drive.NextLink = new TaskCompletionSource<DriveLink>();
        _drive.NextLink.SetException(new DriveSignInException("Access to Google Drive was not given."));
        var cut = Render<GoogleDriveSource>();

        cut.Find("#drive-link").Click();

        cut.WaitForAssertion(() => Assert.Equal("Access to Google Drive was not given.", cut.Find("[role=alert]").TextContent));
        Assert.NotEmpty(cut.FindAll("#drive-link"));
    }

    [Fact]
    public void GoogleNotAnsweringInTime_IsSaid()
    {
        _drive.NextLink = new TaskCompletionSource<DriveLink>();
        _drive.NextLink.SetException(new TaskCanceledException("timeout"));
        var cut = Render<GoogleDriveSource>();

        cut.Find("#drive-link").Click();

        cut.WaitForAssertion(() => Assert.Contains("did not answer in time", cut.Find("[role=alert]").TextContent));
    }

    [Fact]
    public void Unlink_ForgetsTheLink()
    {
        _drive.Link = new DriveLink(DriveAccess.PickedFiles, null, DateTime.UtcNow);
        var cut = Render<GoogleDriveSource>();
        Assert.Contains("Linked as your Google account", cut.Find(".drive-source").TextContent);
        Assert.Contains("Only books I pick", cut.Find(".drive-source").TextContent);

        cut.Find("#drive-unlink").Click();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("#drive-link")));
        Assert.Equal(1, _drive.Unlinks);
    }

    [Fact]
    public void LinkLostElsewhere_IsShown()
    {
        _drive.Link = new DriveLink(DriveAccess.PickedFiles, "Ana", DateTime.UtcNow);
        var cut = Render<GoogleDriveSource>();

        cut.InvokeAsync(_drive.Lose);

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("#drive-link")));
    }
}
