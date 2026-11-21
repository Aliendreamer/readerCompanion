# Desktop Workspace Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or
> superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Lay the app out like a Windows library program (toolbar, panes, filter tree, status bar), with no new
features.

**Architecture:** Blazor RCL (`src/ReaderCompanion.UI`) hosted by Photino. `MainLayout` becomes a grid of title
strip / toolbar / page / status bar. The Library page (`Pages/Home.razor`) becomes a three-pane grid. Small shared
singletons carry state across components (`LibraryStatus` for the count, existing `ScanStatus` for scans,
`FilterTreeState` for open sections).

**Tech Stack:** .NET 10, Blazor, bUnit 2, xUnit v3; CSS in `wwwroot/app.css`.

**Spec:** `openspec/changes/desktop-workspace/design.md` and this change's `specs/`.

## Global Constraints

- No new features; scanning stays on Sources unchanged (no toolbar Scan button).
- Smallest window 720 × 480 and 150% text size must not break the layout.
- Panes: tree ~260 px, details ~340 px; details slide over below ~1,000 px page width; tree behind "Filters" below
  ~760 px.
- Page size for long lists: `Pager.PageSize` (120). Find box from 8 values.
- Tree: Authors and Collections start closed, others open; remembered until the app closes (not saved).
- Run tests with `dotnet run --no-build --project tests/<project>` after `dotnet build -m:1`.

## Review Focus

- Selecting a book, then filtering it out: the pane must keep showing it (Task 3 test).
- A book removed while shown: the pane's existing "no longer in the library" note (Task 3 keeps it).
- Empty library: full-width empty state, no tree/pane, status "No books" (Task 3 test).
- Two scans at once: the status bar shows both (Task 2 test).
- System title bar mode: no strip, so no logo; the toolbar must still work (Task 1 test).

---

### Task 1: Toolbar replaces the sidebar; logo in the title strip; theme switch moves

**Files:**

- Create: `src/ReaderCompanion.UI/Layout/Toolbar.razor`
- Modify: `src/ReaderCompanion.UI/Layout/MainLayout.razor`, `Layout/WindowFrame.razor`, `Layout/ThemeToggle.razor`,
  `Pages/SettingsPage.razor`, `wwwroot/app.css`
- Delete: `src/ReaderCompanion.UI/Layout/Sidebar.razor`
- Test: `tests/ReaderCompanion.UI.Tests/ShellTests.cs`, `WindowFrameTests.cs`

**Interfaces:**

