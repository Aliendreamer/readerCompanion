# Spec Delta

## ADDED Requirements

### Requirement: Organise progress

While a preview, an organise or an undo runs, the Organise page SHALL say what is being done: working out the moves,
moving files or moving them back with how many of how many and a progress bar, and removing emptied folders.

#### Scenario: Organising many files

- **GIVEN** a preview of 340 moves
- **WHEN** the user presses Move
- **THEN** the page shows "Moving files… 12 of 340" with a bar, counting up until the result is shown
