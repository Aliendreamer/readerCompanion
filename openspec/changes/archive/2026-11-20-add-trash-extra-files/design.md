# Design

## Context

The panel lists a book's files (`BookFileInfo`) with Open and Show in folder through `IFileLauncher`. Files are rows
in `BookFiles`. There is no delete anywhere yet. Decisions marked **(my call)** were made without asking, to keep the
roadmap loop moving.

## Goals / Non-Goals

**Goals:** send an extra file to the real system trash after confirmation, and keep the library consistent.

**Non-Goals:** permanent deletion, trashing whole books, emptying the trash, trash for Google Drive (M7).

## Decisions

### 1. `ITrash` in Core, two implementations

`bool MoveToTrash(string path)`. `FreedesktopTrash` (Data, used on Linux and other Unix) follows the freedesktop.org
Trash spec for the home trash: `$XDG_DATA_HOME/Trash` (default `~/.local/share/Trash`), the file moved into `files/`
under a free name and a `info/<name>.trashinfo` with the URL-encoded original path and the deletion time, written
first so desktop trash views can restore it. A file on another file system cannot be renamed into the home trash →
it is copied and the original deleted only after the copy is complete (my call: simpler than per-volume
`.Trash-$uid` folders and still restorable). `RecycleBinTrash` (Desktop, Windows) uses
`Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(..., RecycleOption.SendToRecycleBin)`.

### 2. `ILibraryBooks.TrashFileAsync(fileId)` does the whole job

One call in Data checks the rule (the book has another file), sends the file to the trash off the UI thread, and only
then removes the `BookFile` row; it returns `Trashed`, `LastFile`, `NotFound` or `TrashFailed`. The panel only hides the
button for a single file and shows the outcome. `BookFileInfo` gains `Id`.

### 3. The trash is chosen in one place

`PlatformTrash.ForThisMachine()` in Data: the Recycle Bin on Windows (`RecycleBinTrash`), the freedesktop trash on
Linux and BSD, and on anything else a trash that refuses. It is registered with `TryAdd`, so tests register a
recording trash first and never touch the real one.

### 4. Panel

Next to Open / Show in folder: "Move to trash…", shown only when the book has more than one file and this one is
present. It turns into "Move mort.pdf to the trash? The book keeps its other files." with Move to trash / Cancel.

## Risks / Trade-offs

- A failure between trashing and removing the row leaves a missing file in the library → the next scan marks it
  missing as usual; restoring it from the trash and rescanning repairs it.
- Across file systems the home trash gets a copy (then the original is deleted) → slow for big files on another
  disk; a failed copy is cleaned up. Per-volume `.Trash-$uid` folders can come later.
