# organise-files Specification

## Purpose

Lets the user move and rename the e-book files in a library folder into a layout built from each book's details,
always previewed first and always undoable, with the library following the files.

## Requirements

### Requirement: One layout and names

Files SHALL be organised as `Group/Author/Series/NN - Title`. The author is the first person of the book's first
author, given name first ("Kleppmann, Martin" is "Martin Kleppmann"; of "Mario Casciaro & Luciano Mammino" only Mario
Casciaro). The series folder is used only when the book has a series, and "NN - " only with a series number. The group
SHALL be the same for every book of an author: the genre most of that author's books have (ties go to the first in
alphabetical order); when none has a genre, the top folder most of the author's files are in, if it holds books of
several authors and does not name the author; otherwise there is no group level. A book without a usable author SHALL
NOT be moved. Names SHALL be safe on Windows and Linux (no forbidden characters, no trailing dots or spaces, at most
100 characters).

#### Scenario: A series book

- **GIVEN** "The Player of Games" by Iain M. Banks, Culture #2, and most of Banks's books tagged Science Fiction
- **WHEN** the user previews
- **THEN** the file would move to `Science Fiction/Iain M. Banks/Culture/02 - The Player of Games.epub`

#### Scenario: A standalone book of the same author

- **GIVEN** "The Algebraist" by Iain M. Banks, no series, tagged Fantasy only
- **WHEN** the user previews
- **THEN** it would move to `Science Fiction/Iain M. Banks/The Algebraist.epub`, with the rest of the author's books

#### Scenario: Books kept together by folder

- **GIVEN** "Clean Architecture" by Robert C. Martin, no genre, in `ProgramingBooks/`
- **WHEN** the user previews
- **THEN** it would move to `ProgramingBooks/Robert C. Martin/Clean Architecture.epub`

#### Scenario: No author

- **GIVEN** `ProgramingBooks/learning-docker.pdf` whose author is "Unknown"
- **WHEN** the user previews
- **THEN** it is not in the preview and is not moved

#### Scenario: A book in its own folder

- **GIVEN** `Code Complete, 2nd Edition/Code Complete - Steve McConnell.epub`, no genre, the folder holding only it
- **WHEN** the user previews
- **THEN** it would move to `Steve McConnell/Code Complete.epub`

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

### Requirement: Organise progress

While a preview, an organise or an undo runs, the Organise page SHALL say what is being done: working out the moves,
moving files or moving them back with how many of how many and a progress bar, and removing emptied folders.

#### Scenario: Organising many files

- **GIVEN** a preview of 340 moves
- **WHEN** the user presses Move
- **THEN** the page shows "Moving files… 12 of 340" with a bar, counting up until the result is shown
