# Spec Delta

## ADDED Requirements

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
