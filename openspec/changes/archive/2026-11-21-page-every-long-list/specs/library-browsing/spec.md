# Spec Delta

## ADDED Requirements

### Requirement: Every long list is paged

Every list that can grow with the library (filter options, duplicates, organise moves and failures, unreadable files,
Goodreads rows to review and skipped, the reading page's lists, Drive folders and sources) SHALL show its first 120
items with how many there are, and SHALL show 120 more near its end or on Show more; a new list SHALL start again
from the first 120.

#### Scenario: A big organise preview

- **GIVEN** a preview of 2,000 moves
- **WHEN** it is shown
- **THEN** the first 120 are listed with "Showing 120 of 2000", all 2,000 stay selected, and more appear on scrolling
