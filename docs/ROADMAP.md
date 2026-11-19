# readerCompanion — Roadmap

A cross-platform (Windows + Linux) desktop app for managing a personal **e-book** library: which
books you have, which you've read, grouped by author and collection, with books discovered from a
local folder or Google Drive and reading history optionally imported from Goodreads. All data
lives in a local SQLite file you can back up by hand.

Each milestone goes through the `developer-flow` (design → plan → TDD → simplify → review →
gates → your verification) before it is called done.

## Guiding principles

- **Local-first.** The app is fully usable offline with no accounts. Every integration is optional.
- **Your data stays yours.** One SQLite file, easy to copy, back up, and restore.
- **Files are the source of books.** Every book in the library comes from an e-book file (local or
  on Drive). The app reads its details from the file; you can correct them in the app.
- **Your edits win.** Rescans and imports add and update, but never overwrite a detail you
  changed by hand.

## Decided

| Concern | Choice | Why |
| ------- | ------ | --- |
| Target OS | Windows + Linux | Your call. |
| What a book is | An e-book file (EPUB, PDF to start) | Your call. |
| Runtime | .NET 10 | Already installed (LTS). |
| UI | Blazor components in a Razor Class Library | Keeps the UI independent of the desktop host. |
| Desktop host | Photino.Blazor | Cross-platform; builds, tests, and runs inside WSL2 (WSLg + WebKitGTK present). |
| Data | EF Core + SQLite, with migrations | Single file, schema evolves safely. |
| Tests | xUnit + bUnit (components) | Standard for .NET/Blazor. |
| Gates | `dotnet build`, `dotnet test`, `pnpm format:check`, `pnpm lint:md`, plus pre-commit and commit-msg git hooks | No CI by choice; local hooks enforce format, Markdown style and Conventional Commits. |

Solution sketch:

```text
src/
  ReaderCompanion.Core/        domain model, services, scanning/import logic (no UI, no EF)
  ReaderCompanion.Data/        EF Core DbContext, migrations, repositories
  ReaderCompanion.UI/          Razor Class Library: pages and components
  ReaderCompanion.Desktop/     Photino host (thin)
tests/
  ReaderCompanion.Core.Tests/
  ReaderCompanion.Data.Tests/
  ReaderCompanion.UI.Tests/
```

## Milestones

### M0 — Foundation

An empty app window with all the plumbing in place.

- Solution and projects as sketched above; `.gitignore`, `Directory.Build.props`
  (nullable, warnings as errors), `.editorconfig`.
- Photino host opens a Blazor window showing a placeholder home page.
- SQLite database created in the user's app-data folder; first EF migration applied on startup.
- CI workflow running build, format, and test gates.

**Done when:** `dotnet run` opens the window in WSL, CI is green.

### M1 — Local folder scan

Fill the library from your e-book folder.

- Settings: pick one or more local folders.
- Scan for EPUB and PDF; read metadata from EPUB (title, authors, series and position, ISBN,
  language, cover); PDF from its document properties, falling back to the file name.
- Create books, authors, and series-collections from that metadata; recognise the same book
  across rescans (by file content hash, so moves and renames are detected).
- Rescan detects new, moved, and deleted files; deleted files are marked missing, not erased.

**Done when:** pointing the app at your folder fills the library correctly, and a rescan is safe to
run any time.

### M2 — Library views

Browse and tidy what was found.

- Views: all books, **grouped by author**, **grouped by collection** (series in order).
- Search (title, author) and filters (format, missing file).
- Edit a book's details (these edits survive rescans); create your own collections beyond series.
- "Open" launches the file in the system's default reader.

**Done when:** you can browse your whole library by author and collection and fix any bad
metadata.

### M3 — Reading tracking

- Read status per book: *to read / reading / read / abandoned*.
- Start and finish dates, re-reads, rating, short notes.
- "Read vs not read" filters on every view; simple stats (read this year, per author, collection
  progress like "4 of 7 read").

**Done when:** you can see at a glance what you have and haven't read, in any grouping.

### M4 — Backup and restore

Before any import writes reading history, make it safe to undo.

- "Back up now" writes a consistent copy of the database (SQLite online backup, not a raw file
  copy while open) to a folder you choose, with a timestamp.
- Restore from a backup file (with a confirmation and an automatic backup of the current state).
- Optional: automatic backup on exit, keeping the last N.

**Done when:** you can back up, wipe, and restore without losing anything.

### M5 — Goodreads import

Goodreads closed its public API in 2020, so this works from Goodreads' **library export CSV**.

- Match CSV rows to books in the library (ISBN, then normalized title + author); review screen
  for uncertain matches.
- Apply read status, dates read, and rating to matched books. Rows with no matching file are
  listed in the summary (not added — the library only holds books you have files for).
- Re-importing a newer export updates without duplicating, and never overwrites manual edits.

**Done when:** importing your export brings your Goodreads reading history onto your e-books, and
a second import changes nothing.

### M6 — Google Drive

- Sign in with Google (desktop OAuth, read-only Drive scope); token stored securely per OS.
- Pick Drive folders to index; same metadata and matching as M1, reading only what's needed
  (download on open, cache covers).
- Works offline after indexing; Drive is contacted only on rescan or open.

**Done when:** Drive books appear alongside local ones, and disconnecting Drive leaves your
catalog and reading history intact.

### M7 — Polish and packaging

- Covers from files, or fetched from Open Library by ISBN (optional, cached locally).
- Settings page (data folder, backup location, connected sources).
- Packaging for Windows and Linux (self-contained builds; installer where useful).

## Later / maybe

- Reading goals per year.
- More formats (MOBI/AZW3, CBZ).
- Export to CSV/JSON.
- Other sources (Calibre library).

## Open decisions

1. **Goodreads.** Is a CSV import enough, or did you have something else in mind? (Before M5.)
2. **Google Drive priority.** Last on purpose — it needs a Google Cloud app registration. Move it
   earlier if that's where most of your books are.
