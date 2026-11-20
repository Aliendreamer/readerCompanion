# Design

## Context

Books are created by scans; a book without files is "not owned". `DuplicateFinder` already folds titles and authors
and normalises ISBNs. Reading data: `Book.ReadStatus`, `Rating`, `Sessions`. Decisions marked **(my call)** were made
without asking, to keep the roadmap loop moving.

## Goals / Non-Goals

**Goals:** import status, reads, ratings and wanted books from the export; safe to repeat; files join wanted books.

**Non-Goals (next M6 slice):** a review screen for uncertain matches, custom shelves to collections, the re-import
reminder. Also out: Goodreads reviews and notes, page counts, publishers.

## Decisions

### 1. Parsing

`GoodreadsCsv.Parse(TextReader)` in Core: an RFC 4180 reader (quotes, doubled quotes, newlines in fields), header
lookup by name, required columns `Book Id`, `Title`, `Author`, `Exclusive Shelf`; `ISBN`/`ISBN13` unwrap Goodreads'
`="…"`; dates `yyyy/MM/dd`; `Additional Authors` comma-separated. Throws `InvalidDataException` when the required
columns are missing.

### 2. Match keys shared with duplicates

`DuplicateFinder`'s ISBN and title-author folding become public helpers (`MatchKeys.Isbn`, `MatchKeys.TitleAuthor`) so
imports, duplicate finding and the scan's wanted-book attach agree.

### 3. Remembering what the import set (my call)

`Book.GoodreadsId` (string?, unique), `Book.GoodreadsStatus` and `Book.GoodreadsRating` (what the last import set).
The import updates status only if the book's status is not started or equals `GoodreadsStatus`; the same for rating.
`ReadingSession.FromGoodreads` marks imported reads; a read is added only when no session of the book finishes on that
date. Reading status does not go through `ReadingLog.ChangeStatus` (that would invent "today" sessions); the export's
dates are the history.

### 4. Not-owned books

Created with title, `NameNormalizer`-cleaned authors (via `CatalogNames`), ISBN, `GoodreadsId`, `DateAddedUtc` from
"Date Added" (or now), and the reading data. They have no files, so `Owned` is false everywhere.

### 5. Scan attaches files to wanted books

`CatalogWriter` on `AddBook`: after reading the file's metadata, it looks for a book without files whose ISBN or
title-author key matches; if found, the files join it (its details, edited or not, are kept; only the cover is taken
from the file when it has none).

### 6. UI

`IFilePicker.PickFileAsync(title, extensions)` (Photino `ShowOpenFileAsync`). The Sources page gets a Goodreads card:
a short how-to ("My Books → Import and export → Export library"), the import button, and the last summary.

## Risks / Trade-offs

- Title-author matching can attach a file to the wrong wanted book with the same title and author → the same key the
  user already trusts for duplicates; the review screen (next slice) and merge/trash can fix mistakes.
