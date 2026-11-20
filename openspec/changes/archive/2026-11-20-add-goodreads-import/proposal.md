# Proposal

## Why

M6: what the user has read and wants lives in Goodreads. Goodreads closed its API in 2020, so the way in is its
library export (CSV). Importing it lets the library show have-and-read, have-not-read, want and read-not-owned.

## What Changes

- A **Goodreads** card on the Sources page with **Import export file…** (a file dialog for the CSV) and a summary
  after each import: matched, added as wanted, updated, unchanged, skipped.
- Each row's shelf becomes a reading status (read, currently reading, want to read), its "Date Read" a finished read,
  and its rating the book's rating.
- Rows are **matched** to books by the Goodreads id remembered from an earlier import, then ISBN, then title and first
  author (ignoring case, accents and punctuation). **Unmatched rows become not-owned books.**
- **Re-importing** never duplicates books or reads, and never overwrites what the user set in the app.
- When a **scan finds a file for a not-owned book** (same ISBN, or same title and first author), the file joins that
  book, which becomes owned and keeps its Goodreads history.

## Capabilities

### New Capabilities

- `goodreads-import`: reading the export, matching, applying reading data, wanted books, re-import rules.

### Modified Capabilities

- `library-scanning`: a newly found file can join a not-owned book instead of becoming a new book.

## Impact

- Core: `GoodreadsCsv` (parsing) and the match keys shared with duplicate finding.
- Data: `IGoodreadsImport`; `Book` gains `GoodreadsId` and the last imported status and rating; sessions remember
  that they came from Goodreads; the catalog writer attaches files to not-owned books.
- UI: `IFilePicker`; a Goodreads card on Sources. Desktop: a Photino file dialog.
- A migration; no new packages.
