# Spec Delta

## ADDED Requirements

### Requirement: Read Drive books in place

A Drive scan SHALL read a new or changed EPUB or PDF's details and cover by fetching only the parts of the file the
reader needs, not the whole file, and SHALL read up to 10 books at once. The result SHALL be the same as reading the
whole file.

#### Scenario: A large EPUB

- **GIVEN** a 4 MB EPUB on Drive whose package file and cover take 30 KB
- **WHEN** Drive is scanned
- **THEN** the book gets its title, authors and cover, and far less than the whole file is downloaded
