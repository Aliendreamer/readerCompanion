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
repository root. Builds and packages copy it next to the executable. Without it (or while it is still encrypted),
the Sources page says Drive is not set up in this build. The file can also be placed in the data folder.

The file is committed encrypted with [git-crypt](https://github.com/AGWA/git-crypt) (`.gitattributes`). The
pre-commit hook refuses to commit it unencrypted.

- First time: `git-crypt init`, then export the key and keep it outside the repository (e.g. in a password
  manager): `git-crypt export-key ~/readerCompanion-git-crypt.key`. Check with `git-crypt status -e` after adding
  the file.
- A fresh clone: `git-crypt unlock ~/readerCompanion-git-crypt.key`. Without the key the file stays encrypted and
  the build simply has no Drive.
