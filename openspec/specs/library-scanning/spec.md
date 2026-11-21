# library-scanning Specification

## Purpose

Lets the user point readerCompanion at local folders of e-books and keeps the library in step with those
folders across scans, without ever losing a book because its file moved or vanished.

## Requirements

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

### Requirement: Scan finds e-book files

A scan SHALL search every library folder recursively, at any depth, for files with the extensions `.epub` and
`.pdf` (case-insensitive) and SHALL ignore all other files.

#### Scenario: Nested folders and mixed files

- **GIVEN** a library folder containing `a/b/c/book.EPUB`, `notes.txt` and `doc.pdf`
- **WHEN** the user scans
- **THEN** `book.EPUB` and `doc.pdf` are in the library and `notes.txt` is not

### Requirement: One book per distinct file content

Each distinct file content SHALL become one book. Identical copies of the same file in several places SHALL
belong to the same book, which then has several file locations.

#### Scenario: Two copies of the same file

- **GIVEN** the same EPUB exists at `/books/a.epub` and `/books/backup/a.epub`
- **WHEN** the user scans
- **THEN** the library has one book with two file locations

### Requirement: Merged books stay together

When a file's content changes at the same path, the file SHALL become a separate book only if another present
file of its book still has the file's old content (an identical copy). Otherwise the book SHALL be updated in
place, even if the book has other files with different content, such as after a merge.

#### Scenario: New edition of a merged book's EPUB

- **GIVEN** a book that holds `/books/mort.epub` and `/books/mort.pdf` after a merge
- **WHEN** `/books/mort.epub` is replaced by a new edition and the user rescans
- **THEN** the library still has one book holding both files

#### Scenario: One of two identical copies changes

- **GIVEN** a book whose identical copies are `/books/a.epub` and `/backup/a.epub`
- **WHEN** `/books/a.epub` gets different content and the user rescans
- **THEN** `/books/a.epub` becomes a new book and `/backup/a.epub` stays with the old one

### Requirement: Rescan follows moved and renamed files

When a file seen in an earlier scan is found again at a different path with the same content, the rescan SHALL
update the file's location and keep the same book.

#### Scenario: File renamed

- **GIVEN** a scanned book whose file is `/books/old-name.epub`
- **WHEN** the file is renamed to `/books/new-name.epub` and the user rescans
- **THEN** the library still has that same book, now pointing at `/books/new-name.epub`, and no new book

### Requirement: Missing files are marked, never erased

When a previously scanned file is no longer found in a reachable library folder, the rescan SHALL mark that
file location as missing and keep the book. If the file reappears, it SHALL be marked present again.

#### Scenario: File deleted

- **GIVEN** a scanned book whose only file is deleted from disk
- **WHEN** the user rescans
- **THEN** the book stays in the library and its file is shown as missing

#### Scenario: File comes back

- **GIVEN** a book whose file is marked missing
- **WHEN** the file is restored at any path inside a library folder and the user rescans
- **THEN** the file is marked present at that path

### Requirement: Unavailable folders do not mark files missing

If a library folder cannot be read during a scan (for example an unplugged drive), or is completely empty although
it held books at the last scan (what an unmounted drive's mount point looks like), the scan SHALL skip it, SHALL leave
the status of its files unchanged, and SHALL report the folder as unavailable.

#### Scenario: Drive unplugged

- **GIVEN** a scanned library folder on a drive that is now disconnected
- **WHEN** the user rescans
- **THEN** none of that folder's files are marked missing and the scan summary says the folder was unavailable

#### Scenario: Folder found completely empty

- **GIVEN** a scanned library folder whose drive is unmounted, leaving an empty mount point
- **WHEN** the user rescans
- **THEN** its files are not marked missing and the summary says the folder was unavailable

### Requirement: Changed file content updates the book

When a file at a known path has different content than at the last scan (the file was edited or replaced),
the rescan SHALL re-read its details and update the book that file belongs to. If the user has edited that
book's details, the rescan SHALL keep the user's title, authors, series, genres, language and ISBN, and update
only its cover.

#### Scenario: EPUB replaced with a corrected version

- **GIVEN** a scanned book from `/books/a.epub`
- **WHEN** `/books/a.epub` is replaced by a version with a different title and the user rescans
- **THEN** the same book now shows the new title

#### Scenario: Edited book keeps its details

- **GIVEN** a scanned book from `/books/a.epub` whose title the user changed to "The Real Title"
- **WHEN** `/books/a.epub` is replaced by a version with a different title and the user rescans
- **THEN** the book still shows "The Real Title"

### Requirement: Scan progress and summary

While a scan runs, the app SHALL stay responsive and show how many files have been processed. When it ends, the
app SHALL show a summary: books added, files moved, files marked missing, files that could not be read, and
unavailable folders.

#### Scenario: Summary after a scan

- **WHEN** a scan finishes after finding 3 new books and 1 unreadable file
- **THEN** the summary reports 3 added and 1 unreadable, naming the unreadable file

### Requirement: Unchanged files are not re-read

A rescan SHALL NOT re-read the contents of files whose path, size and last-modified time are unchanged since the
previous scan.

#### Scenario: Rescan with no changes

- **GIVEN** a library that was just scanned
- **WHEN** the user rescans without changing any files
- **THEN** no books are added or changed and the summary reports nothing new

### Requirement: Found files join wanted books

When a scan finds a file with content new to the library, and a not-owned book has the same ISBN, or the same title
and first author (ignoring case, accents and punctuation), the file SHALL join that book instead of becoming a new
book, so the book becomes owned and keeps its reading history; the book keeps its own details and takes from the
file only what it lacks (series, language, ISBN, authors, genres, cover).

#### Scenario: Buying a wanted book

- **GIVEN** a not-owned "Dune" from a Goodreads import, marked want to read
- **WHEN** a scan finds `dune.epub` whose metadata says "Dune" by Frank Herbert
- **THEN** the library has one "Dune", now owned, still marked want to read

### Requirement: Scan status survives leaving the page

A library scan or Drive scan SHALL keep running when the user leaves the Sources page, and when the page is opened
again it SHALL show the running scan's progress, or the last scan's summary or failure.

#### Scenario: Back during a scan

- **GIVEN** a library scan at "12 of 40 files"
- **WHEN** the user opens the Library page and then Sources again
- **THEN** Sources shows the scan's progress, and its summary when it finishes
