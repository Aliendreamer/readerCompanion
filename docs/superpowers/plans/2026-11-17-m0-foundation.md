# M0 — Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or
> superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** An empty, runnable readerCompanion: a Photino Blazor window, a SQLite database created and migrated on
startup, and CI gates green.

**Architecture:** Four `net10.0` projects — `Core` (empty for now), `Data` (EF Core SQLite + startup migration), `UI`
(Razor Class Library with the routes and pages), `Desktop` (thin Photino host wiring DI and opening the window). Tests
cover `Data` (xUnit v3) and `UI` (bUnit).

**Tech Stack:** .NET 10 SDK 10.0.203, Photino.Blazor, EF Core 10 SQLite, xUnit v3, bUnit, GitHub Actions.

**Spec:** [docs/superpowers/specs/2026-11-17-m0-foundation-design.md](../specs/2026-11-17-m0-foundation-design.md)

## Global Constraints

- Target framework `net10.0` for every project; `global.json` pins SDK `10.0.203`, `rollForward: latestPatch`.
- Central package management (`Directory.Packages.props`), **exact pins only** (no `^`, `~`, ranges, `*`):
  `Microsoft.EntityFrameworkCore.Sqlite` 10.0.15, `Microsoft.EntityFrameworkCore.Design` 10.0.15,
  `Microsoft.AspNetCore.Components.Web` 10.0.15, `Microsoft.AspNetCore.Components.WebView` 10.0.15,
  `Photino.Blazor` 4.0.13, `xunit.v3` 4.0.3, `xunit.runner.visualstudio` 4.0.1,
  `Microsoft.NET.Test.Sdk` 18.12.0, `bunit` 2.15.2. (EF Core 11 needs net11 — do not use it.)
