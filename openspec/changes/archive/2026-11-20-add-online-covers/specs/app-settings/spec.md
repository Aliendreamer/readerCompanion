# Spec Delta

## ADDED Requirements

### Requirement: Covers from Open Library

The Settings page SHALL offer a switch, off by default and remembered, to get missing covers from Open Library, and,
while it is on, a Fetch missing covers action. Fetching SHALL look up only books that have an ISBN and no cover, at
most one request per second, save each cover found in the library's covers folder and show it from then on, report
progress, and stop when asked. A book Open Library has no cover for SHALL be left unchanged. The app SHALL not contact
Open Library at any other time.

#### Scenario: Fetch

- **GIVEN** the switch on, and two books with an ISBN and no cover, one of which Open Library has a cover for
- **WHEN** the user presses Fetch missing covers
- **THEN** that book shows its new cover, the other keeps its placeholder, and the page says 1 cover found of 2

#### Scenario: Off

- **GIVEN** the switch off
- **WHEN** the Settings page is shown
- **THEN** there is no Fetch action and nothing is requested from Open Library
