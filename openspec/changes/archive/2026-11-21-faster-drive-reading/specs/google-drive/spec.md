# Spec Delta

## MODIFIED Requirements

### Requirement: Read Drive books in place

A Drive scan SHALL read a new or changed EPUB or PDF's details and cover by fetching only the parts of the file the
reader needs, not the whole file, and SHALL read up to 20 books at once, starting the next book as soon as one is
done. The result SHALL be the same as reading the whole file. A read that Google answers with "too many requests"
or a passing server error SHALL be tried again after a short wait, twice at most.

#### Scenario: A large EPUB

- **GIVEN** a 4 MB EPUB on Drive whose package file and cover take 30 KB
- **WHEN** Drive is scanned
- **THEN** the book gets its title, authors and cover, and far less than the whole file is downloaded

#### Scenario: Drive asks to slow down

- **WHEN** Google answers a read with "too many requests" once
- **THEN** the read is tried again after a short wait and the scan goes on
