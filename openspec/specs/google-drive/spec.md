# google-drive Specification

## Purpose

Lets anyone who installs the app link their own Google Drive from the Sources page, at an access level they
choose, so Drive books can later appear and be managed beside local ones.

## Requirements

### Requirement: Drive access level

The Settings page SHALL offer the Google Drive access level, remembered: only books the user picks, the whole Drive
read only, or the whole Drive read and write (the default). A change SHALL take effect the next time Drive is linked,
and while linked at another level the page SHALL say so.

#### Scenario: Change while linked

- **GIVEN** Drive linked with the whole Drive, read and write
- **WHEN** the user chooses only picked books
- **THEN** the choice is remembered and the page says to link Drive again to use it

### Requirement: Link Google Drive

The Sources page SHALL show a Google Drive card. When the app has an OAuth client, Link Google Drive SHALL open the
system browser at Google's sign-in for the chosen access level, wait for the answer on a loopback address, and on
success keep the user's token protected and show the linked account and level. The token SHALL be kept in the
system keyring on Linux when one is available, encrypted for the user on Windows, and otherwise in a file in the
data folder readable only by the user; a token found in that file SHALL move into the keyring when it becomes
available. The user SHALL be able to cancel the wait. A refused or failed sign-in SHALL leave Drive unlinked and
say why. When the app has no OAuth client, the card SHALL say that Drive is not set up in this build and offer no
link.

#### Scenario: Link

- **GIVEN** an OAuth client and Drive not linked
- **WHEN** the user presses Link Google Drive and signs in in the browser
- **THEN** the card shows the account and the access level, and the token is kept for the next start

#### Scenario: Token in the keyring

- **GIVEN** Linux with a running keyring and a Drive link stored in the data folder's file by an older version
- **WHEN** the app starts
- **THEN** the link is in the keyring, the file is gone, and Drive is still linked

#### Scenario: No keyring

- **GIVEN** Linux without a keyring service
- **WHEN** the user links Drive
- **THEN** the token is kept in the data folder's file, readable only by the user

#### Scenario: Sign-in refused

- **WHEN** the user denies access in the browser
- **THEN** the card says access was not given and Drive stays unlinked

#### Scenario: No OAuth client

- **GIVEN** a build without `google-oauth-client.json`
- **WHEN** the Sources page is shown
- **THEN** the Drive card says Drive is not set up in this build and has no Link button

### Requirement: Unlink Google Drive

Unlink SHALL revoke the token at Google when it can, and SHALL always delete the stored token, leaving the library
unchanged. If Google later rejects the stored token (revoked or expired), the app SHALL treat Drive as unlinked and
say that it needs linking again.

#### Scenario: Unlink

- **GIVEN** Drive linked
- **WHEN** the user presses Unlink
- **THEN** the token is revoked and deleted, and the card offers Link Google Drive again

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

### Requirement: Trash Drive files

Move to trash SHALL work for a Drive file that is not the book's last present file: the file SHALL go to Google
Drive's trash and the library SHALL forget it. With read-only Drive access it SHALL be refused and say so; if Drive
refuses, nothing SHALL change.

#### Scenario: Trash a Drive copy

- **GIVEN** a book with a local EPUB and the same EPUB on Drive, and read and write access
- **WHEN** the user moves the Drive copy to the trash
- **THEN** it is in Google Drive's trash and the book keeps its local file

### Requirement: Organise Drive folders

With whole-Drive read and write access, the Organise page SHALL offer the chosen Drive folders. Preview, apply and
undo SHALL behave as for a local folder: files are renamed and moved within the Drive folder into the layout, missing
folders are created, a file whose target name is taken is reported and not moved, folders emptied by the moves go to
Drive's trash, and the library follows each move.

#### Scenario: Organise and undo on Drive

- **GIVEN** a Drive folder "Books" holding `hobbit_final.epub`, by J.R.R. Tolkien, genre Fantasy
- **WHEN** the user organises "Books" by Genre / Author / Title and applies it
- **THEN** the file is `Books/Fantasy/Tolkien, J.R.R/The Hobbit.epub` on Drive and in the library; undo puts it back

### Requirement: Drive progress

While a Drive scan runs, the Drive card SHALL show what it is doing: while looking through Drive, how many folders
and books it has seen so far; while checking books Drive has no checksum for, and while reading new or changed
books, how many of how many, with a progress bar. While a Drive book downloads to be opened, its Open link SHALL
show the percentage downloaded.

#### Scenario: Scanning a large folder

- **GIVEN** a Drive folder with 12 subfolders and 340 books
- **WHEN** the user presses Scan Drive
- **THEN** the card shows the folders and books seen so far, then "Reading new books from Drive… 3 of 20" with a bar

#### Scenario: Opening a large book

- **WHEN** the user opens a 40 MB PDF from Drive
- **THEN** its Open link reads "Downloading… 25%" and climbs until the book opens

### Requirement: Drive scan dates

The Drive card SHALL show when Drive was last scanned successfully, or that it has not been scanned yet, and SHALL
show for each Drive folder and picked book the date it was added. A Drive scan SHALL look through up to 10 folders at
once.

#### Scenario: After a scan

- **GIVEN** the Drive folder "Books" added on 21 Nov 2026
- **WHEN** the user scans Drive
- **THEN** the card lists "Books" with "added 21 Nov 2026" and says "Last scan:" with today's date and time

### Requirement: Read Drive books in place

A Drive scan SHALL read a new or changed EPUB or PDF's details and cover by fetching only the parts of the file the
reader needs, not the whole file, and SHALL read up to 10 books at once, each slot starting the next book as soon as
its own is done. The result SHALL be the same as reading the whole file. A read that Google answers with "too many
requests" or a passing server error SHALL be tried again after a short wait, twice at most.

#### Scenario: A large EPUB

- **GIVEN** a 4 MB EPUB on Drive whose package file and cover take 30 KB
- **WHEN** Drive is scanned
- **THEN** the book gets its title, authors and cover, and far less than the whole file is downloaded

#### Scenario: Drive asks to slow down

- **WHEN** Google answers a read with "too many requests" once
- **THEN** the read is tried again after a short wait and the scan goes on

### Requirement: The OAuth client is built into the program

When `google-oauth-client.json` is present at build time, its client ID and secret SHALL be compiled into the
program in scrambled form, and no file holding them SHALL be copied to the build or publish output. Release builds
SHALL obfuscate the assembly that holds and unscrambles them. The app SHALL unscramble them only in memory and SHALL
never write them to disk. A `google-oauth-client.json` in the data folder SHALL be used instead when present.

#### Scenario: A release build

- **GIVEN** a release build made with `google-oauth-client.json` present
- **WHEN** its output folder is searched for the client ID and the client secret as plain text, in every file
- **THEN** neither is found, and there is no `google-oauth-client.json`
- **AND** the app can link Google Drive

#### Scenario: A build without the client file

- **GIVEN** a build made without `google-oauth-client.json`
- **WHEN** the Sources page is shown
- **THEN** the Drive card says Drive is not set up in this build

#### Scenario: The user's own client

- **GIVEN** a build with a built-in client, and another client's `google-oauth-client.json` in the data folder
- **WHEN** the user links Google Drive
- **THEN** the data folder's client is used
