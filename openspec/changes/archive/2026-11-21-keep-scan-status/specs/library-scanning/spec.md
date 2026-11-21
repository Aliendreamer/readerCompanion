# Spec Delta

## ADDED Requirements

### Requirement: Scan status survives leaving the page

A library scan or Drive scan SHALL keep running when the user leaves the Sources page, and when the page is opened
again it SHALL show the running scan's progress, or the last scan's summary or failure.

#### Scenario: Back during a scan

- **GIVEN** a library scan at "12 of 40 files"
- **WHEN** the user opens the Library page and then Sources again
- **THEN** Sources shows the scan's progress, and its summary when it finishes
