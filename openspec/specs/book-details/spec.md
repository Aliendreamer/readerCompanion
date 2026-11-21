# book-details Specification

## Purpose

Defines how the user looks at one book: its full details and files, opening a file in the system's reader, and
correcting the book's details so the corrections outlive later scans.

## Requirements

### Requirement: Details panel

The Library page SHALL have a details pane on the right, always present: with no book chosen it says "Select a book to
see its details"; choosing a book in the grid or table SHALL show its cover, title, authors, series and number,
genres, language, ISBN and date added. Its close button or the Escape key SHALL clear the choice. A chosen book SHALL
stay shown when a search or filter hides it. Below about 1,000 px of page width the pane SHALL slide over the books
only while a book is chosen. The library's filters, search, grouping and scroll position SHALL stay as they were.

#### Scenario: Open and close

- **GIVEN** the library filtered to Fantasy
- **WHEN** the user picks "Mort", reads its pane and closes it
- **THEN** the pane showed "Mort", "Terry Pratchett" and "Discworld #4", then "Select a book to see its details",
  and the list is still filtered to Fantasy

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

### Requirement: Trash an extra file

When a book has more than one file, the panel SHALL offer Move to trash on each present file. After the user confirms,
the file SHALL go to the system trash and the library SHALL forget that file, keeping the book, its other files, its
details and its reading history. A book SHALL always keep at least one present file: its last present file SHALL never
be trashed from the app. If the trash cannot take the file, the panel SHALL say so and nothing SHALL change.

#### Scenario: Trash the PDF of a merged book

- **GIVEN** "Mort" with `mort.epub` and `mort.pdf`, both present
- **WHEN** the user moves `mort.pdf` to the trash and confirms
- **THEN** `mort.pdf` is in the system trash and "Mort" lists only `mort.epub`

#### Scenario: Last file

- **GIVEN** a book with a single file
- **WHEN** its panel is open
- **THEN** no Move to trash is offered
