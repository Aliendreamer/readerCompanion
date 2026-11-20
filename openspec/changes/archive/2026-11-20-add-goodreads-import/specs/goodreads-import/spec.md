# Spec Delta

## Purpose

Brings the user's Goodreads history into the library from Goodreads' library export: reading status, dates read and
ratings for books the user owns, and not-owned books for the rest, safely re-importable.

## ADDED Requirements

### Requirement: Read the export

The Sources page SHALL offer importing a Goodreads library export file. Each row SHALL give a book (title, authors,
ISBN, Goodreads id), a status from its exclusive shelf (read → read, currently-reading → reading, to-read → want to
read, any other shelf → no status change), its "Date Read" as a finished read, and its rating (1–5; 0 means none). A
file that is not a Goodreads export SHALL be refused with a clear message and change nothing.

#### Scenario: Not an export

- **GIVEN** a CSV without Goodreads' columns
- **WHEN** the user imports it
- **THEN** the app says it is not a Goodreads library export and nothing changes

### Requirement: Series in titles

The series Goodreads appends to a title ("Mistborn (Mistborn, #1)") SHALL be split off into the book's series and
number, and the title used without it.

#### Scenario: Series suffix

- **GIVEN** an export row titled "Mistborn (Mistborn, #1)"
- **WHEN** it becomes a wanted book
- **THEN** the book is "Mistborn", series "Mistborn" #1

### Requirement: Match rows to books

A row SHALL match the book that has its Goodreads id from an earlier import, else a book with the same ISBN (10 or 13
digits, ignoring hyphens), else a book with the same title and first author, compared ignoring case, accents and
punctuation. A row without a match SHALL become a not-owned book with the row's title, authors, ISBN and date added.

#### Scenario: Owned and wanted

- **GIVEN** the library owns "Mort" by Terry Pratchett, and the export has "Mort" (read) and "Dune" (to-read)
- **WHEN** the user imports it
- **THEN** "Mort" is read with its date, and "Dune" is a wanted, not-owned book

### Requirement: Never overwrite the user

Importing SHALL set a book's status and rating only where the app has none, or where it still equals what the last
import set. Reads SHALL be added only when the book has no read finished within three days of that date. Book details
(title, authors) of existing books SHALL not change.

#### Scenario: Second import changes nothing

- **GIVEN** an export already imported, and no change in Goodreads or the app since
- **WHEN** the user imports the same export again
- **THEN** no book, read, status or rating changes, and the summary says so

#### Scenario: User's own status kept

- **GIVEN** "Mort" imported as read, then set to reading in the app
- **WHEN** the user imports the export again
- **THEN** "Mort" stays reading

### Requirement: Summary

After an import the page SHALL say how many rows matched owned books, how many became wanted books, how many books
were updated, and how many rows were skipped (with a reason).

#### Scenario: Summary

- **GIVEN** an export of 3 rows, 1 matching an owned book and 2 new
- **WHEN** the user imports it for the first time
- **THEN** the summary reads 1 matched, 2 added as wanted
