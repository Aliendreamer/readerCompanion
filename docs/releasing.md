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
