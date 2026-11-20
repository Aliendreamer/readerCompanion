# Proposal

## Why

M8: many PDFs and some EPUBs have no cover, so the grid fills with placeholders. Open Library serves covers by ISBN
for free. Fetching them is optional because it contacts the internet.

## What Changes

- On the Settings page, an opt-in **"Get missing covers from Open Library"** switch (off by default) and a **Fetch
  missing covers** button with progress and Stop.
- Only books with an ISBN and no cover are looked up, one request per second, at
  `covers.openlibrary.org/b/isbn/<isbn>-L.jpg?default=false`; a cover found is saved in the covers folder and used
  from then on. Books without a cover on Open Library are left as they are.
- Nothing contacts the internet unless the switch is on and the user presses Fetch.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `app-settings`: the Settings page offers fetching missing covers from Open Library.

## Impact

- Core: `ICoverFetcher`. Data: `OpenLibraryCovers` with an `HttpClient`; preferences gain the switch.
- UI: the switch, button, progress and Stop on Settings.
- No new packages, no migration.
