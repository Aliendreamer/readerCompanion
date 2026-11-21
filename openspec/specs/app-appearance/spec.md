# app-appearance Specification

## Purpose

Defines readerCompanion's navigation shell and its light and dark themes: how the user moves between pages and
how the app's colours follow the system theme or the user's own choice.

## Requirements

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

### Requirement: Light/dark toggle

The sidebar SHALL have a light/dark toggle showing the theme currently in use. Flipping it SHALL switch to the
other theme immediately, regardless of the system preference, and the choice SHALL be remembered across
restarts. While the user's choice is in effect, a "Use system theme" option SHALL be shown that returns to
following the system and forgets the choice.

#### Scenario: Flipping to dark

- **GIVEN** the user has not chosen a theme and the system prefers light
- **WHEN** the user flips the toggle
- **THEN** the app uses the dark theme immediately, and still does after a restart

#### Scenario: Back to system

- **GIVEN** the user flipped to dark and the system prefers light
- **WHEN** the user picks "Use system theme"
- **THEN** the app uses the light theme, follows the system from then on, and the option is hidden

### Requirement: Readable in both themes

Text, links, buttons, the "missing file" marker and error messages SHALL remain readable in both themes (normal
text at least 4.5:1 contrast against its background).

#### Scenario: Missing marker in dark theme

- **GIVEN** the dark theme and a book whose file is missing
- **WHEN** the library is shown
- **THEN** the "missing file" marker is clearly visible

### Requirement: Text size

The Settings page SHALL offer a text size of 100%, 115%, 130% or 150%, defaulting to 115%. The whole window
(menu, pages, covers) SHALL be shown at that size, applied at start and as soon as the user picks another size.

#### Scenario: Larger text

- **GIVEN** the text size is 115%
- **WHEN** the user picks 130% in Settings
- **THEN** the window is shown at 130% at once, and still is after a restart
