# Spec Delta

## ADDED Requirements

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
