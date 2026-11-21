# Spec Delta

## ADDED Requirements

### Requirement: Large libraries

The Library page SHALL show the first 120 matching books and SHALL add 120 more whenever the end of the list comes near
the view, or when the user presses Show more; a new search, filter, grouping or sort SHALL start again from the first
120. The book count and section counts SHALL count every matching book.

#### Scenario: Scrolling a large library

- **GIVEN** 3,000 books
- **WHEN** the user opens the Library and scrolls down
- **THEN** 120 books are shown at first, "3000 books" is the count, and more appear before the end is reached
