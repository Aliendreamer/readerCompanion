# Proposal

## Why

Packages today are folders to zip by hand. People expect an installer: on Windows an MSI with a Start-menu entry and
an uninstaller, on Linux a `.deb` and an AppImage.

## What Changes

- **Windows MSI**, made with [WiX Toolset](https://wixtoolset.org) (a .NET tool pinned in `dotnet-tools.json`) from the
  `win-x64` package. It installs for the current user (no administrator prompt) into `%LocalAppData%\Programs`, adds a
  Start-menu shortcut with the app's icon, and an entry in Windows' installed apps to uninstall it. A newer version
  replaces an older one. WiX builds only on Windows: `pnpm installer:windows` is run from Windows (e.g. a
  PowerShell in the repository opened from WSL).
- **Linux `.deb`** (Debian/Ubuntu), made with `dpkg-deb` from the `linux-x64` package: the app in
  `/opt/readercompanion`, a launcher (`.desktop`) with the icon, and a dependency on WebKitGTK
  (`libwebkit2gtk-4.1-0`). `pnpm installer:deb`.
- **Linux AppImage**, made with `appimagetool` (pinned release) from the same package: one file to download and run
  on any distribution that has WebKitGTK. `pnpm installer:appimage`.
- Every installer carries the version from the git tag (MinVer) and is named `ReaderCompanion-<version>-<kind>`.
- `pnpm package:check` also opens each installer's contents and fails if the Google client is visible in them.
- Installers are unsigned for now: Windows shows "Windows protected your PC" (More info → Run anyway) until a
  code-signing certificate is bought.
- Uninstalling never removes the user's library, settings or Drive link (the data folder).

## Capabilities

### New Capabilities

- `packaging`: installers for Windows and Linux.

## Impact

`installer/` (WiX source, `.desktop` file, AppImage files), `package.json` scripts, `dotnet-tools.json` (WiX),
`scripts/check-client-hidden.py` (installer contents), `RELEASING.md`. No app code changes.
