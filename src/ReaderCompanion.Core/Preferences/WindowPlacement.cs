namespace ReaderCompanion.Core.Preferences;

/// <summary>Where the window is and how big, outside full screen; with whether it was maximised.</summary>
public sealed record WindowBounds(int Left, int Top, int Width, int Height, bool Maximized);

/// <summary>A screen's usable area (without taskbars and docks).</summary>
public sealed record ScreenArea(int Left, int Top, int Width, int Height)
{
    public bool Contains(int x, int y) => x >= Left && x < Left + Width && y >= Top && y < Top + Height;
}

/// <summary>Where the window opens: full HD on the first start when the screen holds it, otherwise maximised; later,
/// as the user left it, kept on a screen.</summary>
public static class WindowPlacement
{
    public const int PreferredWidth = 1920;
    public const int PreferredHeight = 1080;
    private const int SmallestUseful = 400;

    /// <param name="screens">The screens' usable areas, the main one first; empty when unknown.</param>
    public static WindowBounds First(IReadOnlyList<ScreenArea> screens)
    {
        if (screens is not [var main, ..])
        {
            return new WindowBounds(0, 0, PreferredWidth, PreferredHeight, Maximized: false);
        }

        return main.Width >= PreferredWidth && main.Height >= PreferredHeight
            ? new WindowBounds(
                main.Left + ((main.Width - PreferredWidth) / 2), main.Top + ((main.Height - PreferredHeight) / 2),
                PreferredWidth, PreferredHeight, Maximized: false)
            : new WindowBounds(main.Left, main.Top, main.Width, main.Height, Maximized: true);
    }

    /// <summary>The remembered placement, unless it is too small to use (then as on a first start) or its middle is
    /// on no screen (then moved onto the main screen and shrunk to fit it).</summary>
    public static WindowBounds Restore(WindowBounds left, IReadOnlyList<ScreenArea> screens)
    {
        if (left.Width < SmallestUseful || left.Height < SmallestUseful)
        {
            return First(screens);
        }

        if (screens is not [var main, ..] || screens.Any(s => s.Contains(left.Left + (left.Width / 2), left.Top + (left.Height / 2))))
        {
            return left;
        }

        var width = Math.Min(left.Width, main.Width);
        var height = Math.Min(left.Height, main.Height);
        return left with { Left = main.Left, Top = main.Top, Width = width, Height = height };
    }
}
