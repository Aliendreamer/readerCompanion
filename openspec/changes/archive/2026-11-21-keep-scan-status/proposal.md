# Proposal

## Why

Leaving the Sources page while a library or Drive scan runs, and coming back, lost the scan's progress and summary:
they lived in the page, while the scan itself went on.

## What Changes

- An app-wide scan status runs each scan (library folders, Google Drive) and keeps its progress, summary and
  failure; the Sources page and the Drive card show it, so they pick up a running or finished scan when opened again.

## Capabilities

### New Capabilities

### Modified Capabilities

- `library-scanning`: scan progress and summary survive leaving the page (also for Drive scans).

## Impact

UI: `ScanStatus` / `TrackedScan` singletons; `Sources.razor` and `GoogleDriveSource.razor` use them.
