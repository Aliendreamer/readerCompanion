# Spec Delta

## MODIFIED Requirements

### Requirement: Merge

Merging a group, after the user confirms, SHALL keep the picked book's details, move every file of the other books to
it, combine their genres and user collections, keep the earliest date added and a cover if the picked book has none,
mark the book as edited by the user so scans keep its details, and remove the other books. The library, filters and
duplicates list SHALL reflect the merge.

#### Scenario: Merge EPUB and PDF

- **GIVEN** a group of "Mort" (EPUB) and "MORT" (PDF), with "Mort" picked
- **WHEN** the user merges and confirms
- **THEN** the library has one "Mort" with both files, and the group is gone

#### Scenario: Collections combine

- **GIVEN** "Mort" in "Favourites" and "MORT" in "Book club"
- **WHEN** the user merges them into "Mort"
- **THEN** "Mort" is in both "Favourites" and "Book club"
