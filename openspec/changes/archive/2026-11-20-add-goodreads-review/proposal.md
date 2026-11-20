# Proposal

## Why

The rest of M6: some matches are too uncertain to apply blindly, custom Goodreads shelves are how many people group
books, and since the export is downloaded by hand the user needs a nudge to re-import now and then.

## What Changes

- **Uncertain matches** (title and first author agree but the two ISBNs differ, likely another edition) are not
  applied. They wait in a **review list** on the Goodreads card: "Same book?" Yes applies the row to that book; No adds
  it as a wanted book. The list survives restarts.
- **Shelves as collections** (opt-in checkbox): custom shelves become the user's collections on the matched or wanted
  book.
- **Re-import reminder**: when the last import is older than the chosen interval (3 months by default), the Library
  page shows a banner with the steps to export a fresh file; it can be dismissed for the session.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `goodreads-import`: review of uncertain matches, shelves as collections, re-import reminder.

## Impact

- Core: `GoodreadsReview` records; preferences gain shelves-as-collections, last import time and reminder interval.
- Data: a `GoodreadsPending` table (migration); the import routes uncertain rows there and applies shelves; review
  actions.
- UI: review list and options on the Goodreads card; a reminder banner on the Library page.
