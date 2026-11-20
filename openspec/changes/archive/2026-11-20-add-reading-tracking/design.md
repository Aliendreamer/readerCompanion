# Design

## Context

Books are rows made by scans; user-owned data so far (edits, collections) lives beside them and scans leave it
alone. The panel already has view/edit modes and an `OnSaved` path that reloads the list. Decisions marked
**(my call)** were made without asking, to keep the roadmap loop moving.

## Goals / Non-Goals

**Goals:** status, sessions, rating and note per book; filters for status and owned/wanted; kept through scans and
merges.

**Non-Goals:** stats (next M3 slice), wanted books without files (M6 Goodreads), reading progress (pages/percent).

## Decisions

### 1. Model

`Book.ReadStatus` (enum `ReadStatus { NotStarted, WantToRead, Reading, Read, Abandoned }`, stored as text),
`Book.Rating` (int?, 1–5), `Book.Notes` (string?). `ReadingSession { Id, BookId, StartedOn (DateOnly?),
FinishedOn (DateOnly?), Abandoned (bool) }`, cascading from the book. Dates are calendar days in the user's local
time (my call: a read happens on a day, not at an instant).

### 2. Status rules in Core

`ReadingLog.ChangeStatus(sessions, newStatus, today)` returns the sessions to add or change, as a pure function with
unit tests from the spec: reading opens one unless one is open; read finishes the open one or adds a finished one;
abandoned closes the open one as abandoned; other statuses change nothing. A session is open when `FinishedOn` is
null. `Reads` = finished, not abandoned.

### 3. `IReading` in Data

`GetAsync(bookId)` → `ReadingInfo(Status, Rating, Notes, Sessions)`; `SetStatusAsync(bookId, status, today)` applies
the rules; `SetRatingAsync`, `SetNotesAsync`; `UpdateSessionAsync(sessionId, started, finished)` (refuses finish <
start with `ArgumentException`); `DeleteSessionAsync(sessionId)`. `today` is passed in so tests are deterministic;
the UI passes `DateOnly.FromDateTime(DateTime.Now)`.

### 4. List item and filters

`BookListItem` gains `ReadStatus`, `Rating` and `Owned` (has any file). `BrowseQuery` gains `Statuses` and `Shelf`
(`Owned`/`Wanted`, several allowed); both are filters like the others (any-within, all-across), shown with the
existing generic `FilterPicker` (labels "Not started", "Want to read", …; "Owned", "Wanted"). A small status badge
appears on cards and rows for any status but not started.

### 5. Panel

A "Reading" section under the book's facts: status as a row of buttons (pressed state), five star buttons plus
"Clear", the note in a textarea saved on change (blur), and the session list with date inputs and a delete button
per session. Every change reloads the panel and the list (`OnSaved`).

### 6. Merge

`Duplicates.MergeAsync` moves the others' sessions to the kept book and fills the kept book's status (if not
started), rating and note (if empty) from the first other book that has them. Two open reads become one, started on
the earlier date (`ReadingLog.CombineOpen`): reading two copies of a book at once is one read. The rule that a read
cannot finish before it starts lives in `ReadingLog.DateError`, used by the service, the panel (which checks before
saving) and the test fake.

## Risks / Trade-offs

- Saving the note on blur means closing the panel with Escape while typing loses the last edit → acceptable for a
  short note; the textarea also saves on change.
- Status and sessions can disagree after manual date edits (e.g. status read, session reopened by clearing its finish
  date) → the status is what the user set; sessions are history. Stats use sessions.
