# Design

## Context

The Library page (`Home.razor`) holds the books, the search and filters in component state; navigating away and
back would reset them. `CatalogWriter.Describe` fills a book's title, language, ISBN, series, cover, authors and
genres from its file, and runs for new books and on `UpdateContent` (a known path whose content changed).
Authors, genres and series are shared rows matched by `NameNormalizer.Normalize`. There is no way yet to launch
a file from the app.

Decisions marked **(my call)** were made without asking, to keep the roadmap loop moving; the user can revisit
them.

## Goals / Non-Goals

**Goals:**

- See and correct a book's details without losing the browse state.
- Open a book's file in the system reader, or its folder.
- Corrections are never overwritten by scans.

**Non-Goals:**

- Undo of an edit or "reset to file details" (later, with the undo work in M5).
- Editing covers, user collections (later M2 slice), reading status (M3).
- Removing authors, genres or series that no book uses any more; they stay in the table, hidden because
  browse options come from books.

## Decisions

### 1. A side panel on the Library page, not a separate page (my call)

A separate `/books/{id}` page would rebuild the Library page on return, dropping search, filters and scroll. A panel
rendered by `Home` keeps all of it. The panel is fixed to the right edge, over the books on narrow windows, and closes
with its close button or Escape; Escape in the edit form cancels the edit first. Book titles in the grid and table
become buttons that select the book. A book that disappeared (e.g. merged away) shows "no longer in the library"; a
failed save keeps the form open with the error.

### 2. `ILibraryBooks` gains `GetAsync(id)` and `UpdateAsync(id, BookEdit)`

`GetAsync` returns `BookDetails`: the list fields plus language, ISBN, edited flag and the files
(`BookFileInfo(Path, Format, SizeBytes, Present)`). `UpdateAsync` takes `BookEdit(Title, Authors, Series,
SeriesIndex, Genres, Language, Isbn)`, cleans every name with `NameNormalizer.Clean`, drops blanks and
duplicates (by normalized key), finds or creates the shared author/genre/series rows by normalized name, sets
`EditedUtc`, and saves. It rejects an empty title with an `ArgumentException`; the UI validates first.

The find-or-create logic moves out of `CatalogWriter` into a small internal `CatalogNames` helper both use, so
matching stays identical between scans and edits.

### 3. `Book.EditedUtc` marks edited books

Nullable column, set on every save. For a book with `EditedUtc` set, `CatalogWriter.Describe` still reads the
file (to know if it is unreadable and to refresh the cover) but leaves title, language, ISBN, series, authors and
genres alone. New books and split-off books are never edited, so they read as before.

### 4. `IFileLauncher` in UI (beside `IFolderPicker`), implemented by the desktop host

`bool Open(string path)` and `bool ShowInFolder(string path)`, returning false when the OS refuses. The desktop
implementation uses `Process.Start` with `UseShellExecute = true` (the default-app handler on Windows,
`xdg-open` on Linux); Show in folder opens the file's directory the same way (my call: selecting the file in
the file manager differs per desktop and is not worth it yet). Launch errors (`Win32Exception`,
`InvalidOperationException`, `FileNotFoundException`) return false.

### 5. Edit form inputs (my call)

Plain text inputs: title; authors in a textarea, one per line; series and a number box; genres in a textarea, one per
line (genre names can contain commas); language; ISBN. Series number accepts decimals ("2.5"), and is cleared when the
series is blank. On save the panel shows the saved details and Home reloads the list (and browse options), keeping its
filters.

## Risks / Trade-offs

- Marking the whole book edited, rather than each field, means a later file fix of a field the user did not
  touch is ignored too → simpler to explain ("you edited it, it is yours"); per-field locks can come later.
- Reloading the whole list after a save is a full query → fine at thousands of books, and keeps one source of
  truth.
- `xdg-open` may be missing on minimal Linux systems → Open fails gracefully with a message.
