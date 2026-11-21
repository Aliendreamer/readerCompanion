# Spec Delta

## MODIFIED Requirements

### Requirement: Sidebar navigation

The app SHALL show a toolbar across the top of every page, below the window's title, with a button for each page
(Library, Reading, Duplicates, Organise, Sources, Backup) and, at its right end, the theme switch, Settings and Help.
The button of the current page SHALL show as pressed. The app's logo and name SHALL be in the window's own title
strip.

#### Scenario: Current page highlighted

- **GIVEN** the user is on the Sources page
- **WHEN** the toolbar is shown
- **THEN** it has a button for every page, and Sources shows as pressed

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

## ADDED Requirements

### Requirement: Status bar

A status bar along the bottom of the window SHALL show, on the Library page, the number of books ("310 books", or
"42 of 310 books" while narrowed, "No books" when empty), and on every page the progress of a running library or Drive
scan (both when both run). It SHALL show nothing else.

#### Scenario: Following a scan from another page

- **GIVEN** a Drive scan started on Sources, reading 400 of 2,400 books
- **WHEN** the user opens the Library
- **THEN** the status bar shows the Drive scan's progress next to the book count
