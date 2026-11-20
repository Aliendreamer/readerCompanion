# Spec Delta

## Purpose

Defines how readerCompanion finds books that are likely the same, lets the user compare them, merges them into one
book that keeps every file, and remembers groups the user says are not duplicates.

## ADDED Requirements

### Requirement: Find likely duplicates

The Duplicates page SHALL group books that share an ISBN (ignoring hyphens and spaces), or share a title and
first author (ignoring case, accents, punctuation and extra spaces). Books linked through either rule SHALL form
one group. A book without an author SHALL match others only by ISBN. Two books the user marked as not duplicates
SHALL never be in the same group, even through a third book.

#### Scenario: EPUB and PDF of one book

- **GIVEN** "Mort" by Terry Pratchett from `mort.epub` and "MORT" by "terry pratchett" from `mort.pdf`
- **WHEN** the Duplicates page is shown
- **THEN** both books are listed in one group

#### Scenario: Same ISBN, different titles

- **GIVEN** two books with ISBN 978-0-552-13106-3 and 9780552131063 but different titles
- **WHEN** the Duplicates page is shown
- **THEN** they are listed in one group

#### Scenario: No author

- **GIVEN** two books titled "Untitled", neither with an author or ISBN
- **WHEN** the Duplicates page is shown
- **THEN** they are not grouped

#### Scenario: Same title, different authors

- **GIVEN** "Dune" by Frank Herbert and "Dune" by another author, with no ISBN
- **WHEN** the Duplicates page is shown
- **THEN** they are not grouped

### Requirement: Compare side by side

Each group SHALL show its books side by side with cover, title, authors, series and number, genres, formats,
date added and file locations, and SHALL let the user pick which book's details to keep.

#### Scenario: Default pick

- **GIVEN** a group where one book was edited by the user
- **WHEN** the group is shown
- **THEN** that book is picked to keep; without an edited book, the one with the most details is picked

### Requirement: Merge

Merging a group, after the user confirms, SHALL keep the picked book's details, move every file of the other books to
it, combine their genres, keep the earliest date added and a cover if the picked book has none, mark the book as edited
by the user so scans keep its details, and remove the other books. The library, filters and duplicates list SHALL
reflect the merge.

#### Scenario: Merge EPUB and PDF

- **GIVEN** a group of "Mort" (EPUB) and "MORT" (PDF), with "Mort" picked
- **WHEN** the user merges and confirms
- **THEN** the library has one "Mort" with both files, and the group is gone

### Requirement: Not duplicates

The user SHALL be able to mark a group as not duplicates. Those books SHALL not be grouped together again, even
after restarts and rescans; a new book that matches one of them SHALL still be suggested.

#### Scenario: Dismiss

- **GIVEN** a group of two different books with the same title and author
- **WHEN** the user marks it as not duplicates and restarts the app
- **THEN** the group is not shown

### Requirement: No duplicates

When no groups are found, the Duplicates page SHALL say the library has no likely duplicates.

#### Scenario: Clean library

- **GIVEN** a library with no matching books
- **WHEN** the Duplicates page is shown
- **THEN** it says no likely duplicates were found
