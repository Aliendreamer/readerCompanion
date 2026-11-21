using Microsoft.Extensions.DependencyInjection;
using ReaderCompanion.Core.Drive;
using ReaderCompanion.Core.Preferences;
using ReaderCompanion.UI.Library;

namespace ReaderCompanion.UI.Tests;

public class DriveSourceTests : BunitContext
{
    private readonly FakeDriveAccount _drive = new();
    private readonly FakePreferences _preferences = new(new AppPreferences(DriveAccess: DriveAccess.WholeDrive));

    public DriveSourceTests() => Services.AddAppFakes(preferences: _preferences, drive: _drive);

    [Fact]
    public void NotSetUp_SaysSo_AndOffersNoLink()
    {
        _drive.IsConfigured = false;

        var cut = Render<DriveSource>();

        Assert.Contains("not set up in this build", cut.Find(".drive-source").TextContent);
        Assert.Empty(cut.FindAll("#drive-link"));
    }

    [Fact]
    public void Link_UsesTheChosenAccess_AndShowsWhoIsLinked()
    {
        var cut = Render<DriveSource>();
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
        var cut = Render<DriveSource>();

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
        var cut = Render<DriveSource>();

        cut.Find("#drive-link").Click();

        cut.WaitForAssertion(() => Assert.Equal("Access to Google Drive was not given.", cut.Find("[role=alert]").TextContent));
        Assert.NotEmpty(cut.FindAll("#drive-link"));
    }

    [Fact]
    public void GoogleNotAnsweringInTime_IsSaid()
    {
        _drive.NextLink = new TaskCompletionSource<DriveLink>();
        _drive.NextLink.SetException(new TaskCanceledException("timeout"));
        var cut = Render<DriveSource>();

        cut.Find("#drive-link").Click();

        cut.WaitForAssertion(() => Assert.Contains("did not answer in time", cut.Find("[role=alert]").TextContent));
    }

    [Fact]
    public void Unlink_ForgetsTheLink()
    {
        _drive.Link = new DriveLink(DriveAccess.PickedFiles, null, DateTime.UtcNow);
        var cut = Render<DriveSource>();
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
        var cut = Render<DriveSource>();

        cut.InvokeAsync(_drive.Lose);

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("#drive-link")));
    }
}
