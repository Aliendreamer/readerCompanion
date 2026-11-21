# Proposal

## Why

Previewing, applying and undoing an organise only greyed out the buttons. On Google Drive, or with thousands of
files, that looked like nothing was happening.

## What Changes

- The organiser reports progress: working out the moves (planning, for Drive also listing the folder tree), moving
  files "x of y", moving files back "x of y", and removing emptied folders.
- The Organise page shows what is being done with a progress bar ("x of y" where known).

## Capabilities

### New Capabilities

### Modified Capabilities

- `organise-files`: progress while organising.

## Impact

Core `OrganiseProgress`, `IOrganiser` methods take a progress; Data `Organiser` reports it; UI `OrganisePage` shows it.
