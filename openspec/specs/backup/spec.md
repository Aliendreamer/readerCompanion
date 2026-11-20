# backup Specification

## Purpose

Lets the user keep copies of the library database in a folder of their choice and go back to one of them, so no
change made by the app or the user is final.

## Requirements

### Requirement: Back up now

The user SHALL pick a backup folder, remembered across restarts, and SHALL be able to write a backup to it at any time.
A backup SHALL be a consistent copy of the library database named `readerCompanion-YYYY-MM-DD-HHmmss.db`; afterwards
only the newest N backups are kept, as for backups on exit.

#### Scenario: First backup

- **GIVEN** a library of 310 books and a chosen backup folder
- **WHEN** the user presses "Back up now"
- **THEN** a new backup file appears in the folder and the list shows it first

### Requirement: List backups

The Backup page SHALL list the backups in the chosen folder, newest first, with their date and size, ignoring other
files.

#### Scenario: Other files ignored

- **GIVEN** a backup folder holding two backups and a photo
- **WHEN** the Backup page is shown
- **THEN** it lists the two backups only

### Requirement: Restore

Restoring a backup SHALL ask for confirmation, then back up the current library to the same folder, replace the
library with the backup, upgrade it if it was made by an older version, and reload the app. A file that is not a
readerCompanion library SHALL be refused and the library left unchanged, and so SHALL any restore while a scan is
running.

#### Scenario: Restore after a mistake

- **GIVEN** a backup made yesterday, and today's library where a merge removed a book
- **WHEN** the user restores yesterday's backup and confirms
- **THEN** the removed book is back, and a backup of today's library is in the folder

#### Scenario: Not a backup

- **GIVEN** a file that is not a readerCompanion library, named like a backup
- **WHEN** the user tries to restore it
- **THEN** the app says it is not a readerCompanion backup and the library is unchanged

### Requirement: Back up on exit

When the user turns on "Back up when closing", the app SHALL write a backup when its window closes and then keep
only the newest N backups in the folder (N chosen by the user, 10 by default), never deleting other files or the
copies taken before a restore.

#### Scenario: Keep the last ten

- **GIVEN** "Back up when closing" on, keeping 10, and ten backups in the folder
- **WHEN** the user closes the app
- **THEN** the folder holds the ten newest backups, including the new one
