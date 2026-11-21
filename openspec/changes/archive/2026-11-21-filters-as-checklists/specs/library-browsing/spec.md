# Spec Delta

## MODIFIED Requirements

### Requirement: Filter by genre, author and collection

The Library page SHALL offer genre, author and collection filters, each listing the values present in the
library and accepting several. Each filter SHALL be a dropdown button showing how many values are picked, which
opens a checklist of its values; picked values SHALL stay in that list, ticked, and SHALL NOT be shown outside it.
The checklist SHALL stay open while values are ticked, SHALL offer Clear when any are picked, SHALL close on
Escape or a click outside, and SHALL have a search box when it lists 8 or more values. A book SHALL match a filter
when it has any of the picked values. Collections are the books' series and the user's own collections.

#### Scenario: Two genres

- **GIVEN** "Dune" (Science Fiction), "Mort" (Fantasy) and "Gödel, Escher, Bach" (Nonfiction)
- **WHEN** the user ticks the genres Science Fiction and Fantasy
- **THEN** "Dune" and "Mort" are shown, "Gödel, Escher, Bach" is not, and the Genre button shows 2

#### Scenario: Removing a value

- **GIVEN** the genre filter holds Science Fiction
- **WHEN** the user unticks Science Fiction
- **THEN** all books are shown again

#### Scenario: User collection

- **GIVEN** "Mort" in the Discworld series and in the user's collection "Book club", and "Dune" in no collection
- **WHEN** the user picks the collection "Book club"
- **THEN** only "Mort" is shown; grouped by collection, "Mort" appears under both "Book club" and "Discworld"

#### Scenario: Long list

- **GIVEN** a library with 12 authors
- **WHEN** the user opens the Author filter and types "pratch"
- **THEN** only the matching authors are listed
