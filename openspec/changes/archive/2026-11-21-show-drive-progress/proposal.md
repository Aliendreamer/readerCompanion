# Proposal

## Why

Working with Google Drive can take a while, and most of it showed only "Working with Google Drive…": walking a large
Drive folder tree, hashing files Drive has no checksum for, and downloading a book to open it.

## What Changes

- A Drive scan reports what it is doing: while looking through folders, how many folders and books it has seen;
  while checking files without a Drive checksum and while reading new books, "x of y" with a progress bar.
- Opening a Drive book shows the download's percentage.

## Capabilities

### New Capabilities

### Modified Capabilities

- `google-drive`: scan and open show progress.

## Impact

Core `ScanProgress` gains `Stage` and `FoldersListed`; `IDriveLibrary.DownloadAsync` takes a percentage progress;
Data `DriveLibrary` reports them; UI Drive card and details panel show them.
