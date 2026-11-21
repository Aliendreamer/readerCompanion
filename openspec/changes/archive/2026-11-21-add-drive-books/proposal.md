# Proposal

## Why

M7, slice 2 of 3. Linking Drive (slice 1) does nothing visible yet. Drive books should appear in the library beside
local ones, with the same metadata, duplicate matching and reading history, and open from the details panel.

## What Changes

- **Whole Drive access**: the Drive card lists chosen Drive folders; **Add folder…** browses Drive's folders inside
  the app; **Remove** marks that folder's books' files missing (the books stay).
- **Only books I pick**: **Add books from Drive…** opens Google's picker in the browser (multi-select); the picked
  files are added; each can be removed again.
- **Scan Drive** lists the chosen folders and picked files, and brings the library in line exactly like a local scan:
  new books are added, moved or renamed files are followed, files gone from Drive are marked missing. Drive's own
  SHA-256 checksum identifies content, so a Drive copy of a local book joins that book. Only new or changed files are
  downloaded, once, to read their details and cover. If a chosen folder cannot be read, the scan stops and nothing
  changes.
- **Details panel**: a Drive file shows "Google Drive" and its path; **Open** downloads it to a cache in the data
  folder (reused while unchanged) and opens it; **Show in Drive** opens it on drive.google.com. Moving Drive files to
  the trash and organising them come in slice 3.
- Unlinking Drive keeps the chosen folders, picked files and books; opening a Drive file then asks to link again.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `google-drive`: Drive folders and picked files as sources, Drive scan, opening Drive books.

## Impact

- Core: `DriveSource` entity, `IDriveLibrary`, `DrivePath`; `BookFile.DriveFileId`; `FoundFile.DriveFileId`;
  `BookFileInfo.DriveFileId`; `IDriveAccount.PickFilesAsync`.
- Data: migration (DriveSources table, BookFiles.DriveFileId); Drive REST calls; `DriveLibrary` (sources, scan,
  open); `CatalogWriter` takes a file opener; local scan, folder unlink and organise ignore Drive files.
- UI: Drive card sources, folder browser, picker, scan; details panel Open / Show in Drive for Drive files.
