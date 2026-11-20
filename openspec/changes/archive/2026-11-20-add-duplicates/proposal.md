# Proposal

## Why

The same book often lands in the library twice: an EPUB and a PDF of it, or two downloads with slightly different
metadata. M2 promises finding such duplicates and merging them into one book that keeps all its files.

## What Changes

- A **Duplicates** page in the sidebar lists groups of books that look like the same book: the same ISBN, or the
  same title and first author (ignoring case, accents and punctuation).
- Each group shows its books side by side: cover, title, authors, series, genres, formats and file locations.
- **Merge** keeps one book's details (the user picks; by default the edited one, else the one with the most
  details) and moves every file of the others to it. Genres are combined and the earliest date added is kept. The
  other books are removed. Merging asks for confirmation.
- **Not duplicates** dismisses a group; dismissed pairs are not suggested again.
- A merged book stays merged on rescans: a changed file only splits off into a new book when an identical copy of
  its old content is still in that book.

## Capabilities

### New Capabilities

- `duplicates`: finding likely duplicate books, reviewing them side by side, merging them, and dismissing false
  matches.

### Modified Capabilities

- `library-scanning`: a changed file splits off into its own book only when an identical copy of its old content
  remains, so merged books stay together.

## Impact

- Core: duplicate matching rules (pure, unit-tested); the scan reconciler's split rule.
- Data: an `IDuplicates` service (find groups, merge, dismiss) and a migration adding a dismissed-pairs table.
- UI: a Duplicates page and sidebar link.
- No new packages.
