# Spec Delta

## MODIFIED Requirements

### Requirement: Details panel

The Library page SHALL have a details pane on the right, always present: with no book chosen it says "Select a book to
see its details"; choosing a book in the grid or table SHALL show its cover, title, authors, series and number,
genres, language, ISBN and date added. Its close button or the Escape key SHALL clear the choice. A chosen book SHALL
stay shown when a search or filter hides it. Below about 1,000 px of page width the pane SHALL slide over the books
only while a book is chosen. The library's filters, search, grouping and scroll position SHALL stay as they were.

#### Scenario: Open and close

- **GIVEN** the library filtered to Fantasy
- **WHEN** the user picks "Mort", reads its pane and closes it
- **THEN** the pane showed "Mort", "Terry Pratchett" and "Discworld #4", then "Select a book to see its details",
  and the list is still filtered to Fantasy
