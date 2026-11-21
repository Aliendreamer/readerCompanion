# Tasks

## 1. Genres

- [x] 1.1 `GenreMap.Map` onto the BISAC list by words and codes — Core tests from a real library
- [x] 1.2 New scans store mapped genres — Data test
- [x] 1.3 One-time mapping of existing books (edited books kept), unused genres removed — Data test

## 2. Filter tree

- [x] 2.1 Every section starts closed — bUnit
- [x] 2.2 Owned / wanted filter removed; status counts, want-to-read with/without files — LibraryBrowser + bUnit

## 3. Goodreads series

- [x] 3.1 Two-series titles take the first series — Core test
- [x] 3.2 Re-import corrects wanted books' series; empty series collections removed — Data test

## 4. Release-tagged series

- [x] 4.1 `SeriesName.Clean` for the dotted "(#N of M)" form — Core tests from the real names
- [x] 4.2 Scans clean it; existing books fixed at start (`CatalogCleanup`) — Data tests

## 5. After a first look

- [x] 5.1 FB2 genre codes looked up whole; "prose_classic" gives none; Classics only from whole subjects — Core tests
- [x] 5.2 Every filter value shows its book count — LibraryBrowser + bUnit
