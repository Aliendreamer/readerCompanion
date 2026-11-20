# Design

## Context

Books are recognised by content; `BookFile.Path` is where a file is, and scans follow moves. Library folders are
linked on the Sources page. The scanner must not run while files move. Decisions marked **(my call)** were made
without asking, to keep the roadmap loop moving.

## Goals / Non-Goals

**Goals:** a safe, previewed, undoable reorganisation of one linked folder; the library always points at the files.

**Non-Goals:** Google Drive (M7), deleting files (next M5 slice: duplicates to the trash), custom layout templates,
moving files between linked folders.

## Decisions

### 1. Pure planner in Core

`OrganisePlanner.Plan(root, layout, files)` where each `PlannedFile(FileId, Path, Title, Authors, Series,
SeriesIndex, Genres, Format)` gives `Move(FileId, From, To)` for files whose path changes. It builds the relative
path, sanitises each segment (invalid characters of both Windows and Linux → `_`; trailing dots and spaces trimmed;
reserved Windows names like `CON` get `_` appended; 100 characters max), and resolves collisions against other
targets and against an `exists(path)` callback (" (2)", " (3)"). Paths compare case-insensitively (my call: safe on
case-insensitive file systems). Series number: two digits, decimals kept (`02.5`); no number → title only.

### 2. Author as "Last, First" (my call)

The last word is the surname: "J.R.R. Tolkien" → "Tolkien, J.R.R."; a name already containing a comma or a single
word is kept as is.

### 3. Operation log

`FileOperation(Id, CreatedUtc, Root, UndoneUtc?)` and `FileMove(Id, OperationId, BookFileId, From, To, Done)`.
Apply writes the operation first, then for each move: `File.Move(from, to, overwrite: false)` (creating folders),
update `BookFile.Path`, mark the move done, save — so a crash leaves a log that matches the disk. Failures are
collected and reported. Undo walks the done moves of the latest not-undone operation in reverse.

### 4. Safety

Apply and undo refuse while a scan runs, and the scanner refuses to start while they run (a shared lock in Data:
`LibraryWriteGate`). Only folders under the chosen root that became empty are removed, deepest first, never the
root.

### 5. Page

`/organise`: folder select (linked folders), layout select, Preview → table of from/to (paths relative to the
root) with checkboxes and a count, Apply, then a summary (moved, failed). "Undo last organise" shows when there is an
operation to undo, with the date and file count.

## Risks / Trade-offs

- Moving thousands of files takes time → it runs off the UI thread; the page shows progress as a count.
- A file open in a reader cannot be moved on Windows → reported as failed, the rest continue.
