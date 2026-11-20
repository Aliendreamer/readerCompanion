# Design

## Context

`GoodreadsImport` matches by Goodreads id, then `MatchKeys.For` (ISBN, then title-author). Preferences are a JSON
record. User collections are `BookCollection` links. Decisions marked **(my call)** were made without asking, to keep
the roadmap loop moving.

## Goals / Non-Goals

**Goals:** a safe path for doubtful matches, shelves as collections on request, a reminder to re-import.

**Non-Goals:** fuzzy title matching, editing a pending row, scheduled reminders outside the app.

## Decisions

### 1. What is uncertain (my call)

Only one rule: a title-author match where both sides have an ISBN and they differ. Matches by Goodreads id or ISBN are
certain; a title-author match where either side lacks an ISBN is accepted as before.

### 2. `GoodreadsPending`

A table holding the row's fields (Goodreads id unique, title, authors joined, ISBN, rating, dates, status, shelves) and
the candidate book id (nullable, set null if that book is removed). `IGoodreadsImport` gains `PendingAsync()`,
`ConfirmAsync(pendingId)` (apply the row to the candidate, remember its Goodreads id) and `RejectAsync(pendingId)` (add
it as a wanted book). A re-import skips rows already pending (by Goodreads id) and counts them in the summary.

### 3. Shelves as collections

`AppPreferences.GoodreadsShelvesAsCollections` (off). `ImportAsync` takes an options record; when on, each custom
shelf becomes a user collection (matched by normalized name) linked to the book; links are only added, never removed.

### 4. Reminder

`AppPreferences.GoodreadsLastImportUtc` (set after a successful import) and `GoodreadsReminderMonths` (3). A pure
`GoodreadsReminder.IsDue(lastUtc, months, nowUtc)`; Home shows a dismissible banner. The Goodreads card shows the last
import date and the interval input.

## Risks / Trade-offs

- Stored pending rows go stale if the user later edits Goodreads → a newer import replaces a pending row with the same
  Goodreads id.
