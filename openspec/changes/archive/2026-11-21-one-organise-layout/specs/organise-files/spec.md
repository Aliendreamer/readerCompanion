# Spec Delta

## REMOVED Requirements

### Requirement: Layouts and names

**Reason**: the three layouts made "No series", "Unknown genre" and "Unknown author" folders and split authors.
**Migration**: replaced by "One layout and names".

## ADDED Requirements

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
