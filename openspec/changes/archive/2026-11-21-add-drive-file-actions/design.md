# Design

## Context

Slice 2 stores Drive files as `gdrive://<id>/<display path>` with `DriveFileId`, and Drive folder sources by id and
display path. The organiser logs each operation (`FileOperation`, `FileMove`) and moves files one at a time, updating
the library in a transaction per move and moving the file back if that fails. Decisions marked **(my call)** were made
without asking.

## Decisions

### 1. Drive writes

`GoogleDriveFiles` gains `TrashAsync` (PATCH `trashed: true`), `MoveAsync` (PATCH `name` with `addParents` and
`removeParents`), `CreateFolderAsync` (POST a folder) and parents in `DriveItem`. Writes are refused before any call
when the link is read only.

### 2. Trash

`LibraryBooks.TrashFileAsync` keeps its rules (never the last present file; the library forgets the file only after
the trash took it) and sends a Drive file to Drive's trash instead of the system's. `TrashResult.ReadOnly` covers
read-only access.

### 3. Organise

A Drive root is the folder source's catalog-style path `gdrive://<folder id>/<display path>`, so the operation log
needs no change. `Organiser` keeps the orchestration and picks a mover per root: the existing local file moves, or a
`DriveMover` holding a cache of folder ids and names per operation. Preview plans on display paths (converted to the
OS separator for `OrganisePlanner` and back), with "taken" meaning a present Drive file of the library at that path.
Applying refuses unless the link is whole-Drive read and write. A move checks the file is still at its catalog path
and on Drive, creates missing folders, refuses a taken name in the target folder, then moves and renames in one call.
Folders left empty under the root go to Drive's trash (my call: recoverable, and an undo recreates them).
