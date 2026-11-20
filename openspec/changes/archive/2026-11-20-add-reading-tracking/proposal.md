# Proposal

## Why

M3: the library should show at a glance what you have, what you have read and what you want. Today it only knows
about files. This first M3 slice records reading itself; a stats page follows in a second slice.

## What Changes

- Each book gets a **reading status**: not started, want to read, reading, read, abandoned. It is set in the book's
  details panel.
- **Reading history**: each read is a session with a start and finish date, so re-reads count. Changing the status
  keeps the history in step (starting a read, finishing it, abandoning it). Dates can be corrected and a session
  deleted.
- A **rating** (1 to 5 stars, clearable) and a short **note** per book.
- The Library page gains a **reading status filter** and an **owned / wanted filter** (owned = the book has files;
  wanted books arrive with the Goodreads import in M6). Cards and rows show the status.
- Scans never change any of this; merging duplicates combines the histories and keeps the picked book's status,
  rating and note (or the other's when the picked one has none).

## Capabilities

### New Capabilities

- `reading-tracking`: reading status, sessions with dates and re-reads, rating and notes.

### Modified Capabilities

- `library-browsing`: filters for reading status and owned / wanted; status shown on cards and rows.

## Impact

- Core: `ReadStatus`, `ReadingSession`, the pure status-change rules; list item gains status, rating, owned.
- Data: migration (status, rating, notes on `Books`; `ReadingSessions` table); `IReading` service; merge combines.
- UI: a Reading section in the panel; two filters; status badge.
- No new packages.
