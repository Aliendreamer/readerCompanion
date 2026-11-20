using ReaderCompanion.UI.Pages;

namespace ReaderCompanion.UI.Tests;

public class SettingsPageTests : BunitContext
{
    private readonly FakePreferences _preferences = new();
    private readonly FakeFolders _folders = new();
    private readonly FakeLauncher _launcher = new();

    public SettingsPageTests() => Services.AddAppFakes(preferences: _preferences, folders: _folders, launcher: _launcher);

    [Fact]
    public void ShowsTheDataFolder_AndOpensIt()
    {
        var cut = Render<SettingsPage>();

        Assert.Contains("/home/me/.local/share/ReaderCompanion", cut.Find(".settings-data").TextContent);
        cut.Find("#settings-show-data").Click();

        Assert.Equal("/home/me/.local/share/ReaderCompanion/library.db", Assert.Single(_launcher.Shown));
    }

    [Fact]
    public void SummarisesBackupsAndSources_WithLinks()
    {
        _preferences.Save(_preferences.Current with
        {
            BackupFolder = "/home/me/backups",
            BackupOnExit = true,
            GoodreadsLastImportUtc = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc),
        });
        _folders.Linked.AddRange(["/books", "/more"]);

        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains("2 local folders", cut.Find(".settings-sources").TextContent));
        Assert.Contains("1 Aug 2026", cut.Find(".settings-sources").TextContent);
        Assert.Contains("/home/me/backups", cut.Find(".settings-backup").TextContent);
        Assert.Contains("on close", cut.Find(".settings-backup").TextContent);
        Assert.NotNull(cut.Find(".settings-backup a[href='backup']"));
        Assert.NotNull(cut.Find(".settings-sources a[href='sources']"));
    }

    [Fact]
    public void ShowInFolderFailure_IsShown()
    {
        _launcher.Works = false;
        var cut = Render<SettingsPage>();

        cut.Find("#settings-show-data").Click();

        Assert.Contains("could not be shown", cut.Find(".panel-error").TextContent);
    }

    [Fact]
    public void MissingBackupFolder_IsPointedOut()
    {
        _preferences.Save(_preferences.Current with { BackupFolder = "/no/such/folder" });

        var cut = Render<SettingsPage>();

        Assert.Contains("folder not found", cut.Find(".settings-backup").TextContent);
    }

    [Fact]
    public void NothingSetYet_SaysSo()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains("No backup folder", cut.Find(".settings-backup").TextContent));
        Assert.Contains("never", cut.Find(".settings-sources").TextContent);
    }
}
