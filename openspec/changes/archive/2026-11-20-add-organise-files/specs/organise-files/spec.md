# Spec Delta

## Purpose

Lets the user move and rename the e-book files in a library folder into a layout built from each book's details,
always previewed first and always undoable, with the library following the files.

## ADDED Requirements

### Requirement: Layouts and names

The user SHALL choose a layout: `Genre/Author/Title` (default), `Author/Series/NN - Title` or `Author/Title`. The author
SHALL be written "Last, First" from the first author, the genre SHALL be the first genre, and missing values SHALL be
"Unknown author", "Unknown genre" or "No series". Names SHALL be safe on Windows and Linux: characters not allowed
in file names replaced, no trailing dots or spaces, and no more than 100 characters per name.

#### Scenario: Default layout

- **GIVEN** "The Hobbit" by J.R.R. Tolkien, genre Fantasy, in `/books/hobbit_final(1).epub`
- **WHEN** the user previews the default layout for `/books`
- **THEN** the file would move to `/books/Fantasy/Tolkien, J.R.R/The Hobbit.epub` (Windows does not allow a
  folder name ending in a dot)

#### Scenario: Series layout

- **GIVEN** "Mort" by Terry Pratchett, Discworld #4
- **WHEN** the user previews `Author/Series/NN - Title`
- **THEN** the file would move to `Pratchett, Terry/Discworld/04 - Mort.epub` under the library folder

### Requirement: Preview first

The Organise page SHALL list, before anything moves, every present file in the chosen folder whose path would change,
with its current and new path and a checkbox (ticked by default). Files already in place SHALL not be listed.

#### Scenario: Already tidy

- **GIVEN** a folder already organised in the chosen layout
- **WHEN** the user previews it
- **THEN** the page says nothing needs to move

### Requirement: Apply without overwriting

Applying SHALL move only the ticked files. It SHALL never overwrite a file: a new path already taken, on disk or by
another planned move, SHALL get " (2)", " (3)" and so on before the extension. The library SHALL record each file's new
path in the same step, so books keep their details and reading history. Folders left empty inside the library folder
SHALL be removed. A file that cannot be moved SHALL be reported and the rest still moved.

#### Scenario: Two books with the same title

- **GIVEN** two different books both titled "Poems" by "Unknown author" with no genre
- **WHEN** the user applies the default layout
- **THEN** one becomes `Unknown genre/Unknown author/Poems.epub` and the other `Unknown genre/Unknown author/Poems
  (2).epub`

### Requirement: Moves stay safe

A move that would leave the chosen library folder SHALL be refused, and if the library cannot record a move, the file
SHALL be moved back, so the disk and the library never disagree.

#### Scenario: Outside the folder

- **GIVEN** a move whose new path is outside the chosen library folder
- **WHEN** the user applies it
- **THEN** it is refused and listed, and the file stays where it was

### Requirement: Undo

Every apply SHALL be recorded. Undo last organise SHALL move each file back to where it was, recreating folders, but
only when the file is still at its organised path and its old path is free; skipped files SHALL be listed and the
organise SHALL stay undoable until every file is back. The library SHALL follow the files back.

#### Scenario: Undo

- **GIVEN** an organise that moved 120 files
- **WHEN** the user presses Undo last organise
- **THEN** all 120 files are back at their old paths and the library points at them

#### Scenario: File moved since

- **GIVEN** an organise, after which the user moved one organised file elsewhere
- **WHEN** the user undoes it
- **THEN** every other file goes back and the page lists the one that was skipped
