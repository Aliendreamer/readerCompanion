# goodreads-import Specification

## Purpose

Brings the user's Goodreads history into the library from Goodreads' library export: reading status, dates read and
ratings for books the user owns, and not-owned books for the rest, safely re-importable.

## Requirements

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
punctuation. Failing those, a row SHALL be compared by main title (the title before its first colon, without
bracketed notes or a file extension) with the books not yet linked to Goodreads: with exactly one such book, the
row SHALL match it when they share an author surname and their ISBNs do not conflict, and SHALL otherwise be kept
for review. Author names SHALL be split on ";", "&", "and" and commas, and "Surname, Given" SHALL be read as one
name. A row without a match or a book to review SHALL become a not-owned book with the row's title, authors, ISBN
and date added.

#### Scenario: Owned and wanted

- **GIVEN** the library owns "Mort" by Terry Pratchett, and the export has "Mort" (read) and "Dune" (to-read)
- **WHEN** the user imports it
- **THEN** "Mort" is read with its date, and "Dune" is a wanted, not-owned book

#### Scenario: Subtitle and author form differ

- **GIVEN** the library owns "Designing Data-Intensive Applications" by "Kleppmann, Martin" without an ISBN
- **WHEN** the export has "Designing Data-Intensive Applications: The Big Ideas Behind Reliable, Scalable, and
  Maintainable Systems" by Martin Kleppmann
- **THEN** the row matches the owned book and no wanted copy is added

#### Scenario: Several authors in one name

- **GIVEN** the library owns "Node.js Design Patterns" by "Mario Casciaro & Luciano Mammino"
- **WHEN** the export has "Node.js Design Patterns: Master a series of patterns…" by Mario Casciaro
- **THEN** the row matches the owned book

#### Scenario: No usable author

- **GIVEN** the library owns "Refactoring: Improving the Design of Existing Code ( PDFDrive.com ).epub" by "Unknown"
- **WHEN** the export has "Refactoring: Improving the Design of Existing Code" by Martin Fowler
- **THEN** the row is kept for review with the owned book as the candidate

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

### Requirement: Review uncertain matches

When a row's title and first author match a book but both have an ISBN and the ISBNs differ, or when a row matches
a book only by main title (see Match rows to books), the import SHALL not apply the row; it SHALL keep it for
review. The Goodreads card SHALL list rows waiting for review with the book they might be. Confirming SHALL apply
the row to that book; rejecting SHALL add it as a wanted book. Rows waiting for review SHALL survive restarts and
SHALL not be added twice by a re-import.

#### Scenario: Another edition

- **GIVEN** an owned "Mort" with ISBN 9780552131063 and an export row "Mort" by the same author with ISBN 9780061020681
- **WHEN** the user imports the export
- **THEN** "Mort" is listed for review and its reading is unchanged until the user answers

#### Scenario: Not the same book

- **GIVEN** a row waiting for review
- **WHEN** the user answers No
- **THEN** the row becomes a wanted book and leaves the list

### Requirement: Shelves as collections

When the user turns on "Turn shelves into collections", importing SHALL put each book into a user collection for each
of its Goodreads shelves other than read, currently-reading and to-read. It SHALL be off by default and remembered.

#### Scenario: Book club shelf

- **GIVEN** shelves as collections on, and a row on the shelf "book-club"
- **WHEN** the user imports
- **THEN** the book is in the collection "book-club"

### Requirement: Re-import reminder

The app SHALL remember when the last import happened. When it is older than the chosen interval (3 months by default,
1 to 24 months), the Library page SHALL show a reminder with the steps to export a fresh file and a link to Sources;
the user SHALL be able to dismiss it until the app restarts. Without any import there SHALL be no reminder.

#### Scenario: Four months later

- **GIVEN** the last import was four months ago and the interval is 3 months
- **WHEN** the Library page opens
- **THEN** it shows the re-import reminder