- `Directory.Build.props`: `Nullable` enable, `ImplicitUsings` enable, `TreatWarningsAsErrors` true.
- Root namespace / project prefix `ReaderCompanion.`; solution file `ReaderCompanion.slnx`.
- Database file: `<LocalApplicationData>/ReaderCompanion/library.db`; path always injectable.
- Repo workflow (CLAUDE.md overrides the per-task commits a plan would normally have): work on
  branch `feat/m0-foundation`; **no commits until the user has approved and manually verified**,
  then one commit. Stage explicit paths only — never `git add -A` (the sandbox shows placeholder
  dotfiles, and `x.txt` is the user's).

## Review Focus

- **Parent folder missing on first launch** (fresh machine, no `ReaderCompanion` folder yet) — startup creates it. Test
  in Task 1.
- **User profile path with spaces / non-ASCII** (e.g. `C:\Users\José Ü\…`) — database is created there normally. Test in
  Task 1.
- **Second and later launches** — migration is a no-op, data untouched, no error. Test in Task 1.
- **`LocalApplicationData` resolves to empty** (stripped-down Linux env without `HOME`) — fail fast with a clear
  message, never write `ReaderCompanion/library.db` relative to the working directory. Test in Task 1.
- **App launched from a different working directory** (desktop shortcut) — static assets and DB still resolve. Manual
  check in Task 3.

---

### Task 1: Repo scaffolding + Data layer with startup migration

**Files:**

- Create: `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, `.gitignore`,
  `ReaderCompanion.slnx`
- Create: `src/ReaderCompanion.Core/ReaderCompanion.Core.csproj` (classlib, no code yet)
- Create: `src/ReaderCompanion.Data/ReaderCompanion.Data.csproj`
- Create: `src/ReaderCompanion.Data/LibraryDbContext.cs`
- Create: `src/ReaderCompanion.Data/AppPaths.cs`
- Create: `src/ReaderCompanion.Data/DataServiceCollectionExtensions.cs`
- Create: `src/ReaderCompanion.Data/DesignTimeLibraryDbContextFactory.cs`
- Create: `src/ReaderCompanion.Data/Migrations/*` (generated)
- Test: `tests/ReaderCompanion.Data.Tests/ReaderCompanion.Data.Tests.csproj`,
  `tests/ReaderCompanion.Data.Tests/StartupMigrationTests.cs`, `tests/ReaderCompanion.Data.Tests/AppPathsTests.cs`

**Interfaces:**

- Produces:
  - `public sealed class LibraryDbContext(DbContextOptions<LibraryDbContext> options) : DbContext(options)` — no
    entities yet.
  - `public static class AppPaths { string DatabasePath(string localAppDataRoot); string DefaultDatabasePath(); }` —
    `DatabasePath` returns `Path.Combine(root, "ReaderCompanion", "library.db")`, throws `InvalidOperationException`
    when `root` is null/whitespace; `DefaultDatabasePath()` calls it with
    `Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)`.
  - `public static IServiceCollection AddLibraryData(this IServiceCollection services, string databasePath)` — registers
    `LibraryDbContext` with `UseSqlite($"Data Source={databasePath}")` (build via `SqliteConnectionStringBuilder`, so
    spaces/unicode are safe).
  - `public static void MigrateLibraryDatabase(this IServiceProvider services)` — creates the parent directory, then
    `Database.Migrate()` in a scope.

- [ ] **Step 1: Branch and scaffold.** `git switch -c feat/m0-foundation`. Write the root config files per Global
  Constraints. `.gitignore`: standard .NET (`bin/`, `obj/`, `TestResults/`, `*.user`, `.vs/`, `*.db`, `*.db-shm`,
  `*.db-wal`) plus root sandbox placeholders `/.bashrc /.bash_profile /.profile /.zshrc /.zprofile /.gitconfig
  /.ripgreprc /.idea` (not `.gitmodules`, `.mcp.json`, `.vscode` — real files). Create the slnx and the Core, Data,
  Data.Tests projects; Data.Tests references Data and uses `xunit.v3`, `xunit.runner.visualstudio`,
  `Microsoft.NET.Test.Sdk`.
  Run: `dotnet build ReaderCompanion.slnx` → Expected: build succeeded, 0 warnings.

- [ ] **Step 2: Write the failing tests**

```csharp
// AppPathsTests.cs
[Fact] public void DatabasePath_IsUnderReaderCompanionFolder() =>
    Assert.Equal(Path.Combine("/root", "ReaderCompanion", "library.db"), AppPaths.DatabasePath("/root"));

[Theory, InlineData(""), InlineData("   ")]
public void DatabasePath_EmptyRoot_Throws(string root) =>
    Assert.Throws<InvalidOperationException>(() => AppPaths.DatabasePath(root));

// StartupMigrationTests.cs — each test uses its own temp dir, deleted in Dispose
// (call SqliteConnection.ClearAllPools() before deleting).
[Fact] public void Migrate_CreatesMissingParentFolderAndDatabase()
// path = <temp>/nested/deeper/library.db (folders don't exist)
// Assert.True(File.Exists(path)); applied migrations == ["<timestamp>_InitialCreate"] (assert single, EndsWith "_InitialCreate")

[Fact] public void Migrate_PathWithSpacesAndUnicode_Works()
// path = <temp>/José Ü folder/library.db → File.Exists

[Fact] public void Migrate_RunTwice_IsNoOp()
// migrate, build a fresh provider, migrate again → no exception, applied migrations count == 1
```

- [ ] **Step 3: Run tests to verify they fail.** Run: `dotnet test tests/ReaderCompanion.Data.Tests` → Expected: compile
  errors (`AppPaths`, `AddLibraryData`, `MigrateLibraryDatabase` not defined).

- [ ] **Step 4: Implement** `LibraryDbContext`, `AppPaths`, `DataServiceCollectionExtensions` per Interfaces, plus
  `DesignTimeLibraryDbContextFactory : IDesignTimeDbContextFactory<LibraryDbContext>` (SQLite on a throwaway `design.db`
  path, only used by tooling). Then generate the migration:
  Run: `dotnet ef migrations add InitialCreate --project src/ReaderCompanion.Data --output-dir Migrations` → Expected:
  `Migrations/*_InitialCreate.cs` and `LibraryDbContextModelSnapshot.cs` created. Exclude `Migrations/` from `dotnet
  format` style churn only if format flags generated code (prefer `[GeneratedCode]`-style `.editorconfig` section
  `[**/Migrations/*.cs] generated_code = true`).

- [ ] **Step 5: Run tests to verify they pass.** Run: `dotnet test tests/ReaderCompanion.Data.Tests` → Expected: 6
  passed, 0 failed.

### Task 2: UI Razor Class Library with routes and Home page

**Files:**

- Create: `src/ReaderCompanion.UI/ReaderCompanion.UI.csproj` (`Microsoft.NET.Sdk.Razor`, references
  `Microsoft.AspNetCore.Components.Web`)
- Create: `src/ReaderCompanion.UI/_Imports.razor`, `Routes.razor`, `Layout/MainLayout.razor`, `Pages/Home.razor`,
  `wwwroot/app.css`
- Test: `tests/ReaderCompanion.UI.Tests/ReaderCompanion.UI.Tests.csproj` (xunit.v3 + bunit, refs UI),
  `tests/ReaderCompanion.UI.Tests/HomeTests.cs`

**Interfaces:**

- Produces: `ReaderCompanion.UI.Routes` — root component; `<Router AppAssembly="typeof(Routes).Assembly">` with
  `DefaultLayout="typeof(MainLayout)"` and a "Page not found" `NotFound` fallback. `Home` at `@page "/"`. Stylesheet
  served as `_content/ReaderCompanion.UI/app.css`.

- [ ] **Step 1: Write the failing tests**

```csharp
public class HomeTests : BunitContext
{
    [Fact] public void Home_ShowsAppTitleAndEmptyLibraryMessage()
    {
        var cut = Render<Home>();
        Assert.Equal("readerCompanion", cut.Find("h1").TextContent);
        Assert.Contains("Your library is empty.", cut.Markup);
    }

    [Fact] public void Routes_AtRoot_RendersHomeInsideLayout()
    {
        var cut = Render<Routes>();
        Assert.Equal("readerCompanion", cut.Find("main h1").TextContent);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail.** Run: `dotnet test tests/ReaderCompanion.UI.Tests` → Expected: compile
  errors (`Home`, `Routes` not defined).

- [ ] **Step 3: Implement** the RCL files. `MainLayout` wraps `@Body` in `<main>`. Home: `<h1>readerCompanion</h1>` and
  `<p>Your library is empty.</p>`.

- [ ] **Step 4: Run tests to verify they pass.** Run: `dotnet test tests/ReaderCompanion.UI.Tests` → Expected: 2 passed.

### Task 3: Photino desktop host

**Files:**

- Create: `src/ReaderCompanion.Desktop/ReaderCompanion.Desktop.csproj` (`OutputType` Exe, refs UI + Data, packages
  `Photino.Blazor` and `Microsoft.AspNetCore.Components.WebView` — the explicit WebView pin lifts Photino's transitive
  9.0.1 to 10.0.15)
- Create: `src/ReaderCompanion.Desktop/Program.cs`, `src/ReaderCompanion.Desktop/wwwroot/index.html`

**Interfaces:**

- Consumes: `AppPaths.DefaultDatabasePath()`, `AddLibraryData(string)`, `MigrateLibraryDatabase()` (Task 1); `Routes`
  and `_content/ReaderCompanion.UI/app.css` (Task 2).

- [ ] **Step 1: Implement `Program.Main`** marked `[STAThread]` (required on Windows):
  `PhotinoBlazorAppBuilder.CreateDefault(args)`, `builder.Services.AddLibraryData(AppPaths.DefaultDatabasePath())`,
  `builder.RootComponents.Add<Routes>("#app")`, build, `app.Services.MigrateLibraryDatabase()`, window title
  `readerCompanion`, size 1200×800, `app.Run()`. `index.html` loads `_content/ReaderCompanion.UI/app.css`, has `<div
  id="app">Loading…</div>` and `<script src="_framework/blazor.webview.js"></script>`; mark `wwwroot/**`
  `CopyToOutputDirectory=PreserveNewest`.

- [ ] **Step 2: Build.** Run: `dotnet build ReaderCompanion.slnx` → Expected: 0 warnings, 0 errors.

- [ ] **Step 3: Run from another working directory (Review Focus).** Run: `cd /tmp && dotnet run --project
  ~/projects/readerCompanion/src/ReaderCompanion.Desktop` → Expected: a window titled "readerCompanion" shows the h1 and
  "Your library is empty."; `ls ~/.local/share/ReaderCompanion/library.db` exists. Close the window; process exits 0.

### Task 4: CI and full local gate run

**Files:**

- Create: `.github/workflows/ci.yml`

- [ ] **Step 1: Write the workflow.** Trigger on `push` and `pull_request`; `ubuntu-latest`; `actions/checkout@v4`;
  `actions/setup-dotnet@v4` with `global-json-file: global.json`; steps: `dotnet restore`, `dotnet build --no-restore
  -warnaserror`, `dotnet format --verify-no-changes --no-restore`, `dotnet test --no-build`.

- [ ] **Step 2: Run the same gates locally.** Run each command above from the repo root → Expected: all exit 0; tests: 8
  passed. (CI itself goes green after the user-approved push.)

- [ ] **Step 3: Hand over per developer-flow.** `/simplify`, then code review, re-run gates, report to the user, wait
  for manual verification + approval, then one commit on `feat/m0-foundation` with the explicit paths created above.
