# Proposal

## Why

M1 works, but the app is plain unstyled HTML: a text list of books and a row of links. Before M2 adds grouped
views, the app needs a look worth building on — covers on screen, clear navigation, and a dark theme that
follows the system.

## What Changes

- A left **sidebar** replaces the top links: app name, Library, Sources; the current page is highlighted.
- The Library page shows books as a **cover grid** (cover, title, authors, series) or a **compact table** (small
  cover, title, authors, series, status), with a **toggle** at the top. The choice is remembered.
- Books without a cover get a generated placeholder (title and author on a coloured tile).
- Book covers saved by the M1 scan are shown in the window.
- **Light and dark themes**; by default the app follows the system theme. A **System / Light / Dark** switch in
  the sidebar overrides it, and the choice is remembered.
- The Sources page gets the same styling (cards, buttons, progress, summary); its behaviour does not change.
- A book count and the "missing file" marker stay visible in both views.

## Capabilities

### New Capabilities

- `app-appearance`: the app's navigation shell and its light/dark theme, including following the system theme
  and remembering the user's choice.
- `library-browsing`: how the books in the library are shown — cover grid and table views, the toggle between
  them, covers and placeholders.

### Modified Capabilities

None. Scanning and metadata behaviour are unchanged.

## Impact

- UI project: new layout, sidebar, theme switch, book grid and table components, stylesheet rewritten with
  theme variables.
- Core: `BookListItem` gains the book's cover; a small preferences interface (theme, library view).
- Data: preferences stored in a JSON file next to the database; the book list query returns the cover.
- Desktop: covers served to the window from the covers folder.
- No new packages; no database migration.
