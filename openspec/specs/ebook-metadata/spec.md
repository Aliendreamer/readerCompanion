# ebook-metadata Specification

## Purpose

Defines which book details readerCompanion reads from EPUB and PDF files, how authors and series are matched
across books, and what happens when a detail is missing or a file cannot be read.

## Requirements

### Requirement: EPUB details

For an EPUB file the app SHALL read, from the package metadata: title, authors (in order), language, ISBN,
series name and position, genres (each `dc:subject`), and the cover image when present.

#### Scenario: Fully tagged EPUB

- **GIVEN** an EPUB whose metadata has title "The Hobbit", author "J.R.R. Tolkien", language "en",
  ISBN "9780261103344", series "Middle-earth" at position 1, and a cover image
- **WHEN** it is scanned
- **THEN** the book has exactly those details and its cover is available

#### Scenario: Genres from subjects

- **GIVEN** an EPUB with subjects "Fantasy" and "Classics"
- **WHEN** it is scanned
- **THEN** the book has the genres "Fantasy" and "Classics"

#### Scenario: Series in either common form

- **WHEN** an EPUB declares its series as an EPUB 3 collection (`belongs-to-collection` with
  `group-position`) or with Calibre's `calibre:series` / `calibre:series_index` metadata
- **THEN** the book gets that series and position in both cases

### Requirement: PDF details

For a PDF file the app SHALL read the title and author from the document properties. A PDF author property
containing several names separated by `;` or `&` SHALL produce several authors.

#### Scenario: PDF with properties

- **GIVEN** a PDF whose document properties have title "Dune" and author "Frank Herbert"
- **WHEN** it is scanned
- **THEN** the book's title is "Dune" and its author is "Frank Herbert"

### Requirement: File-name fallback

When a file has no usable title, the title SHALL be the file name without its extension. When a file has no
author, the book SHALL have no author (shown as unknown) rather than a guessed one. Whitespace-only values
count as missing.

#### Scenario: PDF without properties

- **GIVEN** `Neuromancer.pdf` with empty document properties
- **WHEN** it is scanned
- **THEN** the book's title is "Neuromancer" and it has no author

### Requirement: Unreadable files are kept and reported

A file that is damaged, encrypted, or otherwise cannot be read SHALL still become a book titled from its file
name, SHALL be listed as unreadable in the scan summary, and SHALL NOT stop the scan.

#### Scenario: Corrupt EPUB

- **GIVEN** a library folder with a corrupt `broken.epub` and a valid `good.epub`
- **WHEN** the user scans
- **THEN** both are in the library, `broken` is titled "broken", and the summary lists `broken.epub` as unreadable

### Requirement: Authors, genres and series are shared

Authors, genres and series SHALL be matched by name ignoring letter case and surrounding or repeated whitespace, so the
same author, genre or series found in several files is one author, one genre or one collection in the library.

#### Scenario: Same author written differently

- **GIVEN** one EPUB by "Ursula K. Le Guin" and another by "ursula k.  le guin"
- **WHEN** both are scanned
- **THEN** the library has one author with two books

#### Scenario: Series across books

- **GIVEN** three EPUBs in series "Discworld" at positions 1, 2 and 3
- **WHEN** they are scanned
- **THEN** the library has one "Discworld" collection containing the three books in that order
