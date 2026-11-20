# Spec Delta

## MODIFIED Requirements

### Requirement: Changed file content updates the book

When a file at a known path has different content than at the last scan (the file was edited or replaced),
the rescan SHALL re-read its details and update the book that file belongs to. If the user has edited that
book's details, the rescan SHALL keep the user's title, authors, series, genres, language and ISBN, and update
only its cover.

#### Scenario: EPUB replaced with a corrected version

- **GIVEN** a scanned book from `/books/a.epub`
- **WHEN** `/books/a.epub` is replaced by a version with a different title and the user rescans
- **THEN** the same book now shows the new title

#### Scenario: Edited book keeps its details

- **GIVEN** a scanned book from `/books/a.epub` whose title the user changed to "The Real Title"
- **WHEN** `/books/a.epub` is replaced by a version with a different title and the user rescans
- **THEN** the book still shows "The Real Title"
