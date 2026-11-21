# Tasks

## 1. Core and model

- [x] 1.1 `DrivePath`, `DriveSource`, `BookFile.DriveFileId`, `FoundFile.DriveFileId`, `IDriveLibrary`; migration — tests

## 2. Data

- [x] 2.1 Local scan, folder unlink and organise ignore Drive files — tests
- [x] 2.2 `CatalogWriter` file opener and Drive ids — tests
- [x] 2.3 `GoogleDriveFiles` (list, get, download, folders) — fake-handler tests
- [x] 2.4 `DriveLibrary` sources (add folder, add picked, remove) — tests
- [x] 2.5 `DriveLibrary.ScanAsync` — tests: add, join local copy, rename, gone, unreadable folder stops, only new downloaded
- [x] 2.6 `PickFilesAsync`; `DriveLibrary.OpenAsync` cache — tests

## 3. UI

- [x] 3.1 Drive card: folders with browser, picked files, scan with summary — bUnit
- [x] 3.2 Details panel: Drive file location, Open, Show in Drive — bUnit

## 4. Verification

- [x] 4.1 Quality gates; simplify and code review; manual check with a real Drive (user)
