# Proposal

## Why

The window always opened at 1200 × 800, small on most screens, and forgot how the user left it.

## What Changes

- First start: 1920 × 1080, centred, when the screen's work area holds it; otherwise maximised.
- On closing, the window's normal size and position and whether it was maximised are remembered, and restored at the
  next start. A remembered window that would be off every screen is moved onto the screen and shrunk to fit it.

## Capabilities

### New Capabilities

### Modified Capabilities

- `app-appearance`: window placement.

## Impact

Core `WindowBounds` (in `AppPreferences.Window`) and `WindowPlacement`; Desktop applies it at start (and once the
window exists and the monitors are known) and saves it when closing; `PhotinoAppWindow` tracks the normal bounds.
