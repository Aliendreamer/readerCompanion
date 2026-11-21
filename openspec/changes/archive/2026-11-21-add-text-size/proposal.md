# Proposal

## Why

On a laptop screen the menu, filters and book text are too small to read comfortably.

## What Changes

- A Text size setting (100%, 115%, 130%, 150%) on the Settings page, remembered between runs, default 115%.
- The desktop app zooms its whole window to that size at start and as soon as it changes.

## Capabilities

### New Capabilities

### Modified Capabilities

- `app-appearance`: a text size for the whole window.

## Impact

Core `AppPreferences.TextSize`; Settings page card; Desktop `Program` applies Photino's zoom.
