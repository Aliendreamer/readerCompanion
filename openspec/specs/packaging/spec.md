# packaging Specification

## Purpose

How the app is packaged and installed: an MSI for Windows, a .deb and an AppImage for Linux, each checked for
the Google client before release.

## Requirements

### Requirement: Windows installer

The project SHALL build an MSI from the Windows package that installs the app for the current user without an
administrator prompt, adds a Start-menu shortcut with the app's icon, and lists the app in Windows' installed apps for
uninstalling. Installing a newer version SHALL replace an older one. Uninstalling SHALL leave the data folder.

#### Scenario: Install and uninstall

- **GIVEN** `ReaderCompanion-0.2.0-win-x64.msi`
- **WHEN** the user runs it, starts the app from the Start menu, then uninstalls it from installed apps
- **THEN** the app ran as version 0.2.0, and after uninstalling its program files are gone but the library is kept

#### Scenario: Upgrade

- **GIVEN** version 0.1.0 installed
- **WHEN** the 0.2.0 MSI is run
- **THEN** only 0.2.0 is installed and the library is kept

### Requirement: Linux packages

The project SHALL build a `.deb` that installs the app in `/opt/readercompanion` with a launcher and icon and depends
on WebKitGTK (`libwebkit2gtk-4.1-0`), and an AppImage that runs as a single file where WebKitGTK is installed.
Removing the `.deb` SHALL leave the data folder.

#### Scenario: Debian package

- **GIVEN** `ReaderCompanion-0.2.0-amd64.deb`
- **WHEN** it is installed with `apt install ./ReaderCompanion-0.2.0-amd64.deb`
- **THEN** WebKitGTK is installed if missing and the app appears in the applications menu

#### Scenario: AppImage

- **GIVEN** `ReaderCompanion-0.2.0-x86_64.AppImage` marked executable
- **WHEN** it is run
- **THEN** the app starts

### Requirement: Installers named by version and checked

Every installer SHALL be named `ReaderCompanion-<version>-<kind>` with the version from the git tag, and
`pnpm package:check` SHALL fail when the Google client file, or the client ID or secret as plain text, is inside any
installer.

#### Scenario: A leaked client

- **GIVEN** an installer that holds `google-oauth-client.json`
- **WHEN** `pnpm package:check` runs
- **THEN** it fails and names the installer
