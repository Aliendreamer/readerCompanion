using Microsoft.AspNetCore.Components.Web;
using ReaderCompanion.Core.Preferences;
using ReaderCompanion.UI.Pages;

namespace ReaderCompanion.UI.Tests;

public class WindowFrameTests : BunitContext
{
    private readonly FakeAppWindow _window = new() { HasOwnFrame = true };
    private readonly FakeWindowKeys _keys = new();
    private readonly FakePreferences _preferences = new();

    public WindowFrameTests() => Services.AddAppFakes(preferences: _preferences, window: _window, keys: _keys);

    private IRenderedComponent<Routes> RenderApp() => Render<Routes>();

    [Fact]
    public void SystemFrame_ShowsNoStripOrEdges()
    {
        _window.HasOwnFrame = false;
        var cut = RenderApp();

        Assert.Empty(cut.FindAll(".window-bar"));
        Assert.Empty(cut.FindAll(".window-edge"));
        Assert.DoesNotContain("has-frame", cut.Find(".app").ClassList);
    }

    [Fact]
    public void Buttons_DriveTheWindow_AndMaximiseBecomesRestore()
    {
        var cut = RenderApp();
        Assert.Contains("has-frame", cut.Find(".app").ClassList);
        Assert.Equal("Maximise", cut.Find("#window-maximise").GetAttribute("aria-label"));

        cut.Find("#window-fullscreen").Click();
        cut.Find("#window-minimise").Click();
        cut.Find("#window-maximise").Click();
        cut.Find("#window-close").Click();
        Assert.Equal(["ToggleFullScreen", "Minimise", "ToggleMaximise", "Close"], _window.Calls);

        cut.InvokeAsync(() => _window.Become(maximized: true));
        cut.WaitForAssertion(() => Assert.Equal("Restore", cut.Find("#window-maximise").GetAttribute("aria-label")));
    }

    [Fact]
    public void Strip_MovesOnPress_AndMaximisesOnDoublePress()
    {
        var cut = RenderApp();

        cut.Find(".window-drag").MouseDown(new MouseEventArgs { Button = 0, Detail = 1 });
        cut.Find(".window-drag").MouseDown(new MouseEventArgs { Button = 0, Detail = 2 });
        cut.Find(".window-drag").MouseDown(new MouseEventArgs { Button = 2, Detail = 1 }); // right button: nothing

        Assert.Equal(["BeginMove", "ToggleMaximise"], _window.Calls);
    }

    [Fact]
    public void SidebarBrand_MovesTheWindow()
    {
        var cut = RenderApp();

        cut.Find(".sidebar .brand").MouseDown(new MouseEventArgs { Button = 0, Detail = 1 });

        Assert.Equal(["BeginMove"], _window.Calls);
    }

    [Fact]
    public void Edges_Resize_UnlessMaximised()
    {
        var cut = RenderApp();
        Assert.Equal(8, cut.FindAll(".window-edge").Count);

        cut.Find(".window-edge-southeast").MouseDown(new MouseEventArgs { Button = 0 });
        cut.Find(".window-edge-north").MouseDown(new MouseEventArgs { Button = 0 });
        Assert.Equal(["BeginResize SouthEast", "BeginResize North"], _window.Calls);

        cut.InvokeAsync(() => _window.Become(maximized: true));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".window-edge")));
    }

    [Fact]
    public void FullScreen_HidesTheFrame_F11Toggles_EscapeLeaves()
    {
        var cut = RenderApp();

        cut.InvokeAsync(() => _keys.Press("F11"));
        cut.InvokeAsync(() => _keys.Press("Escape")); // not full screen yet: nothing
        Assert.Equal(["ToggleFullScreen"], _window.Calls);

        cut.InvokeAsync(() => _window.Become(fullScreen: true));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".window-bar")));
        Assert.Empty(cut.FindAll(".window-edge"));
        Assert.DoesNotContain("has-frame", cut.Find(".app").ClassList);

        cut.InvokeAsync(() => _keys.Press("Escape"));
        Assert.Equal(["ToggleFullScreen", "ToggleFullScreen"], _window.Calls);
    }

    [Fact]
    public void Settings_OffersTheSystemTitleBar()
    {
        var cut = Render<SettingsPage>();
        Assert.False(cut.Find("#system-title-bar").HasAttribute("checked"));

        cut.Find("#system-title-bar").Change(true);

        Assert.True(_preferences.Current.SystemTitleBar);
        Assert.Contains("restart", cut.Find(".settings-window").TextContent);
    }
}
