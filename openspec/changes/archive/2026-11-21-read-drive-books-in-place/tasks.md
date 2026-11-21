# Tasks

## 1. Data

- [x] 1.1 `GoogleDriveFiles.DownloadRangeAsync` (Range request; a full answer is sliced) — test
- [x] 1.2 `DriveRangeStream`: seekable, chunked, cached — tests through scans of a large EPUB and a large PDF
- [x] 1.3 Scan reads up to 10 books at once in batches; `CatalogWriter` takes metadata read in advance — test
- [x] 1.4 `PdfInfoReader`: classic tables, xref streams, object streams, predictors, updates; fallback to PdfPig
