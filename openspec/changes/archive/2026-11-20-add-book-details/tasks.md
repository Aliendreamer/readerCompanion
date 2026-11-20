# Tasks

## 1. Data

- [x] 1.1 Move find-or-create of authors, genres and series into `CatalogNames`, used by `CatalogWriter` —
  existing scanner tests still pass
- [x] 1.2 `Book.EditedUtc` + migration; `CatalogWriter` keeps an edited book's details on changed content —
  Data test: edited book keeps title, authors and genres after its file's content changes; cover still updates
- [x] 1.3 `ILibraryBooks.GetAsync(id)` returning `BookDetails` with files — Data test: details and files, missing
  file flagged, unknown id gives null
- [x] 1.4 `ILibraryBooks.UpdateAsync(id, BookEdit)` — Data tests: fields saved and `EditedUtc` set; author matched
  ignoring case and spaces; blank series clears series and number; empty title rejected

## 2. Launcher

- [x] 2.1 `IFileLauncher` in UI and the desktop implementation registered in `Program.cs` — checked manually

## 3. UI

- [x] 3.1 Cards and table titles select a book; `BookPanel` view mode with details and files, Open / Show in
  folder for present files, failure message, close button and Escape — bUnit
- [x] 3.2 Edit mode: form, required title, Cancel, Save calling `UpdateAsync`; Home reloads the list keeping
  filters — bUnit
- [x] 3.3 Panel styles in both themes; narrow window overlays the books

## 4. Verification

- [x] 4.1 Quality gates (`dotnet build`, tests, `pnpm format:check`, `pnpm lint:md`); simplify and code review;
  manual checks queued for the user: Open/Show in folder on Linux, edit then rescan keeps edits
