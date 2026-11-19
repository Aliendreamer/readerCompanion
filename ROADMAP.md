# readerCompanion — Roadmap

This file is the project's source of truth for **what** we are building and **in what order**. Detailed
designs live in OpenSpec changes (`openspec/changes/`); this file links them. Keep it updated whenever a
decision is made.

## Vision

> "Basically we want to make a simpler Calibre — with some more useful features too."

readerCompanion is a cross-platform (Windows + Linux) desktop app for managing a personal **e-book** library:

- which books you **have**, which you have **read**, and which you **want**;
- browsing them **grouped by author, series and genre**;
- **finding duplicates** and keeping one clean copy;
- **managing the files themselves** — organising, renaming and moving them — in your OS folders and in Google
  Drive;
- **linking sources** from one place: local folders now, Goodreads and Google Drive later.

All data lives in a local SQLite file you can back up by hand.

## Status

| Milestone | State | Where |
| --------- | ----- | ----- |
| M0 — Foundation | Done | commits `fe71894` and earlier |
| Dev tooling — git hooks, formatting, Markdown lint | Done | `openspec/changes/archive/2026-11-19-add-git-hooks-tooling/` |
| M1 — Local folder scan | Planned, ready to build | `openspec/changes/add-local-folder-scan/` |
| M2–M8 | Not started | this file |

## Guiding principles

- **Simpler than Calibre.** Fewer settings, no conversion pipeline, sensible defaults. When in doubt, the
  simpler option wins.
- **Local-first.** The app is fully usable offline with no accounts. Every integration is optional.
- **Your data stays yours.** One SQLite file, easy to copy, back up, and restore.
- **Have vs want is always visible.** Owned books come from e-book files (local or on Drive). Books you have
  read or want but don't own come from Goodreads (M6) and are marked **not owned**.
- **One place to link sources.** Local folders, Goodreads and Google Drive are all linked from the **Sources**
  page, each with its own button.
- **Nothing is lost.** Rescans never erase books (missing files are marked, not deleted); file changes made by
  the app are previewed and can be undone; imports never overwrite a detail you changed by hand.

## Decided

| Concern | Choice | Why |
| ------- | ------ | --- |
| Target OS | Windows + Linux | Your call. |
| Books in the library | E-book files (EPUB, PDF to start); plus not-owned books from Goodreads (M6) | Your call. |
| Runtime | .NET 10 | Already installed (LTS). |
| UI | Blazor components in a Razor Class Library | Keeps the UI independent of the desktop host. |
| Desktop host | Photino.Blazor (like Tauri, for .NET) | Cross-platform, light, runs inside WSL2; MAUI has no Linux, WPF is Windows-only. |
| Data | EF Core + SQLite, with migrations | Single file, schema evolves safely. |
| Tests | xUnit v3 + bUnit | Standard for .NET/Blazor. |
| Quality gates | `dotnet build`, `dotnet test`, `pnpm format:check`, `pnpm lint:md` | Listed in `CLAUDE.md`. |
| Git hooks | Husky + lint-staged + commitlint + markdownlint-cli2 (pnpm) | Enforce format, Markdown style and commit messages on every commit. |
| CI/CD | None | Your call; local hooks are the gate. |
| Branching | Work directly on `main` | Your call. |
| Commits and versions | Conventional Commits; SemVer derived from them | Your call. |
| Planning | OpenSpec change per milestone (proposal, specs, design, tasks), then the developer-flow | Your call. |

Solution layout:

```text
src/
  ReaderCompanion.Core/        domain model, metadata readers, scan logic, service interfaces (no UI, no EF)
  ReaderCompanion.Data/        EF Core DbContext, migrations, service implementations
  ReaderCompanion.UI/          Razor Class Library: pages and components
  ReaderCompanion.Desktop/     Photino host (thin)
tests/
  ReaderCompanion.Core.Tests/
  ReaderCompanion.Data.Tests/
  ReaderCompanion.UI.Tests/
```

## Milestones

### M0 — Foundation (done)

An empty app window with all the plumbing in place: solution and projects, shared build settings, a Photino
window with a placeholder page, and a SQLite database created and migrated on startup in the user's app-data
folder. (A CI workflow was added and later removed by decision — no CI/CD.)

### M1 — Local folder scan

Fill the library from your e-book folders. Detailed plan: `openspec/changes/add-local-folder-scan/`.

- **Sources** page with a **Link folder** button that opens the system folder picker; unlink a folder; start a
  scan with live progress and a summary.
- Scan for EPUB and PDF at any depth. EPUB: title, authors, series and position, ISBN, language, **genres**,
  cover. PDF: title and author from document properties. Missing title → file name.
- Books, authors, genres and series are created from that; the same author/genre/series is stored once.
- Books are recognised by file content, so moved and renamed files keep their book; deleted files are marked
  missing, never erased; an unplugged drive is skipped, not treated as deleted.
- Home shows a plain list of books (grouped views come in M2).

**Done when:** linking your folder fills the library correctly, and a rescan is safe to run any time.

### M2 — Library views and duplicates

Browse and tidy what was found.

- Views: all books, **grouped by author**, **grouped by series/collection** (in series order), **grouped by
  genre**.
