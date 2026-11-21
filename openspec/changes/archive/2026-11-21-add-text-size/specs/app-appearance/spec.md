# Spec Delta

## ADDED Requirements

### Requirement: Text size

The Settings page SHALL offer a text size of 100%, 115%, 130% or 150%, defaulting to 115%. The whole window
(menu, pages, covers) SHALL be shown at that size, applied at start and as soon as the user picks another size.

#### Scenario: Larger text

- **GIVEN** the text size is 115%
- **WHEN** the user picks 130% in Settings
- **THEN** the window is shown at 130% at once, and still is after a restart
