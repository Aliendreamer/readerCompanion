# Spec Delta

## MODIFIED Requirements

### Requirement: EPUB details

For an EPUB file the app SHALL read, from the package metadata: title, authors (in order), language, ISBN,
series name and position, genres, and the cover image when present. Genres SHALL come from the subjects
(`dc:subject`) mapped onto the app's fixed list of BISAC main headings, by their words or BISAC codes; subjects that
are not a genre on that list SHALL be dropped.

#### Scenario: Fully tagged EPUB

- **GIVEN** an EPUB whose metadata has title "The Hobbit", author "J.R.R. Tolkien", language "en",
  ISBN "9780261103344", series "Middle-earth" at position 1, and a cover image
- **WHEN** it is scanned
- **THEN** the book has exactly those details and its cover is available

#### Scenario: Genres from subjects

- **GIVEN** an EPUB with subjects "Fiction - Science Fiction", "SciFi", "Fantasy Fiction", "General" and
  "Saga of Drizzt Do'Urden"
- **WHEN** it is scanned
- **THEN** the book has the genres "Fantasy" and "Science Fiction" and no others

#### Scenario: Series in either common form

- **WHEN** an EPUB declares its series as an EPUB 3 collection (`belongs-to-collection` with
  `group-position`) or with Calibre's `calibre:series` / `calibre:series_index` metadata
- **THEN** the book gets that series and position in both cases

## ADDED Requirements

### Requirement: Genres already in the library are mapped

At start, every book the user has not edited that has a genre not on the fixed list SHALL have its genres mapped onto
the list the same way, and genres no book uses any more SHALL be removed. Books the user edited SHALL keep their
genres.

#### Scenario: An existing library

- **GIVEN** a scanned book with the genres "Fiction", "SciFi" and "Star Wars", and an edited book with "My Picks"
- **WHEN** the app starts
- **THEN** the first book has "Science Fiction", the edited one still has "My Picks", and "Fiction" is gone

### Requirement: Release-tagged series names are cleaned

A series name in the dotted release form seen in real files ("Serified.Dresden Files.SciFan.(#01 of 14)._.ICA
Serified") SHALL become the series it names ("Dresden Files"), numbered from the name (1). Of the dot-separated parts
before "(#N of M)", the series is the one with a space, else the longest. Any other series name SHALL be kept as it
is. Books already in the library with such a name and not edited by the user SHALL be fixed at start, and series no
book is in any more SHALL be removed.

#### Scenario: Tagged series

- **GIVEN** files whose series are "Serified.Dresden Files.SciFan.(#01 of 14)._.ICA Serified" and
  "Serified.Dresden Files.SciFan.(#02 of 14)._.ICA Serified", each also saying series index 1
- **WHEN** they are scanned
- **THEN** both are in the series "Dresden Files", numbered 1 and 2
