# Spec Delta

## MODIFIED Requirements

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
