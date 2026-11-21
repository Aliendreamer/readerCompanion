# Proposal

## Why

M7, slice 1 of 3. Anyone who installs the app should be able to press **Link Google Drive**, sign in with Google in
their browser and be linked, with no setup of their own. The user picked two access levels, chosen in Settings: only
the files they pick (no Google security assessment needed for a public release) or the whole Drive.

## What Changes

- **Settings → Google Drive access**: *Only books I pick* (default), *Whole Drive, read only*, *Whole Drive, read and
  write*. A change applies the next time Drive is linked.
- **Sources → Google Drive card**: Link Google Drive opens the system browser at Google's sign-in; the app receives
  the answer on a loopback address (`127.0.0.1`), stores the user's own token locally and shows who is linked and at
  which level. Cancel stops waiting; Unlink revokes the token at Google and forgets it.
- The app's OAuth client (`google-oauth-client.json`, Google's "Desktop app" download) is read from next to the
  executable or from the data folder and is never committed. Without it the card says Drive is not set up in this
  build.
- Later slices: Drive books in the library and Open (slice 2); Organise and Trash on Drive (slice 3).

## Capabilities

### New Capabilities

- `google-drive`: linking Google Drive with a chosen access level.

### Modified Capabilities

None.

## Impact

- Core: `Drive/` (access levels, OAuth URL and redirect helpers, `IDriveAccount`, `IWebBrowser`); preference
  `DriveAccess`.
- Data: client config loader, loopback sign-in, token exchange/refresh/revoke over `HttpClient`, token file.
- UI: Drive card on Sources, access card on Settings. Desktop: opens the browser; copies the client file into the
  build when present. `.gitignore` gains the client file.
- No new packages (plain HTTP, no Google library: my call, the API surface is small), no migration.
