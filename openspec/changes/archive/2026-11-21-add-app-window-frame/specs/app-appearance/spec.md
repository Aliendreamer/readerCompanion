# Spec Delta

## ADDED Requirements

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
