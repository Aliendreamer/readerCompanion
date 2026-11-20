# Proposal

## Why

M5: manage the files themselves, not only the catalog. Downloads land with messy names in flat folders; the user
wants a tidy, predictable layout, without ever risking a book or its reading history.

## What Changes

- An **Organise** page: pick a linked library folder and a layout — `Genre/Author/Title` (default),
  `Author/Series/NN - Title` or `Author/Title`.
- A **preview** lists every file that would move, from where to where, each with a checkbox; nothing moves before
  the user applies it.
- **Apply** moves the ticked files, never overwriting (a taken name gets " (2)"), updates the library's file paths in
  the same step, and removes folders left empty inside the library folder.
- Every apply is **recorded**; **Undo last organise** moves the files back where they were, skipping any file that was
  changed or moved since, and says which.
- File and folder names are made safe for Windows and Linux.

## Capabilities

### New Capabilities

- `organise-files`: planning, previewing, applying and undoing file moves into a layout built from book details.

### Modified Capabilities

None.

## Impact

- Core: `OrganisePlanner` (pure: names, layouts, collisions) with unit tests.
- Data: `IOrganiser` (preview, apply, undo) and a migration for the operation log (operations and their moves).
- UI: an Organise page and sidebar link.
- No new packages.
