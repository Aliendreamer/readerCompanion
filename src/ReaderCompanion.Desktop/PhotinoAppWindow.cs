using Photino.NET;
using ReaderCompanion.Core.Preferences;
using ReaderCompanion.UI;

namespace ReaderCompanion.Desktop;

/// <summary>The Photino main window behind the app's own frame. Moving and resizing are handed to the window
/// manager (GTK on Linux, Win32 on Windows), which also works on Wayland, where apps cannot place windows.</summary>
internal sealed class PhotinoAppWindow : IAppWindow
{
    public const string Title = "readerCompanion";

    private PhotinoWindow? _window;
    private bool _maximizedBeforeFullScreen;

    public bool HasOwnFrame { get; private set; }

    public bool IsMaximized { get; private set; }

    public bool IsFullScreen { get; private set; }

    public event Action? Changed;

    /// <summary>Sets the window up before it is shown: without the system title bar unless
    /// <paramref name="ownFrame"/> is false, where it was left (<paramref name="remembered"/>) or full HD on a first
    /// start. Once the window exists, the placement is checked against the screens.</summary>
    public void Attach(PhotinoWindow window, bool ownFrame, WindowBounds? remembered)
    {
        _window = window;
        HasOwnFrame = ownFrame;
        var start = remembered ?? WindowPlacement.First([]); // the screens are known only once the window exists
        _normal = start with { Maximized = false };
        window
            .SetTitle(Title)
            .SetChromeless(ownFrame)
            .SetMinSize(720, 480)
            .SetSize(start.Width, start.Height)
            .RegisterWindowCreatedHandler((_, _) => Place(remembered))
            .RegisterMaximizedHandler((_, _) => SetMaximized(true))
            .RegisterRestoredHandler((_, _) => SetMaximized(false))
            .RegisterSizeChangedHandler((_, _) => TrackNormal())
            .RegisterLocationChangedHandler((_, _) => TrackNormal());
        if (remembered is null)
        {
            window.Center();
        }
        else
        {
            window.SetLocation(new System.Drawing.Point(start.Left, start.Top));
        }
    }

    /// <summary>How to reopen the window: its normal size and place, and whether it was maximised.</summary>
    public WindowBounds? Placement => _normal is { } normal
        ? normal with { Maximized = IsFullScreen ? _maximizedBeforeFullScreen : IsMaximized }
        : null;

    private WindowBounds? _normal;

    /// <summary>Applies the placement for the screens there are: full HD or maximised on a first start, the remembered
    /// one moved back onto a screen when it is off them all.</summary>
    private void Place(WindowBounds? remembered)
    {
        if (_window is not { } window)
        {
            return;
        }

        var main = window.MainMonitor;
        var screens = new[] { main }.Concat(window.Monitors.Where(m => m.WorkArea != main.WorkArea))
            .Select(m => new ScreenArea(m.WorkArea.X, m.WorkArea.Y, m.WorkArea.Width, m.WorkArea.Height))
            .Where(a => a.Width > 0 && a.Height > 0)
            .ToList();
        var target = remembered is null ? WindowPlacement.First(screens) : WindowPlacement.Restore(remembered, screens);
        if (target.Maximized && remembered is null)
        {
            _normal = null; // a first start on a small screen: maximised, with no normal size of its own yet
        }
        else
        {
            window.SetSize(target.Width, target.Height).SetLocation(new System.Drawing.Point(target.Left, target.Top));
            _normal = target with { Maximized = false };
        }

        if (target.Maximized)
        {
            window.SetMaximized(true);
            SetMaximized(true);
        }
    }

    /// <summary>Remembers the window's size and place while it is neither maximised nor full screen.</summary>
    private void TrackNormal()
    {
        if (_window is { } window && !IsMaximized && !IsFullScreen && !window.Maximized && window.Width > 0)
        {
            _normal = new WindowBounds(window.Left, window.Top, window.Width, window.Height, Maximized: false);
        }
    }

    public void Minimise() => _window?.SetMinimized(true);

    public void ToggleMaximise()
    {
        var maximize = !IsMaximized; // read once: the window's own event may update the state in between
        _window?.SetMaximized(maximize);
        SetMaximized(maximize);
    }

    public void ToggleFullScreen()
    {
        if (_window is null)
        {
            return;
        }

        var on = !IsFullScreen;
        if (on)
        {
            _maximizedBeforeFullScreen = IsMaximized;
            if (IsMaximized)
            {
                _window.SetMaximized(false); // Photino cannot be both
                IsMaximized = false;
            }
        }

        _window.SetFullScreen(on);
        IsFullScreen = on;
        if (!on && _maximizedBeforeFullScreen)
        {
            _window.SetMaximized(true);
            IsMaximized = true;
        }

        Changed?.Invoke();
    }

    public void Close() => _window?.Close();

    public void BeginMove() => OnWindowThread(window =>
    {
        if (OperatingSystem.IsWindows())
        {
            Win32WindowDrag.Move(window.WindowHandle);
        }
        else if (OperatingSystem.IsLinux())
        {
            GtkWindowDrag.Move(Title);
        }
    });

    public void BeginResize(WindowEdge edge) => OnWindowThread(window =>
    {
        if (OperatingSystem.IsWindows())
        {
            Win32WindowDrag.Resize(window.WindowHandle, edge);
        }
        else if (OperatingSystem.IsLinux())
        {
            GtkWindowDrag.Resize(Title, edge);
        }
    });

    private void OnWindowThread(Action<PhotinoWindow> work)
    {
        if (_window is { } window)
        {
            window.Invoke(() => work(window));
        }
    }

    private void SetMaximized(bool maximized)
    {
        if (IsMaximized != maximized)
        {
            IsMaximized = maximized;
            Changed?.Invoke();
        }
    }
}
