# Spec Delta

## ADDED Requirements

### Requirement: Drive folders

With whole-Drive access, the Drive card SHALL list the chosen Drive folders and SHALL let the user add a folder by
browsing Drive's folders in the app, and remove one. Removing a folder SHALL keep its books, with their Drive files
marked missing.

#### Scenario: Add a folder

- **GIVEN** Drive linked with whole-Drive access
- **WHEN** the user opens Add folder…, goes into "Books" and chooses it
- **THEN** "Books" is listed as a Drive folder

### Requirement: Picked Drive files

With access to picked books only, Add books from Drive… SHALL open Google's picker in the browser and add the files
the user picks; each picked file SHALL be listed and removable.

#### Scenario: Pick books

- **GIVEN** Drive linked with only picked books
- **WHEN** the user picks two EPUB files in Google's picker
- **THEN** both are listed on the Drive card and the next Drive scan adds them

### Requirement: Drive scan

Scan Drive SHALL bring the library in line with the chosen folders (with their subfolders) and picked files the same
way a local scan does: new content becomes a book, a file renamed or moved on Drive is followed, a file gone from
Drive is marked missing, and content identical to a known book (by SHA-256) joins it. Only new or changed files SHALL
be downloaded, to read their details and cover. When a chosen folder or picked file cannot be read, the scan SHALL
stop, say which, and change nothing.

#### Scenario: Drive copy of a local book

- **GIVEN** a local EPUB in the library and the same file in a chosen Drive folder
- **WHEN** the user scans Drive
- **THEN** the book has two files, one local and one on Drive, and nothing new is added

#### Scenario: Folder no longer readable

- **GIVEN** a chosen Drive folder that was deleted on Drive
- **WHEN** the user scans Drive
- **THEN** the scan says that folder could not be read and no file is marked missing

### Requirement: Open Drive books

The details panel SHALL show a Drive file as on Google Drive. Open SHALL download it to a cache in the data folder,
reusing a cached copy while the file is unchanged, and open it with the system's default app. Show in Drive SHALL
open the file on drive.google.com. When Drive is not linked, Open SHALL say that Drive needs linking.

#### Scenario: Open a Drive book

- **GIVEN** a book whose only file is on Drive
- **WHEN** the user presses Open
- **THEN** the file is downloaded once and opened; opening it again uses the cached copy
