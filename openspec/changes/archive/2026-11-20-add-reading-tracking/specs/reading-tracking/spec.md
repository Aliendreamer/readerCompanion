# Spec Delta

## Purpose

Records the user's reading: a status per book, the dated sessions in which it was read (so re-reads count), and a
rating and note, all kept by the user and never changed by scans.

## ADDED Requirements

### Requirement: Reading status

Each book SHALL have one reading status: not started (the default), want to read, reading, read or abandoned. The
user SHALL set it in the book's details panel.

#### Scenario: Default

- **GIVEN** a book just found by a scan
- **WHEN** its panel is opened
- **THEN** its status is "not started"

### Requirement: Status changes keep the history

Setting the status to reading SHALL start a session dated today unless one is open. Setting it to read SHALL finish the
open session today, or record a session finished today when none is open. Setting it to abandoned SHALL close the open
session today as abandoned. Other changes, and setting the status the book already has, SHALL leave the sessions alone.

#### Scenario: Read twice

- **GIVEN** a book read and finished in 2024
- **WHEN** the user sets it to reading on 3 Mar 2026 and to read on 20 Mar 2026
- **THEN** it has two finished sessions and counts as read twice

#### Scenario: Abandon

- **GIVEN** a book being read since 1 Mar 2026
- **WHEN** the user sets it to abandoned on 5 Mar 2026
- **THEN** that session ends on 5 Mar 2026, marked abandoned, and does not count as a read

### Requirement: Correct the history

The panel SHALL list the book's sessions, newest first, and let the user change a session's start and finish dates
or delete it. A finish date before the start date SHALL be refused.

#### Scenario: Fix a finish date

- **GIVEN** a session finished today by mistake
- **WHEN** the user changes its finish date to 12 Feb 2026
- **THEN** the session shows 12 Feb 2026 as its finish date

### Requirement: Rating and note

Each book SHALL have an optional rating from 1 to 5 and an optional short note, set and cleared in the panel.

#### Scenario: Rate and clear

- **GIVEN** a book without a rating
- **WHEN** the user gives it 4 stars, then clears the rating
- **THEN** it shows 4 stars, then no rating

### Requirement: Kept by the user

Scans SHALL never change a book's status, sessions, rating or note. Merging duplicates SHALL keep the sessions of all
merged books (open reads becoming one read from the earliest start), the picked book's status and rating (taking the
other books' when the picked one has none), and both books' notes; a merged book with an open read SHALL be reading.

#### Scenario: Merge histories

- **GIVEN** "Mort" (EPUB) read in 2024 and "MORT" (PDF) read in 2025
- **WHEN** they are merged
- **THEN** the merged book has both sessions and counts as read twice
