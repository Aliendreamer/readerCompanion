# Proposal

## Why

Reading a 2,400-book Drive library was still slow: reads went in batches that waited for their slowest book, each
EPUB made its round trips one after another, and only 10 books were read at once.

## What Changes

- Up to 20 books are read at once, each slot taking the next book as soon as it is free; the catalog is still
  changed in order, with at most 100 books read and waiting.
- An EPUB's first and last pieces are fetched together.
- A read Google answers with "too many requests" (429) or a passing server error is retried twice after a short wait.

## Capabilities

### New Capabilities

### Modified Capabilities

- `google-drive`: faster in-place reading; retries.

## Impact

Data `DriveLibrary`, `DriveRangeStream.PrefetchAsync`, `GoogleDriveFiles.SendAsync`.
