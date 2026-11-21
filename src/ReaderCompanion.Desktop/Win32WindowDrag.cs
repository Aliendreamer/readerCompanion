using System.Runtime.InteropServices;
using ReaderCompanion.UI;

namespace ReaderCompanion.Desktop;

/// <summary>Starts a system move or resize of a window without a title bar, as if its caption or border were
/// pressed. Must run on the window's thread, while the mouse button is down.</summary>
internal static class Win32WindowDrag
{
    private const uint WmNcLButtonDown = 0xA1;
    private const int HtCaption = 2;
    private const int VkLButton = 0x01;

    public static void Move(nint window) => Press(window, HtCaption);

    public static void Resize(nint window, WindowEdge edge) => Press(window, edge switch
    {
        WindowEdge.West => 10,
        WindowEdge.East => 11,
        WindowEdge.North => 12,
        WindowEdge.NorthWest => 13,
        WindowEdge.NorthEast => 14,
        WindowEdge.South => 15,
        WindowEdge.SouthWest => 16,
        _ => 17, // SouthEast
    });

    private static void Press(nint window, int hitTest)
    {
        if ((GetAsyncKeyState(VkLButton) & 0x8000) == 0)
        {
            return; // released already (a quick click): starting now would leave the window stuck to the pointer
        }

        ReleaseCapture();
        SendMessageW(window, WmNcLButtonDown, hitTest, 0);
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int key);

    [DllImport("user32.dll")]
    private static extern nint SendMessageW(nint window, uint message, nint wParam, nint lParam);
}
