# Spec Delta

## ADDED Requirements

### Requirement: Review uncertain matches

When a row's title and first author match a book but both have an ISBN and the ISBNs differ, the import SHALL not
apply the row; it SHALL keep it for review. The Goodreads card SHALL list rows waiting for review with the book they
might be. Confirming SHALL apply the row to that book; rejecting SHALL add it as a wanted book. Rows waiting for
review SHALL survive restarts and SHALL not be added twice by a re-import.

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
