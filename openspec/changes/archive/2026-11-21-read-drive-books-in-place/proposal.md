# Proposal

## Why

A Drive scan downloaded every new or changed book in full, one at a time, just to read its title, authors, series
and cover. A first scan of a large Drive library therefore downloaded the whole library. Google Drive has no API for
a book's own metadata, but it serves byte ranges of a file, and the metadata sits in small parts of it.

## What Changes

- A Drive scan reads books through a seekable view of the Drive file that fetches only the byte ranges the
  EPUB and PDF readers touch (in 64 KB pieces): for an EPUB the ZIP directory at the end, the package file and the
  cover; for a PDF the trailer, cross-reference data and the document information, through a small reader of its
  own (PdfPig touches every page). A PDF it cannot follow is downloaded whole and read as before. On a real library
  of 73 PDFs: 67 read in place with the same result as PdfPig, 6 fell back, 44 MB read instead of 638 MB.
- Up to 10 books are read at once, in batches, and applied to the catalog in order.
- Opening a book from Drive still downloads the whole file (the reader app needs it); files Drive gives no checksum
  for are still downloaded once to compute one.

## Capabilities

### New Capabilities

### Modified Capabilities

- `google-drive`: Drive scans read only the parts of a book they need, several at once.

## Impact

Data: `GoogleDriveFiles.DownloadRangeAsync`, `DriveRangeStream`, `DriveLibrary` scan reading; `CatalogWriter`
accepts metadata read in advance.
