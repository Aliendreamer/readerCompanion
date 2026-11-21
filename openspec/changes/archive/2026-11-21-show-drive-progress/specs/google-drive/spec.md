# Spec Delta

## ADDED Requirements

### Requirement: Drive progress

While a Drive scan runs, the Drive card SHALL show what it is doing: while looking through Drive, how many folders
and books it has seen so far; while checking books Drive has no checksum for, and while reading new or changed
books, how many of how many, with a progress bar. While a Drive book downloads to be opened, its Open link SHALL
show the percentage downloaded.

#### Scenario: Scanning a large folder

- **GIVEN** a Drive folder with 12 subfolders and 340 books
- **WHEN** the user presses Scan Drive
- **THEN** the card shows the folders and books seen so far, then "Reading new books from Drive… 3 of 20" with a bar

#### Scenario: Opening a large book

- **WHEN** the user opens a 40 MB PDF from Drive
- **THEN** its Open link reads "Downloading… 25%" and climbs until the book opens
