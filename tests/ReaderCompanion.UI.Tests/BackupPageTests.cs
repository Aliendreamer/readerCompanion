using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Preferences;
using ReaderCompanion.UI.Pages;

namespace ReaderCompanion.UI.Tests;

public class BackupPageTests : BunitContext
{
    private readonly FakeBackups _backups = new();
    private readonly FakePreferences _preferences = new();
    private readonly FakePicker _picker = new("/home/me/backups");
    private readonly FakeScanner _scanner = new();

    public BackupPageTests() =>
        Services.AddAppFakes(preferences: _preferences, backups: _backups, picker: _picker, scanner: _scanner);

    private void WithFolder() => _preferences.Save(_preferences.Current with { BackupFolder = "/home/me/backups" });

    private void AddExisting() =>
        _backups.Existing.Add(new BackupInfo("/home/me/backups/readerCompanion-2026-11-19-090000.db", new DateTime(2026, 11, 19, 9, 0, 0), 4096, false));

    [Fact]
    public void NoFolder_BackUpNowIsDisabled_ChoosingOneRemembersIt()
    {
        var cut = Render<BackupPage>();
        Assert.True(cut.Find("#backup-now").HasAttribute("disabled"));

        cut.Find("#backup-choose").Click();

        cut.WaitForAssertion(() => Assert.Equal("/home/me/backups", _preferences.Current.BackupFolder));
        Assert.False(cut.Find("#backup-now").HasAttribute("disabled"));
    }

    [Fact]
    public void BackUpNow_AddsTheBackupToTheList()
    {
        WithFolder();
        var cut = Render<BackupPage>();

        cut.Find("#backup-now").Click();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".backup-list li")));
        Assert.Contains("Backed up", cut.Find(".backup-message").TextContent);
    }

    [Fact]
    public void Restore_AsksFirst_ThenRestoresAndReloadsTheApp()
    {
        WithFolder();
        AddExisting();
        var cut = Render<BackupPage>();

        cut.Find(".backup-restore").Click();
        Assert.Contains("backed up first", cut.Find(".backup-list").TextContent);
        Assert.Empty(_backups.Restored);

        cut.Find(".backup-confirm").Click();

        cut.WaitForAssertion(() => Assert.Single(_backups.Restored));
        var navigation = Services.GetRequiredService<BunitNavigationManager>();
        Assert.True(navigation.History.First().Options.ForceLoad);
    }

    [Fact]
    public void RestoreRefused_IsShown()
    {
        WithFolder();
        AddExisting();
        _backups.RestoreFailure = new InvalidDataException("readerCompanion-2026-11-19-090000.db is not a readerCompanion backup.");
        var cut = Render<BackupPage>();

        cut.Find(".backup-restore").Click();
        cut.Find(".backup-confirm").Click();

        Assert.Contains("is not a readerCompanion backup", cut.Find(".panel-error").TextContent);
    }

    [Fact]
    public void RestoreIsDisabled_WhileAScanRuns()
    {
        WithFolder();
        AddExisting();
        _ = _scanner.ScanAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken);

        var cut = Render<BackupPage>();

        Assert.True(cut.Find(".backup-restore").HasAttribute("disabled"));
    }

    [Fact]
    public void OnExitSettings_AreSaved()
    {
        WithFolder();
        var cut = Render<BackupPage>();

        cut.Find("#backup-on-exit").Change(true);
        cut.Find("#backup-keep").Change("5");

        Assert.True(_preferences.Current.BackupOnExit);
        Assert.Equal(5, _preferences.Current.KeepBackups);
    }
}
