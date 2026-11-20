using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using ReaderCompanion.Core.Preferences;

namespace ReaderCompanion.UI.Tests;

public class ShellTests : BunitContext
{
    private readonly FakePreferences _preferences = new();
    private readonly FakeSystemTheme _system = new(prefersDark: false);

    public ShellTests() => Services.AddAppFakes(preferences: _preferences, systemTheme: _system);

    [Fact]
    public void Sidebar_LinksToEachPage_AndHighlightsCurrentPage()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("sources");

        var cut = Render<Routes>();

        var links = cut.FindAll(".sidebar nav a");
        Assert.Equal(["Library", "Reading", "Duplicates", "Organise", "Sources", "Backup", "Settings"], links.Select(a => a.TextContent.Trim()));
        Assert.Equal("Sources", cut.Find(".sidebar nav a.active").TextContent.Trim());
        Assert.Contains("readerCompanion", cut.Find(".sidebar .brand").TextContent);
    }

    [Fact]
    public void Root_FollowsSystem_ByDefault()
    {
        var cut = Render<Routes>();

        Assert.Equal("system", cut.Find(".app").GetAttribute("data-theme"));
        Assert.Empty(cut.FindAll("#use-system-theme"));
    }

    [Fact]
    public void Root_UsesStoredTheme()
    {
        _preferences.Save(new AppPreferences(ThemeChoice.Dark));

        var cut = Render<Routes>();

        Assert.Equal("dark", cut.Find(".app").GetAttribute("data-theme"));
    }

    [Fact]
    public void Toggle_OnLightSystem_SwitchesToDark_AndSavesIt()
    {
        var cut = Render<Routes>();
        cut.WaitForAssertion(() => Assert.Equal("false", cut.Find("#theme-toggle").GetAttribute("aria-checked")));

        cut.Find("#theme-toggle").Click();

        Assert.Equal("dark", cut.Find(".app").GetAttribute("data-theme"));
        Assert.Equal(ThemeChoice.Dark, _preferences.Current.Theme);
        Assert.Equal("true", cut.Find("#theme-toggle").GetAttribute("aria-checked"));
    }

    [Fact]
    public void Toggle_OnDarkSystem_SwitchesToLight()
    {
        _system.Change(dark: true);
        var cut = Render<Routes>();
        cut.WaitForAssertion(() => Assert.Equal("true", cut.Find("#theme-toggle").GetAttribute("aria-checked")));

        cut.Find("#theme-toggle").Click();

        Assert.Equal("light", cut.Find(".app").GetAttribute("data-theme"));
        Assert.Equal(ThemeChoice.Light, _preferences.Current.Theme);
    }

    [Fact]
    public void UseSystemTheme_ReturnsToSystem_AndHidesItself()
    {
        _preferences.Save(new AppPreferences(ThemeChoice.Dark));
        var cut = Render<Routes>();

        cut.Find("#use-system-theme").Click();

        Assert.Equal("system", cut.Find(".app").GetAttribute("data-theme"));
        Assert.Equal(ThemeChoice.System, _preferences.Current.Theme);
        Assert.Empty(cut.FindAll("#use-system-theme"));
        Assert.Equal("false", cut.Find("#theme-toggle").GetAttribute("aria-checked"));
    }

    [Fact]
    public void Toggle_FollowsSystemChanges_WhileOpen()
    {
        var cut = Render<Routes>();
        cut.WaitForAssertion(() => Assert.Equal("false", cut.Find("#theme-toggle").GetAttribute("aria-checked")));

        _system.Change(dark: true);

        cut.WaitForAssertion(() => Assert.Equal("true", cut.Find("#theme-toggle").GetAttribute("aria-checked")));
    }

    [Fact]
    public async Task Toggle_StopsListening_WhenRemoved()
    {
        var cut = Render<Routes>();
        cut.WaitForAssertion(() => Assert.True(_system.Listening));

        await DisposeComponentsAsync();

        Assert.False(_system.Listening);
    }

    [Fact]
    public void BrokenSystemTheme_StillShowsToggle_AsLight()
    {
        _system.Broken = true;

        var cut = Render<Routes>();

        Assert.Equal("false", cut.Find("#theme-toggle").GetAttribute("aria-checked"));
        cut.Find("#theme-toggle").Click();
        Assert.Equal("dark", cut.Find(".app").GetAttribute("data-theme"));
    }
}
