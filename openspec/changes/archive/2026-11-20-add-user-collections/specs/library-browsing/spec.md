# Spec Delta

## MODIFIED Requirements

### Requirement: Filter by genre, author and collection

The Library page SHALL offer genre, author and collection filters, each listing the values present in the
library and accepting several. A book SHALL match a filter when it has any of the picked values. Collections
are the books' series and the user's own collections.

#### Scenario: Two genres

- **GIVEN** "Dune" (Science Fiction), "Mort" (Fantasy) and "Gödel, Escher, Bach" (Nonfiction)
- **WHEN** the user picks the genres Science Fiction and Fantasy
- **THEN** "Dune" and "Mort" are shown and "Gödel, Escher, Bach" is not

#### Scenario: Removing a value

- **GIVEN** the genre filter holds Science Fiction
- **WHEN** the user removes Science Fiction
- **THEN** all books are shown again

#### Scenario: User collection

- **GIVEN** "Mort" in the Discworld series and in the user's collection "Book club", and "Dune" in no collection
- **WHEN** the user picks the collection "Book club"
- **THEN** only "Mort" is shown; grouped by collection, "Mort" appears under both "Book club" and "Discworld"
