using System.Runtime.InteropServices;
using ReaderCompanion.UI;

namespace ReaderCompanion.Desktop;

/// <summary>Starts a window-manager move or resize of the app's GTK window from the current pointer position, as
/// a title bar would. Must run on the GTK thread, while the mouse button is down.</summary>
internal static class GtkWindowDrag
{
    private const string Gtk = "libgtk-3.so.0";
    private const string Gdk = "libgdk-3.so.0";
    private const string GLib = "libglib-2.0.so.0";
    private const int LeftButton = 1;
    private const uint Button1Mask = 1 << 8; // GDK_BUTTON1_MASK

    public static void Move(string title)
    {
        if (Find(title) is var window && window != 0 && TryPressedPointer(window, out var x, out var y))
        {
            gtk_window_begin_move_drag(window, LeftButton, x, y, gtk_get_current_event_time());
        }
    }

    public static void Resize(string title, WindowEdge edge)
    {
        if (Find(title) is var window && window != 0 && TryPressedPointer(window, out var x, out var y))
        {
            gtk_window_begin_resize_drag(window, GdkEdge(edge), LeftButton, x, y, gtk_get_current_event_time());
        }
    }

    // GdkWindowEdge
    private static int GdkEdge(WindowEdge edge) => edge switch
    {
        WindowEdge.NorthWest => 0,
        WindowEdge.North => 1,
        WindowEdge.NorthEast => 2,
        WindowEdge.West => 3,
        WindowEdge.East => 4,
        WindowEdge.SouthWest => 5,
        WindowEdge.South => 6,
        _ => 7, // SouthEast
    };

    /// <summary>The visible toplevel GTK window with <paramref name="title"/> (Photino's main window), or 0.</summary>
    private static nint Find(string title)
    {
        var list = gtk_window_list_toplevels();
        try
        {
            // GList: { data, next, prev }
            for (var node = list; node != 0; node = Marshal.ReadIntPtr(node, IntPtr.Size))
            {
                var window = Marshal.ReadIntPtr(node);
                if (gtk_widget_get_visible(window) != 0 && Marshal.PtrToStringUTF8(gtk_window_get_title(window)) == title)
                {
                    return window;
                }
            }

            return 0;
        }
        finally
        {
            g_list_free(list);
        }
    }

    /// <summary>The pointer's screen position, only while the left button is still down: a drag started after a
    /// quick click was released would leave the window stuck to the pointer.</summary>
    private static bool TryPressedPointer(nint window, out int x, out int y)
    {
        x = y = 0;
        var display = gdk_display_get_default();
        var seat = display == 0 ? 0 : gdk_display_get_default_seat(display);
        var pointer = seat == 0 ? 0 : gdk_seat_get_pointer(seat);
        var gdkWindow = gtk_widget_get_window(window);
        if (pointer == 0 || gdkWindow == 0)
        {
            return false;
        }

        _ = gdk_window_get_device_position(gdkWindow, pointer, out _, out _, out var mask);
        if ((mask & Button1Mask) == 0)
        {
            return false;
        }

        gdk_device_get_position(pointer, out _, out x, out y);
        return true;
    }

    [DllImport(Gtk)]
    private static extern nint gtk_window_list_toplevels();

    [DllImport(Gtk)]
    private static extern nint gtk_window_get_title(nint window);

    [DllImport(Gtk)]
    private static extern int gtk_widget_get_visible(nint widget);

    [DllImport(Gtk)]
    private static extern uint gtk_get_current_event_time();

    [DllImport(Gtk)]
    private static extern void gtk_window_begin_move_drag(nint window, int button, int rootX, int rootY, uint timestamp);

    [DllImport(Gtk)]
    private static extern void gtk_window_begin_resize_drag(nint window, int edge, int button, int rootX, int rootY, uint timestamp);

    [DllImport(Gtk)]
    private static extern nint gtk_widget_get_window(nint widget);

    [DllImport(Gdk)]
    private static extern nint gdk_window_get_device_position(nint window, nint device, out int x, out int y, out uint mask);

    [DllImport(Gdk)]
    private static extern nint gdk_display_get_default();

    [DllImport(Gdk)]
    private static extern nint gdk_display_get_default_seat(nint display);

    [DllImport(Gdk)]
    private static extern nint gdk_seat_get_pointer(nint seat);

    [DllImport(Gdk)]
    private static extern void gdk_device_get_position(nint device, out nint screen, out int x, out int y);

    [DllImport(GLib)]
    private static extern void g_list_free(nint list);
}