- Produces: `.toolbar` with `NavLink`s `.toolbar-button` (hrefs `""`, `reading`, `duplicates`, `organise`, `sources`,
  `backup`, then `settings`, `help`), each an icon (the sidebar's SVGs) above a `.toolbar-label`; `#theme-toggle`
  (role `switch`, `aria-checked`) in the toolbar; `#use-system-theme` on the Settings page (only while a theme is
  chosen); `.window-bar .brand` (logo + name) whose press moves the window (`WindowFrame.PressOn`).

- [ ] **Step 1: Write the failing tests**
  - `ShellTests.Toolbar_LinksToEachPage_AndShowsTheCurrentOneAsPressed`: navigate to `sources`; assert
    `.toolbar .toolbar-button` texts == Library, Reading, Duplicates, Organise, Sources, Backup, Settings, Help
    (in that order), `.toolbar .toolbar-button.active` text == `"Sources"`, and no `.sidebar`.
  - Existing theme tests in `ShellTests` keep their assertions, but `#use-system-theme` is looked up after rendering
    `SettingsPage`.
  - `WindowFrameTests.TitleStrip_ShowsTheLogo_AndMovesTheWindow`: own frame; `.window-bar .brand` contains
    "readerCompanion"; `MouseDown(Button 0, Detail 1)` on it → calls == `["BeginMove"]`. Replace the old
    `SidebarBrand_MovesTheWindow`.
- [ ] **Step 2: Run** the UI tests (build with `-m:1`, then `dotnet run --no-build --project
  tests/ReaderCompanion.UI.Tests`) — expect the new tests to fail.
- [ ] **Step 3: Implement** `Toolbar.razor`; `MainLayout` grid rows `auto` (strip) `auto` (toolbar) `1fr` (page)
  `auto` (status bar, Task 2) with one column; brand moved into `WindowFrame`'s strip (left of `.window-drag`);
  `ThemeToggle` reduced to the switch (icon button); a "Theme" card on Settings with `#use-system-theme`. Labels hide
  below ~900 px page width (icons stay, `title` keeps the name).
- [ ] **Step 4: Run** the UI tests — all pass.
- [ ] **Step 5: Commit** `feat: toolbar instead of the sidebar, logo in the title strip`.

### Task 2: Status bar with the book count and running scans

**Files:**

- Create: `src/ReaderCompanion.UI/LibraryStatus.cs`, `Layout/StatusBar.razor`
- Modify: `UIServiceCollectionExtensions.cs` (register `LibraryStatus` singleton), `Layout/MainLayout.razor`,
  `Pages/Home.razor` (publish the count, remove `.book-count`), `Library/DriveText.cs` (move `ProgressText` here from
  `GoogleDriveSource.razor` as `public static string ScanProgress(ScanProgress p)`), `Library/GoogleDriveSource.razor`
- Test: `tests/ReaderCompanion.UI.Tests/StatusBarTests.cs`; update `BrowseTests`, `LargeLibraryTests`
  (`.book-count` → `.status-count`); `Fakes.AddAppFakes` registers `LibraryStatus`.

**Interfaces:**

- Produces: `public sealed class LibraryStatus` with `string? Count { get; }`, `event Action? Changed` and
  `void Set(string? count)`;
  `.status-bar` with `.status-count` and one `.status-scan` per running scan (library: `"Scan {p} of {n} files"`;
  Drive: `"Drive scan: " + DriveText.ScanProgress(p)`).
- Consumes: `ScanStatus.Library`, `ScanStatus.Drive` (existing `TrackedScan`).

- [ ] **Step 1: Write the failing tests** (`StatusBarTests`)
  - `Count_ComesFromTheLibraryPage`: 3 books → `.status-count` == `"3 books"`; filter to 2 → `"2 of 3 books"`; no
    books → `"No books"`.
  - `RunningScans_ShowOnEveryPage_BothWhenBothRun`: start a held library scan (FakeScanner, report 12 of 40) and a held
    Drive scan (FakeDriveLibrary `ScanHeld`, report reading 3 of 20) through `ScanStatus`; render `Routes` at
    `settings`; assert `.status-scan` texts == "Scan 12 of 40 files" and
    "Drive scan: Reading new books from Drive… 3 of 20".
- [ ] **Step 2: Run** — fail.
- [ ] **Step 3: Implement**; `Home` calls `LibraryStatus.Set(Count())` after each refresh and `Set(null)` on dispose.
- [ ] **Step 4: Run** — pass (update the moved `.book-count` assertions).
- [ ] **Step 5: Commit** `feat: status bar with the book count and running scans`.

### Task 3: Library in three panes with a permanent details pane

**Files:**

- Modify: `src/ReaderCompanion.UI/Pages/Home.razor`, `Library/BookPanel.razor` (empty state), `wwwroot/app.css`
- Test: `tests/ReaderCompanion.UI.Tests/BookPanelTests.cs`, `BrowseTests.cs`

**Interfaces:**

- Produces: `.workspace` grid with `.workspace-tree` (holds the filter row until Task 4), `.workspace-books`
  (Goodreads reminder, BrowseBar first row, books; the only scrolling area), `.workspace-details` (BookPanel, or
  `.details-empty` with "Select a book to see its details"). Empty/loading library: no `.workspace`, full-width card
  as today.

- [ ] **Step 1: Write the failing tests**
  - `BookPanelTests.DetailsPane_IsAlwaysThere_AndCloseClearsTheChoice`: render Home; `.details-empty` text ==
    "Select a book to see its details"; open "Mort" → `.book-panel .panel-title` == "Mort"; click `#panel-close` →
    `.details-empty` again.
  - `BookPanelTests.AChosenBook_StaysShown_WhenAFilterHidesIt`: open "Dune", filter genre Fantasy → titles exclude
    "Dune", `.panel-title` still "Dune".
  - `BrowseTests.EmptyLibrary_HasNoPanes`: no books → no `.workspace`, `.empty-state` present.
- [ ] **Step 2: Run** — fail.
- [ ] **Step 3: Implement**; CSS: `.workspace { grid-template-columns: 260px minmax(400px, 1fr) 340px }`; under
  `@media (max-width: 1000px)` details become an overlay shown only with a chosen book (today's sliding panel rules);
  only `.workspace-books` scrolls.
- [ ] **Step 4: Run** — pass (adjust existing panel tests that assumed the panel was absent before choosing).
- [ ] **Step 5: Commit** `feat: library in three panes with a details pane`.

### Task 4: Filter tree replaces the dropdown filters

**Files:**

- Create: `src/ReaderCompanion.UI/Library/FilterTree.razor`, `Library/FilterSection.razor` (generic `TValue`),
  `Library/FilterTreeState.cs`
- Modify: `Library/BrowseBar.razor` (keep search, group, sort, view; remove the filters row), `Pages/Home.razor`
  (tree in `.workspace-tree`, "Clear filters" moves into the tree, `#filters-toggle` for narrow windows),
  `UIServiceCollectionExtensions.cs` (register `FilterTreeState` singleton), `wwwroot/app.css`
- Delete: `Library/FilterPicker.razor` (its find and paging logic moves to `FilterSection`)
- Test: `tests/ReaderCompanion.UI.Tests/FilterTreeTests.cs`; `FilterSteps.cs` drives the tree; replace
  `FilterPickerTests.cs`; `Fakes.AddAppFakes` registers `FilterTreeState`.

**Interfaces:**

- Produces: `FilterSection<TValue>` parameters `Id`, `Label`, `Options`, `Text`, `Picked`, `PickedChanged`
  (as `FilterPicker` had); renders a header button `#{Id}` (`aria-expanded`, label and `(count)`) and, when open,
  `#{Id}-menu` with `.filter-option` checkboxes, `.filter-search` from 8 options, and `MoreMarker` paging.
  `FilterTreeState { bool IsOpen(string id); void Toggle(string id); }` defaults open for all ids except
  `filter-author` and `filter-collection`. `#browse-missing` and `#clear-filters` live in the tree.
- Consumes: `Pager`, `MoreMarker` (existing).

- [ ] **Step 1: Write the failing tests** (`FilterTreeTests`)
  - `Sections_InOrder_LongOnesStartClosed`: header texts start with Genres, Authors, Collections, Formats, Status,
    Owned / wanted; `#filter-author` and `#filter-collection` have `aria-expanded="false"`, the rest `"true"`.
  - `TickingNarrows_AndClearFiltersAppears`: tick Science Fiction → titles == ["Dune"]; `#clear-filters` shown; click
    it → all titles.
  - `OpenStateIsRemembered_AcrossPages`: open Authors, navigate to `sources` and back → still open.
  - `NarrowWindow_FiltersButtonShowsTheTree`: click `#filters-toggle` → `.workspace` has class `tree-open`.
  - Port the dropdown tests' meaning (search narrows a long list, paging from 120) to `FilterSteps` on the tree.
- [ ] **Step 2: Run** — fail.
- [ ] **Step 3: Implement**; `FilterSteps.Open` expands the section when closed; drop the Escape/backdrop tests (no
  menus remain).
- [ ] **Step 4: Run** — pass.
- [ ] **Step 5: Commit** `feat: filter tree instead of filter dropdowns`.

### Task 5: Density pass

**Files:**

- Modify: `src/ReaderCompanion.UI/wwwroot/app.css`

- [ ] **Step 1:** Cards: `border-radius: 4px`, no `box-shadow`; paddings and gaps about a third smaller; section
  headings one step smaller. Keep colours, fonts, themes, zoom, and button sizes.
- [ ] **Step 2: Run** all UI tests (no behaviour change) and check the app by eye at 100% and 150% text, 720 × 480 and
  maximised (manual check by the user).
- [ ] **Step 3: Commit** `style: denser, flatter panels on every page`.

### Task 6: Close the change

- [ ] Sync the spec deltas into `openspec/specs/`, archive the change, run all gates (build, all three test projects,
  `dotnet format whitespace`, `pnpm lint:md`), commit `docs: desktop workspace specs`.
