# Tasks

## 1. Data and preferences

- [ ] 1.1 Add `CoverFileName` to `BookListItem` and return it from `LibraryBooks.ListAsync` — Data test: a scanned
  EPUB with a cover lists its cover file name, a PDF lists none
- [ ] 1.2 Add `IAppPreferences` (Theme System/Light/Dark, LibraryView Grid/Table) in Core and a JSON-file
  implementation in Data, registered in `AddLibraryData` — Data tests: defaults when missing, round-trip after
  save, defaults when the file is corrupt

## 2. Theme and shell

- [ ] 2.1 Rewrite `app.css` on CSS variables with light, dark and system palettes (`color-scheme` included) and
  check the main text/background pairs reach 4.5:1 — verified by contrast check in the build notes
- [ ] 2.2 `MainLayout` with sidebar (app name, Library, Sources, active link) and root `data-theme` — bUnit: links
  present, root carries the stored theme
- [ ] 2.3 `theme.js` + `ISystemTheme` and a `ThemeToggle` showing the effective theme, flipping and saving through
  `IAppPreferences`, with "Use system theme" shown only while overridden — bUnit: on a light system the toggle
  sets `data-theme="dark"` and saves it; "Use system theme" restores `system` and hides itself

## 3. Library views

- [ ] 3.1 `ICoverUrls` in UI; `BookCover` showing the image or a placeholder with title, first author and stable
  hash colour — bUnit: cover URL used when present, placeholder text and same colour for the same title
- [ ] 3.2 `BookGrid` cards (cover, title, authors / "Unknown author", series #n, missing marker) — bUnit
- [ ] 3.3 `BookTable` rows with the same data and a small cover — bUnit
- [ ] 3.4 `ViewToggle` and Home composing header (count), toggle and the chosen view, defaulting to grid,
  remembering the choice; empty state unchanged without a toggle — bUnit: toggle switches and saves, empty
  library shows no toggle

## 4. Host and Sources

- [ ] 4.1 Desktop: `cover` scheme handler serving from the covers folder (rejecting separators and `..`) and the
  `ICoverUrls` implementation — test for the file-name guard; covers checked manually in the window
- [ ] 4.2 Restyle Sources with the new classes (cards, buttons, progress, summary) without changing its ids —
  existing Sources tests still pass

## 5. Verification

- [ ] 5.1 Run the quality gates from CLAUDE.md (`dotnet build`, `dotnet test`, `pnpm format:check`,
  `pnpm lint:md`) and a manual check on Linux: covers show, grid/table toggle remembered, light/dark toggle
  and "Use system theme" work

## Workflow follow-up

- Archive the change and update ROADMAP.md after the user's approval.
