# Proposal

## Why

Series come from the files, but people also group books their own way: "To read next", "Book club", "Favourites".
M2 promises user collections beyond series. They are the last M2 slice.

## What Changes

- In a book's details panel, the user can **add the book to a collection**, picking an existing one or typing a new
  name, and **remove it** from one. A collection with no books left disappears.
- The Library's **collection filter and grouping** include the user's collections as well as series. Inside a
  collection, books follow series order where they have one, then title.
- **Scans never change** a book's collections. **Merging duplicates combines** their collections.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `library-browsing`: collections in filters and grouping include the user's own collections.
- `book-details`: the panel lists the book's collections and lets the user add and remove them.
- `duplicates`: merging also combines the books' collections.

## Impact

- Core: `Book` gains user collection links; `BookListItem` gains `Collections`; `ILibraryBooks` gains add/remove
  collection.
- Data: a `BookCollections` link table and migration; list and details return collection names; merge combines them.
- UI: collection chips and an add box in the panel; browse treats series and user collections alike.
- No new packages.
