# Proposal

## Why

The Library page shows every book in one flat list sorted by title. M2 promises browsing the whole library by
author, series and genre. As a library grows, a flat list makes it hard to find a book or see what you have by
one author or in one genre. This is the first slice of M2. Editing, duplicates and user collections come later.

## What Changes

- **Filters** above the books: **genre**, **author** and **collection** (a series for now; the user's own
  collections join later), each accepting several values, plus **format** (EPUB/PDF) and **missing file only**.
  Several values in one filter match **any** of them; different filters must **all** match.
- **Search** by title or author, typed in a box, narrowing the books as you type.
- **Group by** None, Author, Collection or Genre: the shown books split into sections with a heading and count,
  each section collapsible. A book with two authors or genres appears in each of their sections. Collection
  sections list books in series order.
- **Sort** by title, author, series order or date added.
- The count reads "N of M books" while filters or search narrow the list, and "M books" otherwise.
- **Group and sort are remembered** like the grid/table choice; filters and search start empty each launch.
- **Date added**: each book records when it entered the library; books already in the library get the date of
  their oldest file.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `library-browsing`: adds filtering, search, grouping and sorting of the Library page, and the "N of M books"
  count.

## Impact

- Core: `BookListItem` gains genres, formats and date added; `Book` gains `DateAddedUtc`; preferences gain the
  library grouping and sort.
- Data: a migration adds `Books.DateAddedUtc`, filled for existing books from their files; the scan stamps new
  books; the list query returns the new fields.
- UI: a browse toolbar (search, filters, group, sort), grouped sections around the existing grid and table, and
  the browse logic as plain, testable code.
- No new packages.
