# Releasing and packaging

## Versions

The version comes from git tags (`vX.Y.Z`) through [MinVer](https://github.com/adamralph/minver). Untagged builds
read `0.1.0-alpha.N`, where N counts the commits since the last tag.

## Making a release

1. Preview: `pnpm release:dry` shows the next version and the changelog entry, worked out from the Conventional
   Commits since the last tag.
2. The very first release: `pnpm release --release-as 0.1.0` (without it the tool assumes 1.0.0 was released).
3. Later releases: `pnpm release`. It updates `CHANGELOG.md`, commits it as `chore(release): vX.Y.Z` and tags
   `vX.Y.Z` locally. Nothing is pushed.

## Packages

- `pnpm package:linux` → `artifacts/linux-x64/` (self-contained, single executable plus `wwwroot/`).
- `pnpm package:windows` → `artifacts/win-x64/` (the same for Windows; it also builds from Linux).

Zip the folder to share it. The Linux build needs WebKitGTK (`libwebkit2gtk-4.1`) on the machine; the Windows build
needs the WebView2 runtime (part of Windows 11). Installers (MSI/MSIX, AppImage, .deb, Flatpak) are not set up yet.

## Google Drive client

Drive linking needs the app's OAuth client: in Google Cloud, create an OAuth client of type **Desktop app**, enable
the Google Drive API (and the Google Picker API), download the JSON and save it as `google-oauth-client.json` in the
repository root.

The build compiles the client ID and secret into `ReaderCompanion.Client`, scrambled with a key made for each build,
and Release builds obfuscate that assembly with [Obfuscar](https://www.obfuscar.com) (a local tool pinned in
`dotnet-tools.json`; `dotnet tool restore` installs it). The JSON is never copied to any output. Without it (or while
it is still encrypted), the Sources page says Drive is not set up in this build. Someone using their own Google client
can put their `google-oauth-client.json` in the data folder; it wins over the built-in one.

After packaging, `pnpm package:check` fails if any file under `artifacts/` is the client file or holds the client ID
or secret as plain text. Run it before uploading a package.

This keeps the client out of plain sight, not secret: a determined person can still recover it from the program, as
with every desktop app. It gives no access to anyone's Drive (each user signs in and agrees); misuse is limited to the
project's API quota and to imitating the app's name, both handled in Google Cloud (quota caps, replacing the client).

The file is committed encrypted with [git-crypt](https://github.com/AGWA/git-crypt) (`.gitattributes`). The
pre-commit hook refuses to commit it unencrypted.

- First time: `git-crypt init`, then export the key and keep it outside the repository (e.g. in a password
  manager): `git-crypt export-key ~/readerCompanion-git-crypt.key`. Check with `git-crypt status -e` after adding
  the file.
- A fresh clone: `git-crypt unlock ~/readerCompanion-git-crypt.key`. Without the key the file stays encrypted and
  the build simply has no Drive.
- Claude Code is denied reading the file (`.claude/settings.json`), so git in its sandbox cannot hash it either: run
  `git update-index --skip-worktree google-oauth-client.json` once per clone. To commit a new client file, first
  `git update-index --no-skip-worktree google-oauth-client.json`.
