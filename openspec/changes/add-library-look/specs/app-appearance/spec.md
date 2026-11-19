# Spec Delta

## Purpose

Defines readerCompanion's navigation shell and its light and dark themes: how the user moves between pages and
how the app's colours follow the system theme or the user's own choice.

## ADDED Requirements

### Requirement: Sidebar navigation

The app SHALL show a sidebar on every page with the app name and links to Library and Sources. The link for
the current page SHALL be highlighted.

#### Scenario: Current page highlighted

- **GIVEN** the user is on the Sources page
- **WHEN** the sidebar is shown
- **THEN** it links to Library and Sources, and Sources is highlighted

### Requirement: Theme follows the system by default

With no choice made, the app SHALL use a dark theme when the system reports a dark preference and a light theme
otherwise, and SHALL switch when the system preference changes while the app is open.

#### Scenario: Dark system

- **GIVEN** the user has not chosen a theme and the system prefers dark
- **WHEN** the app opens
- **THEN** it uses the dark theme

### Requirement: Theme choice

The sidebar SHALL offer a theme choice of System, Light and Dark. Light and Dark SHALL apply regardless of the
system preference; System SHALL return to following it. The choice SHALL be remembered across restarts.

#### Scenario: Forcing dark

- **GIVEN** the system prefers light
- **WHEN** the user chooses Dark
- **THEN** the app uses the dark theme immediately, and still does after a restart

#### Scenario: Back to system

- **GIVEN** the user had chosen Dark and the system prefers light
- **WHEN** the user chooses System
- **THEN** the app uses the light theme

### Requirement: Readable in both themes

Text, links, buttons, the "missing file" marker and error messages SHALL remain readable in both themes (normal
text at least 4.5:1 contrast against its background).

#### Scenario: Missing marker in dark theme

- **GIVEN** the dark theme and a book whose file is missing
- **WHEN** the library is shown
- **THEN** the "missing file" marker is clearly visible
