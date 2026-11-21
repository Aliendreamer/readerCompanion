# Proposal

## Why

The review list filled with hundreds of rows (313 on a real library) that were plainly the same book: same title,
same author, another edition's ISBN. Answering them one by one was the user's work for nothing.

## What Changes

- A row with a book's title and first author, or its main title and a shared author surname, matches that book
  whatever the ISBNs; the book keeps its own ISBN.
- Review keeps only rows whose title matches a book without any shared author, and rows for a book already linked to
  another Goodreads entry.
- The review list has "Yes to all" and "No to all", and each row's answers sit beside it.

On the real library and export: 313 rows waiting became 15.

## Capabilities

### New Capabilities

### Modified Capabilities

- `goodreads-import`: editions match; review only the doubtful.

## Impact

Data `GoodreadsImport`; UI `GoodreadsSource` (answer all, layout).
