# Design

## Context

`AppPaths.DefaultDatabasePath()` decides the data folder; `LibraryDataPaths` knows it inside Data. Startup in
`Program.Main` builds the Photino app and migrates the database with no error handling. Decisions marked **(my
call)** were made without asking, to keep the roadmap loop moving.

## Goals / Non-Goals

**Goals:** one overview page; no silent startup failures.

**Non-Goals:** moving the data folder from the app (my call: risky, and copying the folder by hand works), editing
every setting on one page (each stays on its own page; Settings links to them).

## Decisions

### 1. `ILibraryLocation`

`string DataFolder` in Core, implemented in Data from `LibraryDataPaths`, so the UI can show it without knowing paths.

### 2. `StartupErrorLog.Write(exception, preferredFolder, now)`

In Data (testable): writes the time, the exception's full text and the .NET/OS versions to `startup-error.log` in the
preferred folder, falling back to `Path.GetTempPath()` when that fails; returns the log path and a short message. The
desktop host wraps everything from building the app to `Run()` in a try/catch, writes the log and shows
`PhotinoWindow.ShowMessage` with the message; if even the dialog fails, it writes to the console.

### 3. Settings page

`/settings`, last in the sidebar. Read-only overview cards, each with a link to the page that changes it.
