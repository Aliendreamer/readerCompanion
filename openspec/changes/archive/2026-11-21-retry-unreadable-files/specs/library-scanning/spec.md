# Spec Delta

## ADDED Requirements

### Requirement: Unreadable files are tried again

A file whose details could not be read SHALL be read again by the next scan even when its content has not changed,
and the scan summary SHALL give the reason for each file it could not read.

#### Scenario: A passing failure

- **GIVEN** a book titled from its file name because its file could not be read
- **WHEN** the next scan reads the file successfully
- **THEN** the book gets the title, authors and cover from the file
