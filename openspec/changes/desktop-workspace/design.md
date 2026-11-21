# Design: a desktop workspace (Calibre-style layout)

## Intent

The app works, but it feels like a website: a dashboard sidebar, rounded cards with shadows, large headings, the
filters as a row of dropdowns, book details sliding over the page. The user wants it to feel like a Windows desktop
program such as Calibre: organised, dense, with a toolbar on top and the window split into panes.

**Goal:** the same app, laid out the way desktop library programs are (Calibre, Book Collector): an action toolbar,
a filter tree on the left, books in the middle, details on the right, a status bar at the bottom.

**Non-goals:** no new features. Every button, list and action already exists somewhere in the app; this only moves
and restyles them. No new commands (no "convert", no bulk edit), no right-click menus, no new shortcuts, no
resizable splitters, no classic File/Edit menu bar (it would only repeat the toolbar).

**Anti-goals:** losing a function in the move; a toolbar so crowded it is harder to use than the sidebar; a layout
that breaks at the smallest window size (720 × 480) or at 150% text size.

## Approaches considered

- **A. Calibre-style workspace** (chosen by the user): toolbar replaces the sidebar, three panes on the Library,
  filter tree, status bar. Biggest change, closest to the desktop feel asked for.
- **B. Menu bar + toolbar on the current layout:** keeps the dashboard sidebar, so the "website with a sidebar"
  shape stays.
- **C. Restyle only:** denser and flatter, same layout; unlikely to change the feeling.

## The window, top to bottom

```text
┌ [logo] readerCompanion ───────────────────────────────── ⛶ – ▢ ✕ ┐   title strip (own frame only)
│ [Library][Reading][Duplicates][Organise][Sources][Backup]      … [☾][⚙][?] │   toolbar
├───────────────┬────────────────────────────────┬────────────────────┤
│ filter tree   │ search · group · sort · ▦☰      │ details of the     │   Library page
│               │ cover grid / table             │ selected book      │
├───────────────┴────────────────────────────────┴────────────────────┤
│ 2,412 books · 120 shown · Drive scan: reading 400 of 2,400          │   status bar
└──────────────────────────────────────────────────────────────────────┘
```

### Title strip (when the app draws its own frame)

Logo and "readerCompanion" on the left (the drag handle, double-click maximises), the window buttons on the right,
as now. With the system title bar the strip is absent, as now.

### Toolbar (replaces the left sidebar)

- One row of buttons, icon above a short label, Calibre-style, ~56 px tall at 100%.
- **Pages** (the sidebar's links, same order): Library, Reading, Duplicates, Organise, Sources, Backup. The current
  page's button shows as pressed.
- **No Scan button:** scanning stays on the Sources page exactly as it is (the user: "don't change how we scan").
- **Right end:** theme switch (an icon button flipping light/dark, the sidebar's toggle), Settings, Help.
- "Use system theme" (the sidebar link under the toggle) moves to the Settings page, next to the text size.
- Book actions (open, show in folder, move to trash, edit) stay in the details pane: they act on one *file*, and a
  book can have several, so a toolbar button would have to guess which.
- When the window is too narrow for every label, labels hide and icons remain (tooltips keep the names).

### Library page: three panes

- **Left, filter tree** (~260 px): replaces the second row of dropdown filters. Sections, each collapsible, in the
  order of today's dropdowns: Genres, Authors, Collections, Formats, Status, Owned / wanted; then "Missing file
  only". Each value is a line with a tick when picked; clicking toggles it. Same meaning as today: any picked value
  within a section matches, all sections must match. Each section shows how many values it has; long sections
  (authors) have the same find box and paging (120) as the dropdowns had. "Clear filters" sits at the top of the
  tree when anything is picked.
- **Middle, books:** the search box, Group, Sort and the Grid/Table switch in one compact row above the books
  (today's first row); the grid or table below, paged as now. Only this pane scrolls.
- **Right, details** (~340 px, always there on the Library page): the existing book panel, no longer sliding over
  the page. With no book selected it says "Select a book to see its details". Closing it (×) clears the selection.
- Other pages use the whole width under the toolbar, without the tree or details pane.

### Status bar (bottom, ~26 px)

- On the Library page: "2,412 books", or "37 of 2,412 books" while narrowed (today's count line moves here).
- While a library or Drive scan runs, its progress (from the existing scan status), on every page.
- Nothing else (no new information).

### Density and flatness (all pages)

- Cards lose their shadows and most of their rounding (4 px), gaps and paddings shrink by about a third, page
  headings stay hidden (as now), section headings get smaller. Buttons keep their size for touch-free mouse use.
- Colours, fonts, themes and the text-size zoom stay as they are.

## Decided after review (approved by the user, with 2 changed)

1. **Narrow windows.** The three panes need about 1,000 px of page width (tree 260 + books ≥ 400 + details 340).
   Below that the details pane goes back to sliding over the books when a book is picked (today's panel), and below
   about 760 px the tree hides behind a "Filters" button at the left of the search row. A laptop at 1366 px and 115%
   text (about 1,190 px of page) keeps all three panes.
2. **Scanning is unchanged** (rejected: a toolbar Scan button). Scans start on the Sources page as today; the status
   bar only shows a running scan's progress, so it can be followed from any page. The outcome stays on Sources.
3. **Empty or loading library.** The Library page shows today's empty-state or "Loading…" card across the full width,
   without tree or details pane; the status bar then says "No books" (or nothing while loading). The Goodreads
   reminder stays at the top of the books pane.
4. **Selection.** A selected book stays in the details pane when a search or filter hides it, until another book is
   picked or × is pressed. A book removed from the library shows the panel's existing "no longer in the library" note.
5. **Status bar content.** The Library page publishes its count through a small shared service; other pages publish
   nothing. While both a library and a Drive scan run, both show side by side ("Scan 12 of 40 · Drive scan: reading
   400 of 2,400").
6. **Tree sections.** Formats, Status, Owned / wanted and Genres start open; Authors and Collections start closed
   (they are long). Open/closed is remembered until the app closes, not saved. "Missing file only" is a single tick
   line at the end, not a section. Find box and 120-paging as in today's dropdowns, for sections of 8+ values.
7. **Pressed state.** Settings and Help show pressed when their page is open, like the page buttons.

## Build order (each step is usable on its own)

1. Toolbar replaces the sidebar; title strip carries the logo; status bar with count and running scans' progress;
   theme and "use system theme" moved.
2. Library in three panes with the permanent details pane.
3. Filter tree replaces the dropdown filters.
4. Density pass over all pages.

## Left to the builder

Exact icons (reuse the sidebar's), pixel sizes within the ranges above, the narrow-window breakpoint, test updates
(the sidebar, filter dropdown and sliding-panel tests change with the layout; behaviour tests keep their meaning).
