# Spec Delta

## MODIFIED Requirements

### Requirement: Manage library folders

The user SHALL link a local folder through a button that opens the system folder picker, SHALL see the linked
folders, and SHALL be able to unlink one. Linking a folder SHALL start a scan at once, with the usual progress and
summary. Linking a folder that is already linked, or that is inside a linked folder, SHALL be refused with a message
saying why, and SHALL start no scan.

#### Scenario: Link a folder

- **WHEN** the user presses the link-folder button and picks a folder that is not yet linked
- **THEN** it appears in the list of library folders and a scan starts, showing its progress and then its summary

#### Scenario: Picker cancelled

- **WHEN** the user presses the link-folder button and closes the picker without choosing
- **THEN** nothing changes and no message is shown

#### Scenario: Add a duplicate or nested folder

- **GIVEN** `/books` is a library folder
- **WHEN** the user adds `/books` or `/books/fantasy`
- **THEN** the folder is not added, the user is told it is already covered, and no scan starts

#### Scenario: Add a parent of existing folders

- **GIVEN** `/books/fantasy` is a library folder
- **WHEN** the user adds `/books`
- **THEN** `/books` replaces `/books/fantasy` in the list and the books already scanned from `/books/fantasy` are kept

#### Scenario: Remove a folder

- **GIVEN** a library folder whose files have been scanned
- **WHEN** the user removes the folder
- **THEN** it disappears from the list and the books from it stay in the library with their files marked missing
