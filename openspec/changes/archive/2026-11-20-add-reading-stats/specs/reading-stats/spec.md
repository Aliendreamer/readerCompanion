# Spec Delta

## Purpose

Summarises the user's reading on one page: what was read this year and per year, how owned and read books relate,
and progress per author and per series.

## ADDED Requirements

### Requirement: What counts as read

A book SHALL count as read when it has at least one finished read that was not abandoned, or its status is read.

#### Scenario: Marked read without dates

- **GIVEN** a book whose status is read but has no finished read recorded
- **WHEN** the Reading page is shown
- **THEN** the book counts as read in the at-a-glance, author and series figures

### Requirement: This year and by year

The Reading page SHALL show how many reads finished in the current year with the books' titles, and the number of
reads per year for every year that has any, newest first. A book read twice in a year SHALL count twice.

#### Scenario: Re-read this year

- **GIVEN** "Mort" finished in January and again in March this year, and "Dune" finished last year
- **WHEN** the Reading page is shown
- **THEN** this year shows 2 reads, listing "Mort" (read twice), and last year shows 1

### Requirement: At a glance

The Reading page SHALL show four counts of books: owned and read, owned and not read, wanted (not owned) and not
read, read and not owned.

#### Scenario: Counts

- **GIVEN** two owned books, one of them read, and no wanted books
- **WHEN** the Reading page is shown
- **THEN** it shows 1 owned and read, 1 owned and not read, 0 wanted, 0 read and not owned

### Requirement: Authors and series progress

The Reading page SHALL list each author with the number of their books read out of their books in the library,
most read first, and each series with two or more books in the library with its books read out of those books.

#### Scenario: Series progress

- **GIVEN** seven Discworld books in the library, four of them read
- **WHEN** the Reading page is shown
- **THEN** Discworld shows "4 of 7 read"

### Requirement: Empty library

With no books, the Reading page SHALL say there is nothing to show yet.

#### Scenario: Nothing yet

- **GIVEN** an empty library
- **WHEN** the Reading page is shown
- **THEN** it says there is nothing to show yet
