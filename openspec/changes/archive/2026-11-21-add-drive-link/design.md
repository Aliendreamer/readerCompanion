# Design

## Context

Desktop app, Photino web view, no web server. Google's "Desktop app" OAuth clients use the loopback redirect
(`http://127.0.0.1:<port>`) with PKCE; their client secret is not confidential. Google's file picker for desktop apps
is the same browser sign-in with `trigger_onepick=true`, and allows only the `drive.file` scope. Decisions marked
**(my call)** were made without asking.

## Goals / Non-Goals

**Goals:** link and unlink per user with a chosen access level; keep tokens usable across restarts; give later slices
an access token on demand.

**Non-Goals:** Drive books, the file picker and Drive file actions (slices 2 and 3); several Google accounts at once
(my call: one account).

## Decisions

### 1. Access levels and scopes

`DriveAccess { PickedFiles, WholeDriveReadOnly, WholeDrive }` → `drive.file`, `drive.readonly`, `drive`. One scope per
sign-in (the picker cannot mix scopes), so no `openid`/`email`: the account name comes from Drive's `about` call
(`user(displayName,emailAddress)`), shown only in the app.

### 2. Sign-in

`GoogleOAuth` (Core, pure): builds the authorization URL (`response_type=code`, `access_type=offline`,
`prompt=consent`, PKCE S256, random `state`) and reads the redirect (`code`, `error`, `state`, `picked_file_ids` for
slice 2). `LoopbackSignIn` (Data) listens with `HttpListener` on `127.0.0.1` at a free port, opens the URL through
`IWebBrowser`, answers the browser with a short "you can close this tab" page and returns the query; it gives up
after 5 minutes or on cancel. The code is exchanged at `oauth2.googleapis.com/token`.

### 3. Tokens

`drive-token.json` in the data folder: refresh token, access level, account name, linked time. On Windows it is
encrypted with DPAPI (current user); elsewhere the file is created with mode 0600 (my call: no keyring dependency on
Linux). Access tokens stay in memory and are refreshed a minute before expiry; `invalid_grant` deletes the file and
raises `DriveLinkLostException` with "Google Drive needs linking again".

### 4. Client file

`GoogleClientConfig.TryLoad` reads Google's download format (`installed.client_id`, `installed.client_secret`) from
`AppContext.BaseDirectory` first, then the data folder. The Desktop project copies `google-oauth-client.json` from the
repo root into the output when it exists; `.gitignore` lists it.

### 5. UI

`DriveSource` card on Sources (state: not set up / unlinked / waiting with Cancel / linked with Unlink, errors in
an alert). Settings gains a Google Drive card with three radio buttons bound to `AppPreferences.DriveAccess`.
