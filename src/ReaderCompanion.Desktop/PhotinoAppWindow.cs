using Photino.NET;
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
    /// <paramref name="ownFrame"/> is false.</summary>
    public void Attach(PhotinoWindow window, bool ownFrame)
    {
        _window = window;
        HasOwnFrame = ownFrame;
        window
            .SetTitle(Title)
            .SetChromeless(ownFrame)
            .SetMinSize(720, 480)
            .RegisterMaximizedHandler((_, _) => SetMaximized(true))
            .RegisterRestoredHandler((_, _) => SetMaximized(false));
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
