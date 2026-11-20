# Tasks

## 1. Core and Data

- [x] 1.1 `ITrash`; `FreedesktopTrash` — Data tests: file moved to `files/`, `.trashinfo` written with encoded path
  and date, name collisions get a new name, a missing file gives false
- [x] 1.2 `BookFileInfo.Id`; `ILibraryBooks.RemoveFileAsync` — Data tests: removes an extra file, refuses the last one

## 2. Desktop and UI

- [x] 2.1 `RecycleBinTrash` on Windows, freedesktop elsewhere, registered in `Program.cs` — checked manually
- [x] 2.2 Panel: Move to trash with confirmation, hidden for a single file, failure shown — bUnit

## 3. Verification

- [x] 3.1 Quality gates; simplify and code review; manual checks queued (trash on Linux, restore from trash and rescan)
