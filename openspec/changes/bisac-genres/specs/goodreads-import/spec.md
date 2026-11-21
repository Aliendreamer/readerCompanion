# Spec Delta

## MODIFIED Requirements

### Requirement: Series in titles

The series Goodreads appends to a title ("Mistborn (Mistborn, #1)") SHALL be split off into the book's series and
number, and the title used without it. When the title names several series ("Guards! Guards! (Discworld, #8; City
Watch, #1)") the first SHALL be used. Each import SHALL set the series of wanted books the user has not edited from
their row, and series no book is in any more SHALL be removed.

#### Scenario: Series suffix

- **GIVEN** an export row titled "Mistborn (Mistborn, #1)"
- **WHEN** it becomes a wanted book
- **THEN** the book is "Mistborn", series "Mistborn" #1

#### Scenario: Two series

- **GIVEN** an export row titled "Guards! Guards! (Discworld, #8; City Watch, #1)"
- **WHEN** it is imported
- **THEN** the book is "Guards! Guards!", series "Discworld" #8

#### Scenario: A series read wrongly before

- **GIVEN** a wanted book in the series "Discworld, #8; City Watch" from an earlier import
- **WHEN** the export is imported again
- **THEN** the book is in "Discworld" #8 and the series "Discworld, #8; City Watch" is gone
