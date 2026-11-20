# Spec Delta

## ADDED Requirements

### Requirement: Merged books stay together

When a file's content changes at the same path, the file SHALL become a separate book only if another present
file of its book still has the file's old content (an identical copy). Otherwise the book SHALL be updated in
place, even if the book has other files with different content, such as after a merge.

#### Scenario: New edition of a merged book's EPUB

- **GIVEN** a book that holds `/books/mort.epub` and `/books/mort.pdf` after a merge
- **WHEN** `/books/mort.epub` is replaced by a new edition and the user rescans
- **THEN** the library still has one book holding both files

#### Scenario: One of two identical copies changes

- **GIVEN** a book whose identical copies are `/books/a.epub` and `/backup/a.epub`
- **WHEN** `/books/a.epub` gets different content and the user rescans
- **THEN** `/books/a.epub` becomes a new book and `/backup/a.epub` stays with the old one
