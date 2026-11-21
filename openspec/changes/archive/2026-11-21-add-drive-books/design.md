# Design

## Context

Slice 1 gives `IDriveAccount` (tokens) and the access level. The local scan is `LibraryScanner` →
`ScanReconciler.Plan` (pure, path-keyed) → `CatalogWriter.Apply`. Decisions marked **(my call)** were made without
asking.

## Decisions

### 1. Drive files in the catalog

A Drive file is a `BookFile` with `DriveFileId` set and `Path = gdrive://<id>/<display path>` (made and read by
`DrivePath`). The id keeps paths unique (Drive allows two files with one name); the display part follows renames and
moves, so the reconciler sees them as moves of the same content. Every local code path (scan, folder unlink,
organise) skips files with a `DriveFileId`; slice 3 adds Drive organise and trash.

### 2. Sources

`DriveSource { Id, DriveId, Name, Kind (Folder|File), AddedUtc }`, unique by `DriveId`. A folder's `Name` is its path
from My Drive ("Books/Fantasy"); a file's is its name. Files found under a folder get the display path
`<folder name>/<relative path>`, picked files just their name. Removing a source marks missing the present Drive files
it covered (by display prefix for folders, by id for files).

### 3. Scan

`DriveLibrary.ScanAsync` takes the library write gate like a local scan. It lists each folder recursively
(`files.list`, `'<id>' in parents and trashed=false`, fields id, name, mimeType, size, modifiedTime, sha256Checksum,
paged) and gets each picked file; only names `BookMetadataReader.IsSupported` accepts are kept, each Drive file once.
Any listing failure stops the scan before anything changes (my call: simpler and safe, unlike per-folder
"unavailable" handling). Known Drive files and the found ones go through `ScanReconciler.Plan`; files that must be
read (added, updated or split) are downloaded first into a temp folder, and `CatalogWriter` gets an opener that reads
those copies. A Drive file without a checksum is downloaded and hashed.

### 4. Drive calls

`GoogleDriveFiles` (Data): list children, get one file, download (`alt=media`) to a stream, list folders for the
browser. Each request carries a fresh token from `IDriveAccount.GetAccessTokenAsync`; listing calls get a 60-second
timeout, downloads only the caller's cancellation. `DriveLinkLostException` reaches the UI as "needs linking again".

### 5. Picker

`IDriveAccount.PickFilesAsync` runs the slice-1 sign-in with `trigger_onepick=true` (only with picked-files access),
replaces the stored token with the new one and returns the picked ids, which become File sources.

### 6. Open

`<data>/drive-cache/<id><ext>`; reused when its size matches the catalog's, otherwise downloaded to `.partial` and
moved in place. Show in Drive opens `https://drive.google.com/file/d/<id>/view` through `IWebBrowser`. Unlinking Drive
keeps sources and books (my call: they are still on Drive; linking again resumes).
