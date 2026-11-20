# Spec Delta

## ADDED Requirements

### Requirement: Found files join wanted books

When a scan finds a file with content new to the library, and a not-owned book has the same ISBN, or the same title
and first author (ignoring case, accents and punctuation), the file SHALL join that book instead of becoming a new
book, so the book becomes owned and keeps its reading history; the book keeps its own details and takes from the
file only what it lacks (series, language, ISBN, authors, genres, cover).

#### Scenario: Buying a wanted book

- **GIVEN** a not-owned "Dune" from a Goodreads import, marked want to read
- **WHEN** a scan finds `dune.epub` whose metadata says "Dune" by Frank Herbert
- **THEN** the library has one "Dune", now owned, still marked want to read
