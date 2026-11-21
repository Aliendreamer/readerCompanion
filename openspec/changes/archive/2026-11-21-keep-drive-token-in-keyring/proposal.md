# Proposal

## Why

On Linux the Drive link (the refresh token) is kept in a plain file readable by the user's account; anything
running as the user can read it. Windows already encrypts it with DPAPI.

## What Changes

- On Linux the Drive link is stored in the system keyring (GNOME Keyring, KWallet) through libsecret, encrypted and
  unlocked by the user's login.
- Without a usable keyring (no libsecret, no session bus, no Secret Service) it falls back to today's 0600 file.
- A link already in the file moves into the keyring the first time it is loaded, and the file is deleted.
- Windows is unchanged (DPAPI file).

## Capabilities

### New Capabilities

### Modified Capabilities

- `google-drive`: where the Drive link is kept.

## Impact

Data: `IKeyring`, `LibsecretKeyring` (P/Invoke to `libsecret-1.so.0`, already a dependency of WebKitGTK),
`DriveTokenFile` uses the keyring first; DI registration on Linux.
