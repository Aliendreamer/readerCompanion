# Proposal

## Why

With about 3,000 books from Google Drive, the Library page built every book card at once: opening it, and every
keystroke in the search, rebuilt thousands of cards.

## What Changes

- The Library page builds the first 120 books (across sections) and adds 120 more whenever the end of the list comes
  within about a screen of the view; a "Show more" button does the same. A new search, filter, grouping or sort starts
  again from the first 120. The book count and section counts stay complete.

## Capabilities

### New Capabilities

### Modified Capabilities

- `library-browsing`: large libraries are shown a page at a time.

## Impact

UI: `IInView` (IntersectionObserver through `inview.js`), `BookSections` limit and marker, `Home` page size.
