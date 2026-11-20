# Proposal

## Why

M8 polish: the app has grown settings in several places and fails silently if it cannot start (for example when the
data folder is not writable or the database is damaged). Users need one place to see where their data lives, and a
clear message when something stops the app from opening.

## What Changes

- A **Settings** page in the sidebar: the data folder (library database and covers) with Show in folder, the backup
  folder and back-up-on-close setting (linking to Backup), and linked sources (local folders, last Goodreads import)
  linking to Sources.
- **Startup errors** are written to `startup-error.log` in the data folder (or the temp folder when the data folder
  is unusable) and shown in a native dialog with the error and the log's location, instead of a silent exit.

## Capabilities

### New Capabilities

- `app-settings`: the Settings overview and how startup failures are reported.

### Modified Capabilities

None.

## Impact

- Core: `ILibraryLocation` (data folder). Data: implementation; `StartupErrorLog`.
- UI: Settings page and sidebar link. Desktop: startup wrapped in error reporting with a native dialog.
- No new packages, no migration.
