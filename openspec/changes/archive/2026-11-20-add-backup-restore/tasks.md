# Tasks

## 1. Data

- [x] 1.1 Preferences: backup folder, back up on exit, keep N — Data tests: defaults and round trip
- [x] 1.2 `IBackups.BackupAsync` and `List` — Data tests: consistent copy, naming, collisions, other files ignored
- [x] 1.3 `RestoreAsync` — Data tests: content replaced, safety backup written, older schema upgraded, non-backup
  refused with the library unchanged
- [x] 1.4 `Prune` — Data test: keeps the newest N, never touches other files

## 2. UI and host

- [x] 2.1 Backup page: folder, back up now, list, restore with confirmation, on-exit settings; sidebar link — bUnit
- [x] 2.2 Desktop: back up and prune on window close when enabled — checked manually
- [x] 2.3 Styles

## 3. Verification

- [x] 3.1 Quality gates; simplify and code review; manual checks queued
