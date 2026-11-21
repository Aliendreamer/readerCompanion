# Proposal

## Why

The system title bar does not match the app and takes room; the user wants the window drawn in the app's own
style, with a full-screen button and resizing.

## What Changes

- The desktop window has no system title bar. A slim strip at the top of the page, in the app's theme, holds
  Full screen, Minimise, Maximise/Restore and Close; dragging the strip (or the sidebar's brand) moves the window
  and double-clicking it maximises or restores.
- The window can be resized from every edge and corner; it has a minimum size.
- F11 toggles full screen; Escape leaves it. In full screen the strip is hidden.
- Settings offers "Use the system title bar" (applies after a restart) as a fallback.

## Capabilities

### New Capabilities

### Modified Capabilities

- `app-appearance`: the app's own window frame.

## Impact

- UI: `IAppWindow` (no frame by default), `WindowFrame.razor`, `window.js` for F11/Escape, sidebar brand drag,
  styles, Settings option, `AppPreferences.SystemTitleBar`.
- Desktop: `PhotinoAppWindow` (Photino state + native move/resize: GTK `gtk_window_begin_move_drag` /
  `begin_resize_drag` on Linux, `WM_NCLBUTTONDOWN` on Windows), chromeless window, minimum size.
