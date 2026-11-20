# Proposal

## Why

M5 will start moving files and M6 will import history. Before the app changes more, the user needs a safe undo for
the library itself: back up, wipe, restore, without losing anything.

## What Changes

- A **Backup** page in the sidebar. The user picks a backup folder once (system folder picker); it is remembered.
- **Back up now** writes a consistent copy of the library database, named with the date and time, using SQLite's
  online backup (safe while the app runs).
- The page **lists the backups** in the folder, newest first, with date and size.
- **Restore** a backup after confirmation: the current library is backed up first, then replaced by the backup,
  upgraded to the current version if it is older, and the app reloads. Files that are not readerCompanion backups
  are refused.
- Optional **back up on exit**, keeping the last N backups (10 by default).

## Capabilities

### New Capabilities

- `backup`: backing up, listing, restoring and pruning library backups, and the automatic backup on exit.

### Modified Capabilities

None.

## Impact

- Core: `IBackups` and `BackupInfo`; preferences gain backup folder, back up on exit and how many to keep.
- Data: `Backups` using `SqliteConnection.BackupDatabase`; migration after restore.
- UI: a Backup page and sidebar link.
- Desktop: back up on window close when enabled.
- No new packages. Covers and preferences are not part of a backup (covers are re-read from the books by a scan).
