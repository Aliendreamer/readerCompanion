# Design

## Context

Books come from file content: identical copies share a book, everything else is its own book. `ScanReconciler`
splits a changed file off into a new book whenever *any* other present file of the book remains, which assumed a
book's files are all copies. Merging breaks that assumption, so the rule must look at content, not file count.
Book details live in `Book` and its author/genre links; `CatalogNames` shares names. The Library page reloads its
list on navigation.

Decisions marked **(my call)** were made without asking, to keep the roadmap loop moving.

## Goals / Non-Goals

**Goals:**

- Suggest likely duplicates with few false matches, merge them safely, and remember "not duplicates".
- Merged books stay merged across rescans.

**Non-Goals:**

- Undo of a merge (M5 brings previewed, undoable file operations; until then merging asks for confirmation).
- Fuzzy title matching (edit distance), subtitle stripping, or cover comparison (my call: start strict; loosen
  later if real libraries show misses).
- Deleting duplicate files from disk (M5).

## Decisions

### 1. Matching in Core, as a pure function

`DuplicateFinder.Find(candidates, dismissed)` returns groups of book ids. Keys: the ISBN's digits (and X) when there
are at least 10; and `title|first author`, each folded (lower case, accents removed, letters and digits only, single
spaces), only when the book has an author (a bare title such as "Untitled" says too little). Books sharing a key are
joined with union-find, except that two groups are never joined if that would put a dismissed pair together, even
through a third book. Groups are ordered by their first book's title. Unit tests cover the spec scenarios.

### 2. Split rule looks at content

`ScanReconciler` splits a changed file only when another file of the book that is present after the scan has the
file's *old* content hash, or a copy with that hash joined the book in this scan. Otherwise it updates the book in
place. This is what the existing doc comment already promised.

### 3. `IDuplicates` in Data

- `FindAsync()` loads candidates (id, title, first author, ISBN) and the dismissed pairs, runs the finder, and
  returns `DuplicateGroup`s of `DuplicateBook` (list item + ISBN, edited flag, files) for display.
- `MergeAsync(keepId, otherIds)` in one transaction: re-point the others' `BookFile`s to the keeper, add their
  genres to the keeper, set `DateAddedUtc` to the earliest, take the first other cover if the keeper has none, mark
  the keeper edited (the user chose its details), carry the others' dismissals over to the keeper, and delete the
  other books (their links and own dismissals cascade).
- `DismissAsync(ids)` stores every pair of the group's still-existing books (smaller id first) in
  `DismissedDuplicates`.

### 4. Default pick (my call)

The edited book first; otherwise the most filled-in one (count of authors, series, genres, ISBN, language, cover),
then the one with an EPUB, then the lowest id.

### 5. Page

`/duplicates`, linked in the sidebar after Library. One card per group, its books in columns with the details
from the spec and a "Keep these details" radio per book. "Merge…" turns into "Merge into ‹title›? Yes / No";
"Not duplicates" dismisses at once. Groups never share books, so a merged or dismissed group just leaves the list.
One action runs at a time (buttons disabled meanwhile); a failure is shown on its group. Empty state when there are
none.

## Risks / Trade-offs

- Strict keys miss duplicates whose titles differ ("Mort" vs "Mort: A Discworld Novel") → accepted for now;
  ISBN catches many of them.
- Merging cannot be undone yet → explicit confirmation naming the book kept.
- Dismissing a 3-book group stores 3 pairs → if a 4th book matches, it still shows with whichever books it links to.
