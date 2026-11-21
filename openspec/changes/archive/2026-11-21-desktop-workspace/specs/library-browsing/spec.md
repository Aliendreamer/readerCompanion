# Spec Delta

## MODIFIED Requirements

### Requirement: Library header

The Library page SHALL show the number of books in the status bar, and its empty state SHALL still point the user to
the Sources page. Pages SHALL NOT show a visible title; the pressed toolbar button names the page, and the title is
kept for screen readers only.

#### Scenario: Empty library

- **GIVEN** no books
- **WHEN** the Library page is shown
- **THEN** it says the library is empty and links to Sources across the full width, without filter tree, details
  pane or browse controls, and the status bar says "No books"

#### Scenario: Count while narrowed

- **GIVEN** 310 books, 42 of them tagged "Science Fiction"
- **WHEN** the user filters by "Science Fiction"
- **THEN** the status bar reads "42 of 310 books"; with no filter or search it reads "310 books"

### Requirement: Filter by genre, author and collection

The Library page SHALL show its filters as a tree in a pane on the left: sections Genres, Authors, Collections,
Formats, Status and Owned / wanted, each listing the values present in the library with a tick on the picked ones,
and a "Missing file only" tick line after them. Clicking a value SHALL pick or unpick it; several can be picked. Each
section SHALL show how many values it has and SHALL open and close; Authors and Collections SHALL start closed and
the others open, remembered until the app closes. A section of 8 or more values SHALL have a find box. "Clear
filters" SHALL be at the top of the tree when anything is picked. A book SHALL match a section when it has any of its
picked values. Collections are the books' series and the user's own collections.

#### Scenario: Two genres

- **GIVEN** "Dune" (Science Fiction), "Mort" (Fantasy) and "Gödel, Escher, Bach" (Nonfiction)
- **WHEN** the user ticks the genres Science Fiction and Fantasy in the tree
- **THEN** "Dune" and "Mort" are shown and "Gödel, Escher, Bach" is not

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
- **WHEN** the user opens the Authors section and types "pratch" in its find box
- **THEN** only the matching authors are listed

#### Scenario: Narrow window

- **GIVEN** a window under about 760 px of page width
- **WHEN** the Library is shown
- **THEN** the tree is hidden behind a "Filters" button, which shows and hides it
