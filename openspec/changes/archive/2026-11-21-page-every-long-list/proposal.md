# Proposal

## Why

Only the Library page showed a large list a page at a time. With thousands of books, other lists also grow long: an
author filter's options, the duplicates, an organise preview, unreadable files in a scan summary, Goodreads rows to
review, the reading page's lists and the Drive folder browser.

## What Changes

- One `Pager` (page size 120: whole rows of 4, 5, 6, 8, 10 or 12 covers) and one `MoreMarker` ("Showing 120 of N",
  Show more, and more automatically near the view) are used by every list that can grow long. A new list (a new
  preview, a new scan, a new search) starts again from the first page.
- The reasons for the Drive scan's limits are written next to them.

## Capabilities

### New Capabilities

### Modified Capabilities

- `library-browsing`: every long list is shown a page at a time.

## Impact

UI `Pager`, `MoreMarker`; Library sections, filter options, Duplicates, Organise, scan summary, Goodreads, Reading,
Drive card.
