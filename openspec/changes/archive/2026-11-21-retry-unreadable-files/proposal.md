# Proposal

## Why

A file whose details could not be read stayed that way: unchanged files are never read again, so a passing failure
(a network hiccup, a reader bug fixed since) left the book titled from its file name for good. The scan summary also
listed such files without saying why.

## What Changes

- A file marked unreadable is read again on every scan, local or Drive, even when its content is unchanged.
- The scan summary shows the reason next to each file that could not be read.

## Capabilities

### New Capabilities

### Modified Capabilities

- `library-scanning`: unreadable files are retried; reasons are shown.

## Impact

Core `KnownFile.Unreadable`, `ScanReconciler`; Data `KnownFiles`; UI `ScanSummaryView`.
