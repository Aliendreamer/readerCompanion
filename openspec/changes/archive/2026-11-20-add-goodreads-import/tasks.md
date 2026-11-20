# Tasks

## 1. Core

- [x] 1.1 `GoodreadsCsv` parsing — unit tests: quotes and newlines, `="…"` ISBNs, dates, shelves, ratings, missing
  columns refused
- [x] 1.2 Public match keys shared by duplicates and import — existing duplicate tests still pass

## 2. Data

- [x] 2.1 Migration: `GoodreadsId`, `GoodreadsStatus`, `GoodreadsRating`, `ReadingSession.FromGoodreads`
- [x] 2.2 `IGoodreadsImport.ImportAsync` — Data tests: owned match by id/ISBN/title-author, wanted books, reads and
  ratings, second import changes nothing, user's status kept
- [x] 2.3 Scan attaches a new file to a matching wanted book — Data test

## 3. UI

- [x] 3.1 `IFilePicker` + Photino implementation; Goodreads card on Sources with summary and errors — bUnit

## 4. Verification

- [x] 4.1 Quality gates; simplify and code review; manual check with a real export
