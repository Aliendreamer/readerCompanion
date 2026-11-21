using ReaderCompanion.Core.Preferences;

namespace ReaderCompanion.Core.Tests;

public class WindowPlacementTests
{
    private static readonly ScreenArea Big = new(0, 0, 2560, 1400);

    [Fact]
    public void FirstStart_OnAScreenThatHoldsHd_IsHdCentred()
    {
        Assert.Equal(new WindowBounds(320, 160, 1920, 1080, Maximized: false), WindowPlacement.First([Big]));
    }

    [Theory]
    [InlineData(1366, 728)]
    [InlineData(1920, 1040)] // a full-HD screen minus its taskbar
    public void FirstStart_OnASmallerScreen_IsMaximised(int width, int height)
    {
        Assert.True(WindowPlacement.First([new ScreenArea(0, 0, width, height)]).Maximized);
    }

    [Fact]
    public void Restore_KeepsWhatTheUserLeft_OnAnyScreen()
    {
        var second = new ScreenArea(2560, 0, 1920, 1040);
        var left = new WindowBounds(2700, 50, 1500, 900, Maximized: true);

        Assert.Equal(left, WindowPlacement.Restore(left, [Big, second]));
    }

    [Fact]
    public void Restore_OffEveryScreen_MovesOntoTheMainScreen_AndShrinksToFit()
    {
        var unplugged = new WindowBounds(3000, 100, 2400, 1300, Maximized: false);

        Assert.Equal(new WindowBounds(0, 0, 1920, 1040, Maximized: false),
            WindowPlacement.Restore(unplugged, [new ScreenArea(0, 0, 1920, 1040)]));
    }

    [Fact]
    public void Restore_TooSmallToUse_GetsTheFirstStartPlacement() =>
        Assert.Equal(WindowPlacement.First([Big]), WindowPlacement.Restore(new WindowBounds(10, 10, 50, 40, false), [Big]));

    [Fact]
    public void WithoutScreens_TheRememberedOrHdPlacementIsUsed()
    {
        var left = new WindowBounds(10, 10, 1500, 900, false);
        Assert.Equal(left, WindowPlacement.Restore(left, []));
        Assert.Equal(new WindowBounds(0, 0, 1920, 1080, false), WindowPlacement.First([]));
    }
}
