using Microsoft.Extensions.DependencyInjection;
using ReaderCompanion.Core.Preferences;

namespace ReaderCompanion.Data.Tests;

public sealed class AppPreferencesTests : IDisposable
{
    private readonly TempLibrary _library = new();

    private string PreferencesFile => Path.Combine(Path.GetDirectoryName(_library.DatabasePath)!, "preferences.json");

    [Fact]
    public void NoFile_GivesDefaults()
    {
        var preferences = _library.Services.GetRequiredService<IAppPreferences>();

        Assert.Equal(new AppPreferences(ThemeChoice.System, LibraryView.Grid), preferences.Current);
    }

    [Fact]
    public void Saved_IsReadBackByANewInstance()
    {
        _library.Services.GetRequiredService<IAppPreferences>().Save(new AppPreferences(ThemeChoice.Dark, LibraryView.Table));

        using var restarted = new ServiceCollection().AddLibraryData(_library.DatabasePath).BuildServiceProvider();

        Assert.Equal(
            new AppPreferences(ThemeChoice.Dark, LibraryView.Table),
            restarted.GetRequiredService<IAppPreferences>().Current);
    }

    [Fact]
    public void Save_UpdatesCurrent_AndRaisesChanged()
    {
        var preferences = _library.Services.GetRequiredService<IAppPreferences>();
        var raised = 0;
        preferences.Changed += () => raised++;

        preferences.Save(preferences.Current with { Theme = ThemeChoice.Light });

        Assert.Equal(ThemeChoice.Light, preferences.Current.Theme);
        Assert.Equal(1, raised);
    }

    [Fact]
    public void TextSize_Defaults115_AndAnOlderFileWithoutItGetsTheDefault()
    {
        File.WriteAllText(PreferencesFile, """{"Theme":"Dark"}""");

        var preferences = _library.Services.GetRequiredService<IAppPreferences>();

        Assert.Equal(ThemeChoice.Dark, preferences.Current.Theme);
        Assert.Equal(115, preferences.Current.TextSize);
    }

    [Fact]
    public void LibraryGroupAndSort_DefaultToNoneAndTitle_AndRoundTrip()
    {
        var preferences = _library.Services.GetRequiredService<IAppPreferences>();
        Assert.Equal(LibraryGroup.None, preferences.Current.LibraryGroup);
        Assert.Equal(LibrarySort.Title, preferences.Current.LibrarySort);

        preferences.Save(preferences.Current with { LibraryGroup = LibraryGroup.Genre, LibrarySort = LibrarySort.DateAdded });
        using var restarted = new ServiceCollection().AddLibraryData(_library.DatabasePath).BuildServiceProvider();

        var current = restarted.GetRequiredService<IAppPreferences>().Current;
        Assert.Equal(LibraryGroup.Genre, current.LibraryGroup);
        Assert.Equal(LibrarySort.DateAdded, current.LibrarySort);
    }

    [Fact]
    public void FileFromBeforeGroupAndSort_KeepsItsChoices_AndDefaultsTheRest()
    {
        File.WriteAllText(PreferencesFile, """{ "Theme": "Dark", "LibraryView": "Table" }""");

        var current = _library.Services.GetRequiredService<IAppPreferences>().Current;

        Assert.Equal(new AppPreferences(ThemeChoice.Dark, LibraryView.Table, LibraryGroup.None, LibrarySort.Title), current);
    }

    [Fact]
    public void BackupSettings_DefaultOff_KeepTen_AndRoundTrip()
    {
        var preferences = _library.Services.GetRequiredService<IAppPreferences>();
        Assert.Equal((null, false, 10), (preferences.Current.BackupFolder, preferences.Current.BackupOnExit, preferences.Current.KeepBackups));

        preferences.Save(preferences.Current with { BackupFolder = "/backups", BackupOnExit = true, KeepBackups = 5 });
        using var restarted = new ServiceCollection().AddLibraryData(_library.DatabasePath).BuildServiceProvider();

        var current = restarted.GetRequiredService<IAppPreferences>().Current;
        Assert.Equal(("/backups", true, 5), (current.BackupFolder, current.BackupOnExit, current.KeepBackups));
    }

    [Fact]
    public void CorruptFile_GivesDefaults()
    {
        File.WriteAllText(PreferencesFile, "{ not json");

        var preferences = _library.Services.GetRequiredService<IAppPreferences>();

        Assert.Equal(new AppPreferences(), preferences.Current);
    }

    [Fact]
    public void UnwritableFile_KeepsChoiceForThisSession()
    {
        Directory.CreateDirectory(PreferencesFile); // a folder where the file should be: writes fail
        var preferences = _library.Services.GetRequiredService<IAppPreferences>();

        preferences.Save(new AppPreferences(ThemeChoice.Dark, LibraryView.Grid));

        Assert.Equal(ThemeChoice.Dark, preferences.Current.Theme);
    }

    public void Dispose() => _library.Dispose();
}
