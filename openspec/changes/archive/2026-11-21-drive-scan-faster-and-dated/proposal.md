# Proposal

## Why

A Drive scan lists folders one at a time, so a deep folder tree is slow. And the Drive card does not say when Drive
was last scanned or when each folder was linked.

## What Changes

- A Drive scan lists up to 10 folders at once (about 2,000–3,000 requests a minute, well within Google's limit
  of 12,000); a file found in two folders keeps the same path whatever the timing.
- The Drive card shows when Drive was last scanned ("Not scanned yet" before the first scan), and each Drive folder
  or picked book shows the date it was added.

## Capabilities

### New Capabilities

### Modified Capabilities

- `google-drive`: faster listing; last scan and linked dates.

## Impact

Data `DriveLibrary` (parallel listing, saves `AppPreferences.DriveLastScanUtc`); UI Drive card.
