# Proposal

## Why

Scanned metadata is often wrong or thin: a title with the file name in it, a missing author, no series number, no
genres. M2 promises "fix bad metadata" with edits that survive rescans, and "Open" to read the book. Without
them the new browse views are only as good as the files' metadata.

## What Changes

- Clicking a book in the grid or table opens a **details panel** beside the books: cover, title, authors, series
  and number, genres, language, ISBN, date added, and each of the book's files with its format, size and status.
- **Open** launches a file in the system's default app; **Show in folder** opens the folder that holds it.
  Missing files offer neither. A failure to launch is shown in the panel.
- **Edit** in the panel: title (required), authors (one per line, in order), series and number, genres, language
  and ISBN. Saving updates the library list without losing the current filters or search.
- An **edited book keeps its details**: later scans never overwrite its title, authors, series, genres, language
  or ISBN, even when its file's content changes. Its cover still follows the file.

## Capabilities

### New Capabilities

- `book-details`: viewing one book's details and files, opening its files, and editing its details so that the
  edits survive rescans.

### Modified Capabilities

- `library-scanning`: a rescan of a changed file no longer overwrites the details of a book the user edited.

## Impact

- Core: `ILibraryBooks` gains get-details and update; `Book` gains `EditedUtc`.
- Data: a migration adds `Books.EditedUtc`; the catalog writer skips details for edited books; updates reuse
  authors, genres and series by normalized name.
- UI: `IFileLauncher` (open a file, show its folder); a details panel with view and edit modes; book titles in
  the grid and table become clickable.
- Desktop: `IFileLauncher` via the operating system's default-app launcher.
- No new packages.
