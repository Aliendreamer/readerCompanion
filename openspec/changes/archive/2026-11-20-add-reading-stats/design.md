# Design

## Context

Reading data lives on `Book` (status) and `ReadingSession` (finished dates, abandoned). Ownership is "has files".
Decisions marked **(my call)** were made without asking, to keep the roadmap loop moving.

## Goals / Non-Goals

**Goals:** the figures in the spec, correct and fast for a few thousand books.

**Non-Goals:** charts, custom date ranges, pages read, exporting stats.

## Decisions

### 1. Pure computation in Core

`StatsBook(Id, Title, Authors, Series, Owned, Status, FinishedReads)` where `FinishedReads` are the finish dates of
non-abandoned reads. `ReadingStats.From(books, year)` returns `ReadingStats(ThisYear, ThisYearBooks, ByYear,
Glance, Authors, Series)`. Unit tests cover the spec scenarios. Authors are ordered by books read, then name; series
by name (my call: series read as a checklist, authors as a leaderboard).

### 2. One query in Data

`IReadingStats.LoadAsync()` projects every book to `StatsBook` in one query; the page passes the current year
(`DateTime.Now.Year`) to `ReadingStats.From`.

### 3. Page

`/reading`, linked in the sidebar after Library. Four small cards in a row for "at a glance", then "This year" with
its count and titles (a re-read shows "×2"), "By year" as a simple list, then "Authors" and "Series" as tables with a
thin progress bar each.

## Risks / Trade-offs

- Long author lists → the page shows the top 20 authors and says how many more there are (my call).
