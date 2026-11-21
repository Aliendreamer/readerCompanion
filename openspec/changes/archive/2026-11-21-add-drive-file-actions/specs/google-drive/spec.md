# Spec Delta

## ADDED Requirements

### Requirement: Trash Drive files

Move to trash SHALL work for a Drive file that is not the book's last present file: the file SHALL go to Google
Drive's trash and the library SHALL forget it. With read-only Drive access it SHALL be refused and say so; if Drive
refuses, nothing SHALL change.

#### Scenario: Trash a Drive copy

- **GIVEN** a book with a local EPUB and the same EPUB on Drive, and read and write access
- **WHEN** the user moves the Drive copy to the trash
- **THEN** it is in Google Drive's trash and the book keeps its local file

### Requirement: Organise Drive folders

With whole-Drive read and write access, the Organise page SHALL offer the chosen Drive folders. Preview, apply and
undo SHALL behave as for a local folder: files are renamed and moved within the Drive folder into the layout, missing
folders are created, a file whose target name is taken is reported and not moved, folders emptied by the moves go to
Drive's trash, and the library follows each move.

#### Scenario: Organise and undo on Drive

- **GIVEN** a Drive folder "Books" holding `hobbit_final.epub`, by J.R.R. Tolkien, genre Fantasy
- **WHEN** the user organises "Books" by Genre / Author / Title and applies it
- **THEN** the file is `Books/Fantasy/Tolkien, J.R.R/The Hobbit.epub` on Drive and in the library; undo puts it back
