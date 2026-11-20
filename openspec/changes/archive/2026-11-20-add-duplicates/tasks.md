# Tasks

## 1. Scanning

- [x] 1.1 `ScanReconciler` splits a changed file only when an identical copy of its old content remains — Core
  tests: merged book with EPUB + PDF updates in place; identical copies still split

## 2. Matching

- [x] 2.1 `DuplicateFinder` in Core: folded title/author key, ISBN key, union of groups, dismissed pairs skipped —
  Core tests from the spec scenarios

## 3. Data

- [x] 3.1 `DismissedDuplicates` table + migration; `IDuplicates.FindAsync` and `DismissAsync` — Data tests
- [x] 3.2 `IDuplicates.MergeAsync` — Data tests: files moved, genres combined, earliest date, cover taken when
  missing, others removed, dismissed pairs cleaned; then a rescan keeps the merge

## 4. UI

- [x] 4.1 Duplicates page with groups side by side, default pick, Merge with confirmation, Not duplicates, empty
  state; sidebar link — bUnit
- [x] 4.2 Styles in both themes

## 5. Verification

- [x] 5.1 Quality gates; simplify and code review; manual checks queued for the user
