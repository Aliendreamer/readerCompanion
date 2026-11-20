# Spec Delta

## Purpose

Defines how the user looks at one book: its full details and files, opening a file in the system's reader, and
correcting the book's details so the corrections outlive later scans.

## ADDED Requirements

### Requirement: Details panel

Choosing a book in the grid or table SHALL open a panel beside the books showing its cover, title, authors,
series and number, genres, language, ISBN and date added. The panel SHALL close from its close button or the
Escape key. The library's filters, search, grouping and scroll position SHALL stay as they were.

#### Scenario: Open and close

- **GIVEN** the library filtered to Fantasy
- **WHEN** the user picks "Mort", reads its panel and closes it
- **THEN** the panel showed "Mort", "Terry Pratchett" and "Discworld #4", and the list is still filtered to
  Fantasy

### Requirement: Book files

The panel SHALL list each of the book's files with its file name, folder, format, size and whether it is present
or missing.

#### Scenario: Two files, one missing

- **GIVEN** a book with `/books/a.epub` present and `/old/a.pdf` missing
- **WHEN** its panel is open
- **THEN** both files are listed, the PDF marked missing

### Requirement: Open a file

Each present file SHALL offer Open, which opens it in the system's default app for that file type, and Show in
folder, which opens the folder that holds it. Missing files SHALL offer neither. If the system cannot open it,
the panel SHALL say so.

#### Scenario: Open the EPUB

- **GIVEN** a book whose EPUB is present
- **WHEN** the user presses Open on that file
- **THEN** the system opens the file in its default reader

#### Scenario: Launch fails

- **GIVEN** a present file the system cannot open
- **WHEN** the user presses Open
- **THEN** the panel says the file could not be opened

### Requirement: Edit details

The panel SHALL offer Edit, letting the user change the title, authors (one per line, in order), series and number,
genres (one per line, as names can contain commas), language and ISBN. The title SHALL be required. Saving SHALL update
the book and the library list; Cancel SHALL leave the book unchanged.

#### Scenario: Fix author and series

- **GIVEN** "Mort" with no author and no series
- **WHEN** the user edits it to author "Terry Pratchett", series "Discworld", number 4, and saves
- **THEN** the panel and the list show "Terry Pratchett" and "Discworld #4"

#### Scenario: Empty title refused

- **GIVEN** the edit form for a book
- **WHEN** the user clears the title and saves
- **THEN** the book is not saved and the form says a title is required

### Requirement: Names are shared

Saving SHALL match authors, genres and series to existing ones ignoring case and extra spaces, so "terry
pratchett" joins the existing "Terry Pratchett" rather than creating another.

#### Scenario: Same author, different case

- **GIVEN** a library with the author "Terry Pratchett"
- **WHEN** the user sets another book's author to "terry  pratchett"
- **THEN** both books list under the same author "Terry Pratchett"

### Requirement: Edits survive rescans

Once the user saves a book's details, the book SHALL be marked as edited, and scans SHALL no longer change its
title, authors, series, genres, language or ISBN.

#### Scenario: Rescan after an edit

- **GIVEN** the user retitled a book to "The Real Title"
- **WHEN** its file's content changes and the user rescans
- **THEN** the book is still titled "The Real Title"
