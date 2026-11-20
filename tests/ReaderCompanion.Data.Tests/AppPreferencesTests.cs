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
