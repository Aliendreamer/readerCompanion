# Proposal

## Why

readerCompanion aims to be a simpler Calibre: an e-book library. Right now the library is empty and there is no
way to get books into it. Roadmap milestone M1 fills it from the user's own e-book folders, so every later feature
(browsing by author and collection, reading tracking, Goodreads import) has real books to work on.

## What Changes

- A **Sources** page is where the user links where their books come from. In this change it has one kind of
  source: local folders, linked with a **Link folder** button that opens the system folder picker. Google Drive
  (M7) and Goodreads (M6) will be linked from the same page later, so the page is built as one section per
  source type. The user can also unlink a folder and start a **scan**.
- A scan walks each folder recursively and finds EPUB and PDF files.
- For each file the app reads its details — EPUB: title, authors, series and position, ISBN, language,
  genres, cover; PDF: title and author from document properties — falling back to the file name when a detail
  is missing.
- Books, authors, genres and series collections are created from those details; the same author, genre or
  series found in several files is stored once. Genres are captured now so M2 can group by genre without
  re-reading every file.
- Files are recognised by their content, so a **rescan** keeps a book when its file was moved or renamed,
  adds new files, and marks files that disappeared as **missing** instead of deleting anything.
- A minimal UI to drive and check this: the Sources page (link, unlink, scan, progress, summary) and a plain list
  of scanned books on the home page. Grouped views, search and editing stay in M2.

Assumptions (from the roadmap; the user did not specify their collection): any folder layout, nested to any
depth; a library of a few thousand books.

## Capabilities

### New Capabilities

- `library-scanning`: managing library folders, scanning and rescanning them, and how files map to books
  across moves, renames, deletions and unavailable folders.
- `ebook-metadata`: which book details are read from EPUB and PDF files and the fallbacks when a detail is
  missing or the file cannot be read.

### Modified Capabilities

None.

## Impact

- `ReaderCompanion.Core`: domain model and scan/metadata logic (first real code in this project).
- `ReaderCompanion.Data`: new tables and a migration (books, authors, collections, book files, library folders).
- `ReaderCompanion.UI`: Library folders page, book list on Home.
- `ReaderCompanion.Desktop`: native folder picker wiring.
- New dependency: `PdfPig` (Apache-2.0) for PDF properties. EPUB is read with the BCL (ZIP + XML).
- New test project `ReaderCompanion.Core.Tests`.
- Cover images are stored as files under the app's data folder, next to `library.db`.