- Sorting (title, author, series order, date added), search (title, author), filters (format, genre, missing
  file).
- **Find duplicates**: books that are likely the same (same ISBN, or same title and author — for example the
  EPUB and the PDF of one book). Review side by side and merge into one book that keeps all files.
- Edit a book's details, including genres (edits survive rescans); create your own collections beyond series.
- "Open" launches the file in the system's default reader.

**Done when:** you can browse the whole library by author, series and genre, fix bad metadata, and merge
duplicates.

### M3 — Reading tracking

- Read status per book: *want to read / reading / read / abandoned*.
- Start and finish dates, re-reads, rating, short notes.
- Filters on every view for **read vs not read** and **owned vs wanted**, so you can see at a glance: have and
  read, have but not read, want (not owned), read but not owned.
- Simple stats: read this year, per author, series progress ("4 of 7 read").

**Done when:** you can see at a glance what you have, what you've read and what you want, in any grouping.

### M4 — Backup and restore

Make it safe to undo before the app starts changing files or importing history.

- "Back up now" writes a consistent copy of the database (SQLite online backup) to a folder you choose, with a
  timestamp.
- Restore from a backup (with confirmation and an automatic backup of the current state first).
- Optional: automatic backup on exit, keeping the last N.

**Done when:** you can back up, wipe, and restore without losing anything.

### M5 — Organise files

Manage the files themselves from the app, not only the catalog — starting with your OS folders.

- Rename and move files from within the app, for one book or many, e.g. into `Author/Series/Title` or
  `Genre/Author` folders using a naming pattern you choose.
- Always preview before applying; every change is recorded and can be undone.
- The library follows the files (books are recognised by content since M1), so reading history is never lost.
- Remove the extra file of a duplicate (found in M2) only after confirmation, to the system trash.
- The same actions work on Google Drive folders once Drive is linked (M7).

**Done when:** you can reorganise a library folder from the app, check the preview, apply it, and undo it.

### M6 — Goodreads

Link Goodreads from the Sources page. Goodreads closed its public API in 2020, so linking means importing its
**library export** (CSV); re-import a newer export any time to sync.

- Shelves map to reading status: *read*, *currently reading*, *want to read*, plus dates read and rating.
- Rows are matched to books in the library (ISBN, then normalized title + author); uncertain matches go to a
  review screen.
- Matched books get their reading status from Goodreads. **Unmatched rows become not-owned books**, so "want to
  read" is your wishlist and books you read elsewhere still count as read.
- When a scan later finds a file for a not-owned book, it becomes owned and keeps its Goodreads history.
- Re-importing updates without duplicating and never overwrites manual edits; custom shelves can become
  collections (opt-in).

**Done when:** after importing you can tell, in every view, what you have and read, have and haven't read,
want, and read without owning — and a second import changes nothing.

### M7 — Google Drive

- Link Google Drive from the Sources page (desktop OAuth); token stored securely per OS.
- Pick Drive folders to index; same metadata and matching as M1, reading only what's needed (download on open,
  cache covers).
- **With write access, manage the Drive folder from the app**: the M5 organise actions (rename, move, organise
  into folders, undo) work on Drive files too. Read-only access stays an option; then the app only indexes.
- Works offline after indexing; Drive is contacted only on rescan, open, or a change you make.

**Done when:** Drive books appear alongside local ones, changes made in the app show up in Drive, and
disconnecting Drive leaves the catalog and reading history intact.

### M8 — Polish and packaging

- Covers fetched from Open Library by ISBN when a file has none (optional, cached locally).
- Settings page (data folder, backup location, linked sources).
- Startup errors shown in a dialog instead of a silent exit.
- Releases: version and changelog from Conventional Commits (e.g. MinVer + git tags).
- Packaging for Windows and Linux (self-contained builds; installer where useful).

## Later / maybe

- Reading goals per year.
- More formats (MOBI/AZW3, CBZ).
- Export to CSV/JSON.
- Import an existing Calibre library.

## Open decisions

1. **Goodreads (M6).** Is the CSV export import enough, or did you have something else in mind?
2. **Organising files (M5).** Which naming patterns should be built in, and should anything beyond rename, move
   and trash be possible?
3. **Google Drive priority (M7).** Last on purpose — it needs a Google Cloud app registration. Move it earlier
   if that's where most of your books are.

## Decision log

- **2026-11-17** — Desktop app in .NET + Blazor; e-books only; Windows + Linux; Photino host; SQLite.
- **2026-11-17** — M0 built; Linux runtime needs `libnotify4` (and WebKitGTK 4.1) for the Photino window.
- **2026-11-18** — Work on `main` only; Conventional Commits with SemVer; no CI/CD (`.github/` removed).
- **2026-11-18** — OpenSpec for planning changes; Husky + lint-staged + commitlint + markdownlint as git hooks.
- **2026-11-19** — Vision: "a simpler Calibre with more useful features". Sources linked by button from one
  Sources page (folders now; Goodreads, Drive later).
- **2026-11-19** — Added genres (read in M1, browsed in M2), duplicate finding and merging (M2), organising
  files in OS folders and Drive (new M5; later milestones renumbered), Goodreads want-to-read as not-owned books
  so have/want/read are distinguishable (M6). Roadmap moved to the repo root.
