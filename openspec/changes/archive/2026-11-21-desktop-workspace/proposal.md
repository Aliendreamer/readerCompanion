# Proposal

## Why

The app feels like a website (dashboard sidebar, cards with shadows, dropdown filters, a details panel sliding over
the page). The user wants it to feel like a Windows desktop program such as Calibre. Design: `design.md` (approved).

## What Changes

- A toolbar replaces the sidebar (the same pages, theme switch, Settings, Help); the logo moves to the title strip.
- A status bar shows the book count and the progress of running scans.
- The Library is split into panes: a filter tree (replacing the filter dropdowns), the books, and a permanent details
  pane.
- Every page becomes denser and flatter.
- No new features; scanning stays on the Sources page unchanged.

## Capabilities

### New Capabilities

### Modified Capabilities

- `app-appearance`: toolbar instead of sidebar, theme switch in the toolbar, status bar.
- `library-browsing`: filter tree; the count moves to the status bar.
- `book-details`: a permanent details pane on the Library.

## Impact

UI only: layout components (toolbar, status bar, window strip), Library page and its components, Settings (use system
theme), styles; their bUnit tests.
