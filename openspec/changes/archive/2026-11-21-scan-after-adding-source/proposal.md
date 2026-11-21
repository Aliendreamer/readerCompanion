# Proposal

## Why

Adding a library folder or Drive folder only saved it: its books appeared after a separate Scan press. The user
expected them to appear right away and thought Drive books were not recognised.

## What Changes

- Linking a local folder starts a library scan at once (with progress and summary, as the Scan button does).
- Adding a Drive folder, or picking Drive books, starts a Drive scan at once.
- A refused add (duplicate, already covered, nothing picked) starts no scan. The Scan buttons stay for rescans.

## Capabilities

### New Capabilities

### Modified Capabilities

- `library-scanning`: linking a folder scans it.
- `google-drive`: adding a Drive folder or picked books scans Drive.

## Impact

UI only: `Sources.razor` and `GoogleDriveSource.razor`, with their tests.
