# Spec Delta

## MODIFIED Requirements

### Requirement: Library header

The Library page SHALL show the number of books above the books in both views, and its empty state SHALL still
point the user to the Sources page. Pages SHALL NOT show a visible title; the active sidebar link names the page,
and the title is kept for screen readers only.

#### Scenario: Empty library

- **GIVEN** no books
- **WHEN** the Library page is shown
- **THEN** it says the library is empty and links to Sources, and no view toggle, browse controls or header bar
  are shown

#### Scenario: Count while narrowed

- **GIVEN** 310 books, 42 of them tagged "Science Fiction"
- **WHEN** the user filters by "Science Fiction"
- **THEN** the count reads "42 of 310 books"; with no filter or search it reads "310 books"

## ADDED Requirements

### Requirement: Filter by genre, author and collection

The Library page SHALL offer genre, author and collection filters, each listing the values present in the
library and accepting several. A book SHALL match a filter when it has any of the picked values. Collections
are series until the user's own collections exist.

#### Scenario: Two genres

- **GIVEN** "Dune" (Science Fiction), "Mort" (Fantasy) and "Gödel, Escher, Bach" (Nonfiction)
- **WHEN** the user picks the genres Science Fiction and Fantasy
- **THEN** "Dune" and "Mort" are shown and "Gödel, Escher, Bach" is not

#### Scenario: Removing a value

- **GIVEN** the genre filter holds Science Fiction
- **WHEN** the user removes Science Fiction
- **THEN** all books are shown again

### Requirement: Filters combine

Different filters, the format filter, the missing-file filter and the search SHALL all have to match for a book
to be shown.

#### Scenario: Genre and author

- **GIVEN** Iain M. Banks has two Science Fiction books and one Fantasy book
- **WHEN** the user picks the genre Science Fiction and the author Iain M. Banks
- **THEN** only his two Science Fiction books are shown

### Requirement: Format and missing-file filters

The Library page SHALL filter by format (EPUB, PDF), matching a book that has a file in any picked format, and
SHALL offer a "missing file only" switch that shows only books none of whose files is present.

#### Scenario: PDF only

- **GIVEN** a book with only an EPUB and a book with an EPUB and a PDF
- **WHEN** the user picks the PDF format
- **THEN** only the book with the PDF is shown

#### Scenario: Missing only

- **GIVEN** one book whose file is missing and others whose files are present
- **WHEN** the user turns on "missing file only"
- **THEN** only the book whose file is missing is shown

### Requirement: Search

The Library page SHALL offer a search box. A book SHALL match when its title or any of its authors contains the
typed text, ignoring case and accents. The list SHALL narrow as the user types.

#### Scenario: Author search

- **GIVEN** "Mort" by Terry Pratchett and "Dune" by Frank Herbert
- **WHEN** the user types "pratch"
- **THEN** only "Mort" is shown

#### Scenario: Accents ignored

- **GIVEN** "Gödel, Escher, Bach"
- **WHEN** the user types "godel"
- **THEN** that book is shown

### Requirement: Group the books

The Library page SHALL group the shown books by None, Author, Collection or Genre, in grid and table alike. Each
section SHALL have a heading with its name and book count. A book with several authors or genres SHALL appear
in each of their sections; books without one SHALL go to a last section ("Unknown author", "No collection",
"No genre").

#### Scenario: Group by author

- **GIVEN** "Good Omens" by Terry Pratchett and Neil Gaiman, and "Mort" by Terry Pratchett
- **WHEN** the user groups by author
- **THEN** a "Neil Gaiman (1)" section holds "Good Omens", and a "Terry Pratchett (2)" section holds both books

#### Scenario: Collection in series order

- **GIVEN** Discworld #2 "The Light Fantastic" and #1 "The Colour of Magic"
- **WHEN** the user groups by collection
- **THEN** the Discworld section lists "The Colour of Magic" before "The Light Fantastic", whatever the sort

### Requirement: Collapse sections

Each section SHALL collapse and expand from its heading, keeping its heading and count visible while collapsed.

#### Scenario: Collapse

- **GIVEN** books grouped by author
- **WHEN** the user collapses the "Terry Pratchett" section
- **THEN** his books are hidden and the heading "Terry Pratchett (2)" stays

### Requirement: Sort the books

The Library page SHALL sort books by title, author (first author, then title), series order (series name, then
position, books without a series last) or date added (newest first). Title SHALL be the default sort.

#### Scenario: Date added

- **GIVEN** "Dune" added yesterday and "Mort" added today
- **WHEN** the user sorts by date added
- **THEN** "Mort" comes before "Dune"

### Requirement: Remember grouping and sort

The chosen grouping and sort SHALL be remembered across restarts. Filters and search SHALL start empty at each
launch.

#### Scenario: Restart

- **GIVEN** the user groups by genre, sorts by date added and filters by an author
- **WHEN** the app is restarted
- **THEN** the books are grouped by genre and sorted by date added, with no author filter

### Requirement: Date added

Each book SHALL record when it entered the library. A book found by a scan SHALL get the time of that scan; a
book already in the library before this was recorded SHALL get the last-modified time of its oldest file.

#### Scenario: New book

- **GIVEN** a scan finds a new book
- **WHEN** the scan finishes
- **THEN** the book's date added is the time of that scan, and later rescans do not change it
