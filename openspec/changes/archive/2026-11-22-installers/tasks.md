# Tasks

## 1. Linux

- [x] 1.1 `installer/linux/readercompanion.desktop` and `pnpm installer:deb` (`dpkg-deb`) — check: `dpkg-deb --info`
  and `--contents` list the version, the WebKitGTK dependency, `/opt/readercompanion` and the launcher
- [x] 1.2 `pnpm installer:appimage` (`appimagetool`, exact release pinned, checksum checked) — check: the AppImage
  extracts and holds the app
- [x] 1.3 User check: install the `.deb`, start from the menu, remove; run the AppImage

## 2. Windows

- [x] 2.1 WiX (exact version in `dotnet-tools.json`) source in `installer/windows/`: per-user install, Start-menu
  shortcut, upgrade code, version from MinVer; `pnpm installer:windows`
- [x] 2.2 User check on Windows: install, start, upgrade from an older MSI, uninstall; the library is kept

## 3. Checks and docs

- [x] 3.1 `scripts/check-client-hidden.py` looks inside `.deb`, AppImage and MSI files — tested with a client planted
  in a `.deb` and an AppImage; the MSI path is checked by the user on Windows (2.2)
- [x] 3.2 `RELEASING.md`: building each installer, where it runs, the unsigned-installer warning
