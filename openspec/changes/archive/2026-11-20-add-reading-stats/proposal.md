# Proposal

## Why

M3 ends with "see at a glance what you have, what you've read and what you want". Reading data now exists; this
slice adds the simple stats the roadmap names: read this year, per author, series progress.

## What Changes

- A **Reading** page in the sidebar with:
  - **This year**: how many reads finished this year, and which books.
  - **By year**: reads per year for the years with any.
  - **At a glance**: have and read, have but not read, want (not owned), read but not owned.
  - **Authors**: per author, books read of books in the library ("3 of 5").
  - **Series**: per series with two or more books, books read of books in the library ("4 of 7").
- A book counts as read when it has a finished (not abandoned) read or its status is read.

## Capabilities

### New Capabilities

- `reading-stats`: the Reading page's counts and lists.

### Modified Capabilities

None.

## Impact

- Core: `ReadingStats` computed by a pure function from per-book facts.
- Data: `IReadingStats` loads those facts in one query.
- UI: a Reading page and sidebar link.
- No new packages, no migration.
