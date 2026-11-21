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
- **THEN** it says the library is empty and links to Sources, and no view toggle, browse controls or header bar
  are shown

#### Scenario: Count while narrowed

- **GIVEN** 310 books, 42 of them tagged "Science Fiction"
- **WHEN** the user filters by "Science Fiction"
- **THEN** the count reads "42 of 310 books"; with no filter or search it reads "310 books"

### Requirement: Filter by genre, author and collection

The Library page SHALL offer genre, author and collection filters, each listing the values present in the
library and accepting several. Each filter SHALL be a dropdown button showing how many values are picked, which
opens a checklist of its values; picked values SHALL stay in that list, ticked, and SHALL NOT be shown outside it.
The checklist SHALL stay open while values are ticked, SHALL offer Clear when any are picked, SHALL close on
Escape or a click outside, and SHALL have a search box when it lists 8 or more values. A book SHALL match a filter
when it has any of the picked values. Collections are the books' series and the user's own collections.

#### Scenario: Two genres

- **GIVEN** "Dune" (Science Fiction), "Mort" (Fantasy) and "Gödel, Escher, Bach" (Nonfiction)
- **WHEN** the user ticks the genres Science Fiction and Fantasy
- **THEN** "Dune" and "Mort" are shown, "Gödel, Escher, Bach" is not, and the Genre button shows 2

#### Scenario: Removing a value

- **GIVEN** the genre filter holds Science Fiction
- **WHEN** the user unticks Science Fiction
- **THEN** all books are shown again

#### Scenario: User collection

- **GIVEN** "Mort" in the Discworld series and in the user's collection "Book club", and "Dune" in no collection
- **WHEN** the user picks the collection "Book club"
- **THEN** only "Mort" is shown; grouped by collection, "Mort" appears under both "Book club" and "Discworld"

#### Scenario: Long list

- **GIVEN** a library with 12 authors
- **WHEN** the user opens the Author filter and types "pratch"
- **THEN** only the matching authors are listed

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

### Requirement: Reading and owned filters

The Library page SHALL filter by reading status (several may be picked) and by owned or wanted, where owned means
the book has at least one file. Cards and table rows SHALL show a book's status unless it is not started.

#### Scenario: Have but not read

- **GIVEN** "Dune" read and "Mort" not started, both owned
- **WHEN** the user picks the statuses "not started" and "want to read" and "owned"
- **THEN** only "Mort" is shown

### Requirement: Large libraries

The Library page SHALL show the first 120 matching books and SHALL add 120 more whenever the end of the list comes near
the view, or when the user presses Show more; a new search, filter, grouping or sort SHALL start again from the first
120. The book count and section counts SHALL count every matching book.

#### Scenario: Scrolling a large library

- **GIVEN** 3,000 books
- **WHEN** the user opens the Library and scrolls down
- **THEN** 120 books are shown at first, "3000 books" is the count, and more appear before the end is reached

### Requirement: Every long list is paged

Every list that can grow with the library (filter options, duplicates, organise moves and failures, unreadable files,
Goodreads rows to review and skipped, the reading page's lists, Drive folders and sources) SHALL show its first 120
items with how many there are, and SHALL show 120 more near its end or on Show more; a new list SHALL start again
from the first 120.

#### Scenario: A big organise preview

- **GIVEN** a preview of 2,000 moves
- **WHEN** it is shown
- **THEN** the first 120 are listed with "Showing 120 of 2000", all 2,000 stay selected, and more appear on scrolling
