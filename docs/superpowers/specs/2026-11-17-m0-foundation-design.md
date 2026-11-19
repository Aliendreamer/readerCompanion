# M0 — Foundation: design

Roadmap: [ROADMAP.md](../../../ROADMAP.md), milestone M0.

## Intent

Stand up an empty, runnable readerCompanion with every piece of plumbing later milestones rely on,
so M1 starts with feature work, not setup.

**Goal:** `dotnet run` opens a Blazor window (in WSL via WSLg, and on Windows), a SQLite database
is created and migrated on startup, and CI runs the quality gates green.

**Non-goals:** any book, author, or scanning features; styling beyond a placeholder page;
packaging/installers.

## Approaches (decided)

| Option | Trade-off | Outcome |
| ------ | --------- | ------- |
| **Photino.Blazor** | Cross-platform, light; smaller community | **Chosen** — you want Windows + Linux, and it runs inside WSL |
| WPF/WinForms BlazorWebView | Mature on Windows; Windows only, can't run from WSL | Rejected — Windows only |
| MAUI Blazor Hybrid | Windows/macOS/mobile; no Linux desktop, heavy | Rejected — no Linux |

## Design

- **Projects** (`net10.0`): `Core` (classlib), `Data` (classlib, EF Core SQLite), `UI` (Razor
  Class Library), `Desktop` (Photino host, references UI + Data). Tests: `Data.Tests` and
  `UI.Tests` now; `Core.Tests` arrives with the first Core logic in M1.
- **Repo-wide config:** `global.json` (SDK 10.0.x), `Directory.Build.props` (nullable, implicit
  usings, `TreatWarningsAsErrors`), `Directory.Packages.props` (central package management,
  **exact version pins**), `.editorconfig`, `.gitignore` (standard .NET, plus the sandbox
  placeholder dotfiles).
- **Database:** `LibraryDbContext` in `Data`, stored at
  `<LocalApplicationData>/ReaderCompanion/library.db` (`~/.local/share/...` on Linux,
  `%LOCALAPPDATA%\...` on Windows). An `InitialCreate` migration with no tables yet;
  `Database.Migrate()` runs at startup. The path is injectable so tests use a temp file.
- **UI:** a placeholder Home page and main layout in the RCL; the Desktop host only wires DI and
  opens the window.
- **Tests (TDD):** Data — startup migration creates the database file and the migrations history
  at an injected path. UI — bUnit renders the Home page.
- **CI:** GitHub Actions on `ubuntu-latest`: restore, `dotnet build -warnaserror`,
  `dotnet format --verify-no-changes`, `dotnet test`.

## Left to the builder

Exact package versions (pinned, latest stable at build time), folder names inside projects,
placeholder page wording.
