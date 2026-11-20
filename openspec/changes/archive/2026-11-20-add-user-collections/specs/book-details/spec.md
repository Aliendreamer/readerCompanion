# Spec Delta

## ADDED Requirements

### Requirement: Book collections

The details panel SHALL list the book's own collections and let the user add the book to one, choosing an existing
collection or typing a new name (matched to an existing one ignoring case and extra spaces), and remove it from one.
A collection without books SHALL no longer be listed anywhere. Scans SHALL never change a book's collections.

#### Scenario: Add to a new collection

- **GIVEN** "Mort" in no user collection
- **WHEN** the user adds it to "Book club"
- **THEN** the panel lists "Book club", and the Library's collection filter offers "Book club"

#### Scenario: Remove the last book

- **GIVEN** "Book club" holding only "Mort"
- **WHEN** the user removes "Mort" from "Book club"
- **THEN** "Book club" is no longer offered anywhere
