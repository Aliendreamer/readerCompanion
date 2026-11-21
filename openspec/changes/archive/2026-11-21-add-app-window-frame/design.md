# Design

## Moving and resizing

Photino has no drag or resize for a window without a title bar. Moving it with `SetLocation` from mouse events
fails on Wayland, where apps cannot place their windows. Instead the window manager does it, as Tauri does:

- Linux: on mouse-down the app finds its `GtkWindow` (the visible toplevel titled "readerCompanion") and calls
  `gtk_window_begin_move_drag` / `gtk_window_begin_resize_drag` with the pointer position; works on X11 and
  Wayland.
- Windows: `ReleaseCapture` then `SendMessage(hwnd, WM_NCLBUTTONDOWN, HTCAPTION or an HT edge, 0)`.

Native calls run on the window's thread through `PhotinoWindow.Invoke`.

## Boundaries

`IAppWindow` lives in the UI project with a default that has no frame (tests and any other host keep the old
look). The desktop app registers `PhotinoAppWindow`, attached to the main window after build. Native code is
covered by the manual check; the UI (buttons, drag areas, edges, keys, hiding in full screen) by bUnit tests.

## Fallback

If the custom frame misbehaves on some desktop, Settings → "Use the system title bar" restores the system
frame at the next start (`SetChromeless` only applies before the window is created).
