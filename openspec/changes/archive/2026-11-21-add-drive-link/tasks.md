# Tasks

## 1. Core

- [x] 1.1 `DriveAccess`, scopes, preference; `GoogleOAuth` URL and redirect parsing, PKCE — Core tests

## 2. Data

- [x] 2.1 `GoogleClientConfig.TryLoad` — tests: base folder first, data folder, missing, damaged
- [x] 2.2 Token file (DPAPI on Windows, 0600 elsewhere) — tests: round trip, mode, damaged file reads as unlinked
- [x] 2.3 `LoopbackSignIn` — test with a real loopback request; cancel
- [x] 2.4 `GoogleDriveAccount`: link, refresh, invalid_grant, unlink/revoke — tests with a fake handler and sign-in

## 3. UI and Desktop

- [x] 3.1 `DriveSource` card on Sources; Drive access card on Settings — bUnit
- [x] 3.2 Desktop browser opener, client file copy, `.gitignore`

## 4. Verification

- [x] 4.1 Quality gates; simplify and code review; manual check with the real client (user)
