# Design

## Context

`Collection` already has `Kind` (Series or User) and a unique index on (Kind, NormalizedName); a book points at its
one series through `Book.SeriesId`. Nothing links books to user collections yet. `LibraryBrowser.Collections(book)`
returns the series only. Decisions marked **(my call)** were made without asking, to keep the roadmap loop moving.

## Goals / Non-Goals

**Goals:** add and remove a book's user collections; browse them like series; keep them through scans and merges.

**Non-Goals:** renaming or deleting a collection as a whole, collection pages, ordering books by hand inside a
collection (later, if wanted).

## Decisions

### 1. `BookCollection` link table

`BookCollection(BookId, CollectionId)` with a composite key, cascading from both sides; `Book.Collections` lists the
links. Only user-kind collections are linked this way; series stay on `Book.SeriesId`. A user collection and a series
may share a name; they are different rows (the unique index includes `Kind`).

### 2. `ILibraryBooks.AddToCollectionAsync(id, name)` / `RemoveFromCollectionAsync(id, name)`

Add cleans the name with `NameNormalizer`, finds the user collection by normalized name with one query (or
creates it), and links it (no-op if already linked). Remove unlinks it and deletes the collection
row when no book links to it any more (my call: no empty collections lying around, since nothing can show them).
Both return false for an unknown book; Add throws `ArgumentException` for a blank name. Neither marks the book
edited: collections are never touched by scans anyway.

### 3. List and details carry `Collections`

`BookListItem.Collections` (user collection names, ordered) joins `Genres` and `Formats` as an init property, set by
the shared projection. `LibraryBrowser.Collections(book)` becomes series plus user collections, so filters, options
and grouping pick them up with no other change. A series and a user collection with the same name read as one
collection there (my call: to the reader they are the same shelf); names compare ignoring case, as elsewhere in
browsing. Inside a user collection's section, the existing series sort orders
books by series, number, then title (my call: sensible for mixed collections, and identical to series sections).

### 4. Panel UI (my call)

Under the details, a "Collections" row: the book's collections as chips with × (reusing `.chip`), then an input with a
`<datalist>` of existing user collections and an "Add" button; Enter adds too. After a change the panel reloads its
details and asks the page to reload the list (the existing `OnSaved` path).

### 5. Merge

`Duplicates.MergeAsync` adds the others' collection links to the kept book like genres.

## Risks / Trade-offs

- `<datalist>` support varies in the Linux web view → it only offers suggestions; typing the name always works.
- A typo creates a new collection → removing the book from it deletes it again.
