# library-browsing Specification

## Purpose

Defines how readerCompanion shows the books in the library: a cover grid and a compact table, the toggle between
them, and how covers and their placeholders appear.

## Requirements

### Requirement: Cover grid view

The Library page SHALL be able to show books as a grid of cards. Each card SHALL show the book's cover, title,
authors ("Unknown author" when it has none), series and position when it has one, and a "missing file" marker
when none of its files is present. The grid SHALL fill the window's width and reflow when the window is resized.

#### Scenario: Book in a series

- **GIVEN** "Mort" by Terry Pratchett, Discworld #4, with a cover
- **WHEN** the grid is shown
- **THEN** its card shows the cover, "Mort", "Terry Pratchett" and "Discworld #4"

### Requirement: Table view

The Library page SHALL be able to show books as a table with one row per book: a small cover, title, authors,
series with position, and the missing-file status.

#### Scenario: Row content

- **GIVEN** a book whose file is missing
- **WHEN** the table is shown
- **THEN** its row shows its title, authors and series, and is marked "missing file"

### Requirement: View toggle

The Library page SHALL offer a toggle between grid and table. Grid SHALL be the default. The chosen view SHALL
be remembered across restarts.

#### Scenario: Switching to table

- **GIVEN** the library is shown as a grid
- **WHEN** the user picks the table view and restarts the app
- **THEN** the library is shown as a table

### Requirement: Covers and placeholders

A book with a cover saved by the scan SHALL show that cover. A book without one, or whose cover file can no
longer be loaded, SHALL show a placeholder with its title and first author on a colour derived from the title,
so the same book always gets the same colour.

#### Scenario: No cover

- **GIVEN** `Neuromancer.pdf` with no cover
- **WHEN** the grid is shown
- **THEN** its card shows a placeholder reading "Neuromancer"

### Requirement: Library header

The Library page SHALL show the number of books above the books in both views, and its empty state SHALL still
point the user to the Sources page. Pages SHALL NOT show a visible title; the active sidebar link names the page,
and the title is kept for screen readers only.

#### Scenario: Empty library

- **GIVEN** no books
- **WHEN** the Library page is shown
- **THEN** it says the library is empty and links to Sources, and no view toggle or header bar is shown
