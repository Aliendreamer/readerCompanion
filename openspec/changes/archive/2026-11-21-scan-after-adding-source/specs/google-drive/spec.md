# Spec Delta

## MODIFIED Requirements

### Requirement: Drive folders

With whole-Drive access, the Drive card SHALL list the chosen Drive folders and SHALL let the user add a folder by
browsing Drive's folders in the app, and remove one. Adding a folder SHALL start a Drive scan at once; a refused
add SHALL start none. Removing a folder SHALL keep its books, with their Drive files marked missing.

#### Scenario: Add a folder

- **GIVEN** Drive linked with whole-Drive access
- **WHEN** the user opens Add folder…, goes into "Books" and chooses it
- **THEN** "Books" is listed as a Drive folder and a Drive scan starts, ending with its summary

### Requirement: Picked Drive files

With access to picked books only, Add books from Drive… SHALL open Google's picker in the browser and add the files
the user picks; each picked file SHALL be listed and removable. Picking new books SHALL start a Drive scan at once.

#### Scenario: Pick books

- **GIVEN** Drive linked with only picked books
- **WHEN** the user picks two EPUB files in Google's picker
- **THEN** both are listed on the Drive card and a Drive scan starts and adds them
