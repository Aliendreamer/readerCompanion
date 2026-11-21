# app-appearance Specification

## Purpose

Defines readerCompanion's navigation shell and its light and dark themes: how the user moves between pages and
how the app's colours follow the system theme or the user's own choice.

## Requirements

### Requirement: Sidebar navigation

The app SHALL show a toolbar across the top of every page, below the window's title, with a button for each page
(Library, Reading, Duplicates, Organise, Sources, Backup) and, at its right end, the theme switch, Settings and Help.
The button of the current page SHALL show as pressed. The app's logo and name SHALL be in the window's own title
strip.

#### Scenario: Current page highlighted

- **GIVEN** the user is on the Sources page
- **WHEN** the toolbar is shown
- **THEN** it has a button for every page, and Sources shows as pressed

### Requirement: Theme follows the system by default

With no choice made, the app SHALL use a dark theme when the system reports a dark preference and a light theme
otherwise, and SHALL switch when the system preference changes while the app is open.

#### Scenario: Dark system

- **GIVEN** the user has not chosen a theme and the system prefers dark
- **WHEN** the app opens
- **THEN** it uses the dark theme

### Requirement: Light/dark toggle

The toolbar SHALL have a light/dark switch showing the theme currently in use. Flipping it SHALL switch to the other
theme immediately, regardless of the system preference, and the choice SHALL be remembered across restarts. While the
user's choice is in effect, the Settings page SHALL offer "Use system theme", which returns to following the system and
forgets the choice.

#### Scenario: Flipping to dark

- **GIVEN** the user has not chosen a theme and the system prefers light
- **WHEN** the user flips the switch
- **THEN** the app uses the dark theme immediately, and still does after a restart

#### Scenario: Back to system

- **GIVEN** the user flipped to dark and the system prefers light
- **WHEN** the user picks "Use system theme" in Settings
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

### Requirement: App window frame

The desktop window SHALL be drawn without the system title bar, unless the user chose the system title bar in
Settings (applied at the next start). A strip at the top of the page, in the app's theme, SHALL offer Full
screen, Minimise, Maximise or Restore, and Close; dragging the strip or the sidebar's brand SHALL move the window
and double-clicking the strip SHALL maximise or restore it. Every edge and corner SHALL resize the window while
it is neither maximised nor full screen.

#### Scenario: Move and maximise

- **WHEN** the user drags the top strip, then double-clicks it
- **THEN** the window moves with the pointer, then fills the screen; double-clicking again restores it

#### Scenario: System title bar

- **GIVEN** the user ticked "Use the system title bar" and restarted
- **THEN** the system draws the title bar and the app shows no strip

### Requirement: Full screen

F11 and the Full screen button SHALL toggle full screen; Escape SHALL leave it. In full screen the top strip and
resize edges SHALL be hidden.

#### Scenario: Leave full screen

- **GIVEN** the window is full screen
- **WHEN** the user presses Escape
- **THEN** the window returns to its previous size and the top strip shows again

### Requirement: Window placement

At its first start the window SHALL be 1920 × 1080 and centred when the screen's work area holds that size, and
maximised otherwise. When closed, the window's size, position and maximised state SHALL be remembered and restored
at the next start; a remembered window that would lie off every screen SHALL be moved onto the main screen and
shrunk to fit it.

#### Scenario: Small laptop screen

- **GIVEN** a first start on a 1366 × 768 screen
- **THEN** the window opens maximised

#### Scenario: Reopened as left

- **GIVEN** the user left the window at 1500 × 900 near the top left, not maximised
- **WHEN** the app starts again
- **THEN** the window is 1500 × 900 at the same place

### Requirement: Status bar

A status bar along the bottom of the window SHALL show, on the Library page, the number of books ("310 books", or
"42 of 310 books" while narrowed, "No books" when empty), and on every page the progress of a running library or Drive
scan (both when both run). It SHALL show nothing else.

#### Scenario: Following a scan from another page

- **GIVEN** a Drive scan started on Sources, reading 400 of 2,400 books
- **WHEN** the user opens the Library
- **THEN** the status bar shows the Drive scan's progress next to the book count
