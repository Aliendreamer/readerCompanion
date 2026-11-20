# Proposal

## Why

M5 ends with removing the extra file of a duplicate, after confirmation, to the system trash. After a merge (M2) or
with two identical copies, a book holds files the user no longer wants on disk.

## What Changes

- In the book panel, each present file of a book with more than one file offers **Move to trash**, which asks first
  and then sends the file to the system trash (Recycle Bin on Windows, the freedesktop trash on Linux).
- The library forgets the trashed file; the book keeps its other files and all its details and reading history.
- The last file of a book can never be trashed from the app.
- Restoring the file from the system trash and rescanning brings it back to the same book (books are recognised by
  content).

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `book-details`: the panel can move a book's extra file to the system trash.

## Impact

- Core: `ITrash`.
- Data: `FreedesktopTrash` (Linux), `RecycleBinTrash` (Windows), chosen by platform, and
  `ILibraryBooks.TrashFileAsync`.
- UI: Move to trash with confirmation in the panel.
- No new packages, no migration.
