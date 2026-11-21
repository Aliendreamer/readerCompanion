# Spec Delta

## MODIFIED Requirements

### Requirement: Filter by genre, author and collection

The Library page SHALL show its filters as a tree in a pane on the left: sections Genres, Authors, Collections,
Formats and Status, each listing the values present in the library with a tick on the picked ones, and a "Missing file
only" tick line after them. Clicking a value SHALL pick or unpick it; several can be picked. Each value SHALL show how
many books have it. Each section SHALL show how many values it has and SHALL open and close; every section SHALL start
closed, remembered until the app closes. A section of 8 or more values SHALL have a find box. "Clear filters" SHALL be
at the top of the tree when anything is picked. A book SHALL match a section when it has any of its picked values.
Collections are the books' series and the user's own collections.

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

### Requirement: Reading and owned filters

The Library page SHALL filter by reading status (several may be picked). Each status SHALL show how many books have
it, and "Want to read" SHALL also show, under it, how many of those books have files and how many do not. Cards and
table rows SHALL show a book's status unless it is not started.

#### Scenario: Have but not read

- **GIVEN** "Dune" read and "Mort" not started
- **WHEN** the user picks the statuses "not started" and "want to read"
- **THEN** only "Mort" is shown

#### Scenario: Wanted books with and without files

- **GIVEN** three want-to-read books, one of which has a file
- **WHEN** the user opens the Status section
- **THEN** "Want to read" shows 3, and under it "1 with files, 2 without"
