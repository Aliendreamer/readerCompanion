# Spec Delta

## ADDED Requirements

### Requirement: Reading and owned filters

The Library page SHALL filter by reading status (several may be picked) and by owned or wanted, where owned means
the book has at least one file. Cards and table rows SHALL show a book's status unless it is not started.

#### Scenario: Have but not read

- **GIVEN** "Dune" read and "Mort" not started, both owned
- **WHEN** the user picks the statuses "not started" and "want to read" and "owned"
- **THEN** only "Mort" is shown
