# app-settings Specification

## Purpose

Gives the user one page that says where their data lives and what is linked, and makes sure a failure at startup is
explained instead of the app silently not opening.

## Requirements

### Requirement: Settings overview

The Settings page SHALL show the data folder holding the library database and covers, with Show in folder; the
backup folder (or that none is chosen) and whether backups run on close, linking to the Backup page; and the linked
sources — the number of local folders and the date of the last Goodreads import — linking to Sources.

#### Scenario: Where is my data

- **GIVEN** the library in `~/.local/share/ReaderCompanion`
- **WHEN** the user opens Settings
- **THEN** the page shows that folder and opens it on Show in folder

### Requirement: Startup errors are explained

When the app cannot start, it SHALL write the error with its details to `startup-error.log` in the data folder (or a
uniquely named file in the system temp folder if the data folder cannot be written) and SHALL show a dialog with a short
explanation, the error message and the log's path, then exit.

#### Scenario: Damaged database

- **GIVEN** a library database that cannot be opened
- **WHEN** the user starts the app
- **THEN** a dialog says the app could not start, shows the error and where the log is, and the log holds the details
