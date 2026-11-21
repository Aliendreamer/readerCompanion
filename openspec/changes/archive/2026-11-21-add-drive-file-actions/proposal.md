# Proposal

## Why

M7, slice 3 of 3, and the roadmap's "done when": changes made in the app show up in Drive. With write access, the
M5 file actions (organise with preview and undo, and moving an extra file to the trash) should work on Drive files
too.

## What Changes

- **Move to trash** in the details panel works for a Drive file (not the book's last one): it goes to Google Drive's
  trash, where the user can restore it. With read-only access it is refused with a clear message.
- **Organise** lists chosen Drive folders beside local folders when Drive is linked with whole-Drive read and write
  access. Preview, apply and undo work as for a local folder: files are renamed and moved inside that Drive folder,
  missing folders are created, a taken name is reported and skipped, folders emptied by the move go to Drive's trash,
  and the library follows every move.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `google-drive`: Drive trash and organise.

## Impact

- Data: Drive write calls (update name/parents, trash, create folder); `LibraryBooks.TrashFileAsync` handles Drive
  files; `Organiser` moves through a local or Drive mover chosen by the root; `TrashResult.ReadOnly`.
- UI: Organise page lists Drive folders; the details panel offers Move to trash for Drive files.
- No migration: an organise of a Drive folder is logged like a local one, with Drive catalog paths.
