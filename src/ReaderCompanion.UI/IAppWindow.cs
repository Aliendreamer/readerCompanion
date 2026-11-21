namespace ReaderCompanion.UI;

/// <summary>An edge or corner of the window, where dragging resizes it.</summary>
public enum WindowEdge
{
    North,
    South,
    East,
    West,
    NorthEast,
    NorthWest,
    SouthEast,
    SouthWest,
}

/// <summary>The window the app runs in, for the app's own frame (title strip, buttons, resize edges).</summary>
public interface IAppWindow
{
    /// <summary>False when the system draws the title bar: the app then shows no frame of its own.</summary>
    bool HasOwnFrame { get; }

    bool IsMaximized { get; }

    bool IsFullScreen { get; }

    /// <summary>Raised when the window is maximised, restored, or enters or leaves full screen.</summary>
    event Action? Changed;

    void Minimise();

    void ToggleMaximise();

    void ToggleFullScreen();

    void Close();

    /// <summary>Lets the window manager move the window with the pointer; called on a mouse press.</summary>
    void BeginMove();

    /// <summary>Lets the window manager resize the window from <paramref name="edge"/>; called on a mouse press.</summary>
    void BeginResize(WindowEdge edge);
}

/// <summary>For hosts without a window of their own to draw: no frame, nothing to do.</summary>
internal sealed class NoAppWindow : IAppWindow
{
    public bool HasOwnFrame => false;

    public bool IsMaximized => false;

    public bool IsFullScreen => false;

    public event Action? Changed
    {
        add { }
        remove { }
    }

    public void Minimise()
    {
    }

    public void ToggleMaximise()
    {
    }

    public void ToggleFullScreen()
    {
    }

    public void Close()
    {
    }

    public void BeginMove()
    {
    }

    public void BeginResize(WindowEdge edge)
    {
    }
}
