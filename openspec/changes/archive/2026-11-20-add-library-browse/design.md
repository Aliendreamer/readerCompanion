# Design

## Context

`Home.razor` loads `ILibraryBooks.ListAsync()` once and hands the whole list to `BookGrid` or `BookTable`. The
list item carries authors, series and position, cover and missing state, but not genres, formats or any date.
Genres and series already exist in the database (`BookGenre`, `Book.Series`); there is no "date added". Books
have one series at most; the user's own collections arrive in a later M2 slice. Preferences are a JSON-backed
record (`AppPreferences`) that already remembers theme and grid/table view.

## Goals / Non-Goals

**Goals:**

- Filter, search, group and sort the library instantly for a personal library (thousands of books).
- Keep the browse rules in plain C# that can be unit-tested without rendering.
- Reuse `BookGrid` and `BookTable` unchanged inside each section.

**Non-Goals:**

- Editing genres or other details, duplicate finding, user collections (later M2 slices).
- Remembering filters or search across launches.
- Paging or virtualised lists.

## Decisions

### 1. Browse in memory, on the page

The page loads all books once (as now) and applies filters, search, grouping and sort in memory. Re-querying
SQLite on every click was the alternative: it only pays off for tens of thousands of books, and it makes each
click wait on a query. Thousands of items filter in well under a frame.

### 2. A pure `LibraryBrowser` in UI, a `BrowseQuery` record as its input

`BrowseQuery` holds search text, picked genres, authors, collections and formats, missing-only, grouping and
sort. `LibraryBrowser.Browse(books, query)` returns `BrowseResult`: total count, shown count and a list of
`BookSection(string? Name, IReadOnlyList<BookListItem> Books)`; with grouping None there is one section with no
name. It also lists the filter values present in the library (`Options(books)`), sorted, so filter pickers offer
only real values. Being pure, it gets plain xUnit tests (in the UI test project) for every rule in the spec;
bUnit tests only cover wiring.

Rules, as in the spec:

- Any-within (a book matches a filter if it has any picked value), all-across (every active filter and the
  search must match).
- Search uses `NameNormalizer`-style folding (case and accents removed) on title and authors, and matches by
  "contains".
- Grouping fans a book out to each of its authors/genres; the empty bucket ("Unknown author", "No collection",
  "No genre") comes last; sections are ordered by name, ignoring case.
- Collection sections are always in series position order (title breaks ties): grouping by collection sorts by
  series first, and the Sort control is disabled meanwhile. The chosen sort applies inside the other groupings
  and to the ungrouped list.
- Sort keys: title; first author then title; series name then position, books without a series last; date added
  newest first. Comparisons ignore case.

### 3. List item carries genres, formats and date added

`BookListItem` gains `IReadOnlyList<string> Genres`, `IReadOnlyList<BookFormat> Formats` (distinct, of all its
files) and `DateTime DateAddedUtc` as `init` properties with empty defaults rather than constructor parameters,
so existing call sites are untouched and tests set only what they need. The query projects them in the same
`Select`.

### 4. `Book.DateAddedUtc` with a backfilling migration

A non-null `DateAddedUtc` column. `CatalogWriter` sets it to the scan's `now` when it creates a book; nothing
else writes it, so rescans and moves keep it. The migration adds the column and fills existing rows with
`MIN(BookFiles.LastWriteUtc)` for the book, falling back to the migration time for a book without files. The
oldest file's last-modified time is closer to "when I got this book" than the time of the first scan, which for
every existing book would be the same afternoon.

### 5. Preferences gain `LibraryGroup` and `LibrarySort`

Two enums with defaults `None` and `Title`, added to `AppPreferences` as optional record parameters, so older
`preferences.json` files without them load with the defaults (the existing tolerant loader already covers this;
a Data test confirms it).

### 6. Toolbar and sections

A `BrowseBar` component above the books: search box (updates on input), Group and Sort selects, the grid/table toggle,
then a filter row of `FilterPicker`s for genre, author, collection and format (a dropdown of the library's values not
yet picked; picked values show as removable chips) and a "Missing file only" checkbox. A `BookSections` component
renders each section as a `<details open>` element whose `<summary>` is the heading "Name (count)", holding the
existing `BookGrid` or `BookTable`. Native `<details>` gives collapse, keyboard support and screen-reader state
without JavaScript. With grouping None, the list renders without a heading. When nothing matches, the page shows "No
books match" with a "Clear filters" button instead of an empty grid.

Collapsed state lives in the browser's `<details>` element; the sections are keyed on the grouping, so a new
grouping (or a restart) opens them all. Group and sort are read from the preferences only; the page itself holds
just the search and filters, so "Clear filters" resets them without touching grouping or sort. `FilterPicker` is
generic, so the format filter works on `BookFormat` values with an upper-case label.

## Risks / Trade-offs

- A long author list in a dropdown is unwieldy → pickers are native `<select>`s, which jump to an entry as you
  type its first letters; a type-to-filter picker or a dedicated author page can follow if that is not enough.
  (A `<datalist>` would filter, but its support in the Linux web view is uneven.)
- Fanning out multi-author books repeats cards → by design (spec); counts per section reflect that, while the
  page count counts each book once.
- Very large libraries (tens of thousands of books) may feel slow → out of scope; moving `LibraryBrowser`'s
  rules into the query later does not change the UI.
