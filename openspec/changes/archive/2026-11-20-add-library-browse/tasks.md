# Tasks

## 1. Data

- [x] 1.1 Add `Book.DateAddedUtc`, set by `CatalogWriter` when it creates a book, and a migration that adds the
  column and fills existing books from their oldest file's last-modified time — Data tests: a new book gets the
  scan time, a rescan keeps it; migration fills an existing book from its oldest file
- [x] 1.2 `BookListItem` gains genres, formats (distinct) and date added; `LibraryBooks.ListAsync` returns them —
  Data test: a book with an EPUB and a PDF and two genres lists both formats and both genres
- [x] 1.3 `AppPreferences` gains `LibraryGroup` (None/Author/Collection/Genre) and `LibrarySort`
  (Title/Author/Series/DateAdded) — Data tests: round-trip, and an older file without them loads the defaults

## 2. Browse rules

- [x] 2.1 `BrowseQuery`, `BrowseResult`, `BookSection` and `LibraryBrowser.Options` (genres, authors,
  collections and formats present, sorted) — unit tests
- [x] 2.2 Filters: any-within, all-across; format; missing only — unit tests from the spec scenarios
- [x] 2.3 Search on title and authors, ignoring case and accents — unit tests ("pratch", "godel")
- [x] 2.4 Sort by title, author, series order, date added — unit tests
- [x] 2.5 Grouping by author, collection, genre: fan-out, empty bucket last, collection in series order — unit
  tests ("Good Omens", Discworld)

## 3. UI

- [x] 3.1 `BrowseBar`: search, group and sort selects, view toggle, genre/author/collection pickers with chips,
  format and missing-only — bUnit: picking a genre narrows the books, removing the chip restores them
- [x] 3.2 `BookSections`: a collapsible `<details>` per section with "Name (count)" around `BookGrid`/`BookTable`,
  no heading when ungrouped — bUnit: grouped by author shows headings and counts in grid and table
- [x] 3.3 Home: compose bar, sections, "N of M books" count, "No books match" with "Clear filters"; group and sort
  saved to preferences; filters not saved; empty library unchanged — bUnit: count while filtered, group/sort
  saved, clear filters, empty library shows no browse controls
- [x] 3.4 Styles for the bar, chips, pickers and section headings in both themes

## 4. Verification

- [x] 4.1 Run the quality gates from CLAUDE.md (`dotnet build`, `dotnet test`, `pnpm format:check`,
  `pnpm lint:md`) and a manual check on Linux: filters, search, grouping with collapse, sort, remembered group and
  sort after restart, existing books show a sensible date added

## Workflow follow-up

- Archive the change and update ROADMAP.md after the user's approval.
