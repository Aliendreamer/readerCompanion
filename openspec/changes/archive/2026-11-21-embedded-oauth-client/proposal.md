# Proposal

## Why

Release builds copy `google-oauth-client.json` next to the program, so anyone who downloads the app can open the
file and read the client ID and secret. The secret of a Desktop-app client cannot be truly hidden (the app must send
it to Google), but it must not ship as a readable file, and users must not have to create their own Google client.

## What Changes

- At build time the client ID and secret are read from the (git-crypt) `google-oauth-client.json`, scrambled, and
  compiled into a small dedicated assembly, `ReaderCompanion.Client`. No JSON file is copied into build or publish
  output.
- Release builds run [Obfuscar](https://github.com/obfuscar/obfuscar) (free, open source) over that assembly only:
  names are renamed and string literals encrypted, so a decompiler shows no readable client or unscrambling code.
  Only that assembly is obfuscated, because EF Core and Blazor rely on reflection the obfuscator would break.
- At run time the app unscrambles the client in memory when it talks to Google, and never writes it to disk.
- A `google-oauth-client.json` in the data folder still overrides the built-in client (for people who build their
  own copy with their own Google client).
- A build without the file (e.g. a clone without the git-crypt key) has no built-in client; Drive says it is not set
  up, as today.

## Limits

This stops casual reading (opening the folder, a text editor, a decompiler). A determined person can still recover
the client from the running program; that is true of every desktop app, and the client gives no access to anyone's
Drive (each user signs in and consents). Abuse is limited to the project's API quota and to impersonating the app's
name; both are handled in Google Cloud (quota caps, replacing the client).

## Capabilities

### Modified Capabilities

- `google-drive`: where the OAuth client comes from.

## Impact

New `src/ReaderCompanion.Client` (generated source, Obfuscar step); `GoogleClientConfig` (built-in client after the
data folder); `ReaderCompanion.Desktop.csproj` (no longer copies the JSON); `dotnet-tools.json` (Obfuscar 2.2.50);
`scripts/check-client-hidden.py` and `pnpm package:check`; `RELEASING.md`.
