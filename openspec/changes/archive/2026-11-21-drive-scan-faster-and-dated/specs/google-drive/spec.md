# Spec Delta

## ADDED Requirements

### Requirement: Drive scan dates

The Drive card SHALL show when Drive was last scanned successfully, or that it has not been scanned yet, and SHALL
show for each Drive folder and picked book the date it was added. A Drive scan SHALL look through up to 10 folders at
once.

#### Scenario: After a scan

- **GIVEN** the Drive folder "Books" added on 21 Nov 2026
- **WHEN** the user scans Drive
- **THEN** the card lists "Books" with "added 21 Nov 2026" and says "Last scan:" with today's date and time
