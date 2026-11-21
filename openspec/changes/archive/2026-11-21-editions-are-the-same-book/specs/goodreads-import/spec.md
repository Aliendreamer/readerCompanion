# Spec Delta

## MODIFIED Requirements

### Requirement: Review uncertain matches

When a row's title matches a book's only by main title without a shared author, or the book it matches is already
linked to another Goodreads entry, the import SHALL not apply the row; it SHALL keep it for review. A row with another
edition's ISBN but the book's title and author SHALL match without review. The Goodreads card SHALL list rows waiting
for review with the book they might be, and SHALL let the user answer each or all of them. Confirming SHALL apply the
row to that book; rejecting SHALL add it as a wanted book. Rows waiting for review SHALL survive restarts and SHALL not
be added twice by a re-import.

#### Scenario: Another edition

- **GIVEN** an owned "Mort" by Terry Pratchett with ISBN 9780552131063
- **WHEN** the export has "Mort" by Terry Pratchett with ISBN 9780061020681
- **THEN** the row matches "Mort" without review and "Mort" keeps its ISBN

#### Scenario: No author in common

- **GIVEN** an owned "Refactoring" by "Unknown"
- **WHEN** the export has "Refactoring" by Martin Fowler
- **THEN** the row waits for review with "Refactoring" as the candidate

#### Scenario: Not the same book

- **GIVEN** a row waiting for review
- **WHEN** the user answers No
- **THEN** the row becomes a wanted book and leaves the list

#### Scenario: Answer all

- **GIVEN** 15 rows waiting for review
- **WHEN** the user presses Yes to all
- **THEN** each row is applied to its book and the list is empty
