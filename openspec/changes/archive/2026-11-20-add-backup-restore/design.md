# Design

## Context

The library is one SQLite file (`LibraryDataPaths.DatabasePath`); EF Core opens pooled connections through a context
factory. Preferences are a JSON record. `IFolderPicker` opens the system picker. Decisions marked **(my call)** were
made without asking, to keep the roadmap loop moving.

## Goals / Non-Goals

**Goals:** back up, list, restore, prune; automatic backup on exit; never lose data, including during a restore.

**Non-Goals:** cloud backups, backing up covers or preferences, scheduled backups while running, encrypting backups.

## Decisions

### 1. SQLite online backup both ways

Back up: open the live database and a new file, `live.BackupDatabase(file)`. Restore: check the file, back up the
live database first (named `readerCompanion-…-before-restore.db`), then `backup.BackupDatabase(live)` into the live
database, clear the connection pools, and run the migrations so an older backup is upgraded. Copying into the live
file through SQLite avoids replacing a file other connections hold open.

### 2. What is a backup

A file named `readerCompanion-YYYY-MM-DD-HHmmss[-before-restore].db` (local time). Listing and pruning only ever
touch files matching that name. Restore also opens the file read-only and requires the `__EFMigrationsHistory` and
`Books` tables, refusing anything else with a clear message.

### 3. `IBackups` in Core, `Backups` in Data

`BackupAsync(folder, now)`, `List(folder)`, `RestoreAsync(path, now)` (returns the safety backup),
`Prune(folder, keep)`. `now` is passed in for testable names. Names that collide within one second get `-2`, `-3`.

### 4. Preferences

`BackupFolder` (string?), `BackupOnExit` (bool, default false), `KeepBackups` (int, default 10, 1 to 100).

### 5. Page and exit hook

`/backup`: folder with "Choose…", "Back up now", the list with Restore per backup (inline confirm), and the
on-exit settings. After a restore the page reloads the app (`NavigateTo("/", forceLoad: true)`). The desktop host
handles the window's closing event: if back up on exit is on and a folder is set, it backs up and prunes, ignoring
failures (closing must never be blocked by a backup problem) (my call).

## Risks / Trade-offs

- A restore while a scan runs could mix states → `RestoreAsync` refuses while the scanner is busy, and the page
  disables Restore meanwhile.
- Pruning could delete the copy taken before a restore → `Prune` never deletes those.
- Closing the app writes a backup synchronously → a copy of a few MB takes well under a second.
