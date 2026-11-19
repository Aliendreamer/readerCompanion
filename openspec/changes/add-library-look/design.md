# Design

## Context

The UI is a Razor Class Library hosted by Photino (WebView2 on Windows, WebKitGTK on Linux). M1 left a single
`app.css` with no theme, a top link bar, and a plain list on Home. Covers are saved by the scan as files in
`<data folder>/covers/` and referenced by `Book.CoverFileName`, but nothing shows them yet. The UI project
references Core only; anything host-specific goes behind an interface implemented in Desktop.

## Goals / Non-Goals

**Goals:**

- One stylesheet built on CSS custom properties, so every later page gets both themes for free.
- Covers visible in the window without pushing image bytes through Blazor's render messages.
- Grid and table built as components that M2's grouped views can reuse.

**Non-Goals:**

- Sorting, searching, filtering, grouping, book details pages (M2).
- Virtualized lists (M2, when grouping changes the list shape anyway).
- A settings page (M8); the theme switch lives in the sidebar.
- Icon fonts or CSS frameworks.

## Decisions

### 1. Theme via CSS variables on the app root, set by Blazor — no JavaScript

`MainLayout` renders `<div class="app" data-theme="system|light|dark">`. The stylesheet defines the light palette
on `.app`, the dark palette under `.app[data-theme=dark]` and under
`@media (prefers-color-scheme: dark) { .app[data-theme=system] }`, and sets `color-scheme` so native controls and
scrollbars match. The root fills the viewport, so `body` needs no theming.

*Alternatives:* setting `data-theme` on `<html>` needs JS interop and is awkward to test with bUnit; a
class-per-theme stylesheet duplicates rules.

### 2. Covers served by a custom URL scheme registered by the Desktop host

Desktop registers a `cover` scheme on the Photino window (`RegisterCustomSchemeHandler`) that streams
`<covers folder>/<file name>` with its image content type. It rejects names containing path separators or `..`,
so only files in the covers folder can be read. The UI asks an `ICoverUrls` interface (UI project, like
`IFolderPicker`) for a book's image URL; Desktop's implementation returns `cover://covers/<file name>`.
`<img loading="lazy">` keeps large libraries cheap, and `onerror` falls back to the placeholder.

*Alternatives:* data URIs through Blazor — every render would carry the image bytes (the same base64 flood seen
in M1's log), and they'd sit in memory; plugging the covers folder into Photino.Blazor's own `app` file provider —
not a public extension point in 4.0.13.

### 3. Placeholder colour from a stable hash of the title

A small FNV-1a hash of the normalized title picks a hue; saturation and lightness are fixed per theme so white
text stays readable. `string.GetHashCode` is randomized per process, so it can't be used — the same book must
keep its colour across restarts.

### 4. Preferences in a JSON file next to the database

Core gets `IAppPreferences` (`Theme`: System/Light/Dark, `LibraryView`: Grid/Table, load and save). Data
implements it with `preferences.json` in the data folder. A missing or unreadable file gives the defaults
(System, Grid); a write failure leaves the choice in effect for this session only.

*Alternatives:* a SQLite table — a migration for two values the user can lose without harm; browser
`localStorage` — not reliably persisted for Photino's `app://` origin on WebKitGTK.

### 5. Book list carries the cover

`BookListItem` gains `CoverFileName` (nullable). No other query changes.

### 6. Components

- `Layout/MainLayout` + `Layout/Sidebar` (navigation and `ThemeSwitch`).
- `Library/BookGrid`, `Library/BookTable`, `Library/BookCover` (image or placeholder), `Library/ViewToggle`.
- `Pages/Home` composes them; `Pages/Sources` keeps its markup and element ids, restyled through classes.

Existing bUnit selectors (`#link-folder`, `#scan`, `.book-title`, `.book-missing`, …) stay so current tests keep
working.

## Risks / Trade-offs

- [WebKitGTK reports the GTK theme, not Windows' dark mode, under WSLg] → the System/Light/Dark switch; the user
  picks Dark once and it is remembered.
- [Custom scheme images may be blocked from the `app://` page on one engine] → verify on Linux first in the
  manual check; fallback is to serve covers under the `app` scheme by wrapping Photino.Blazor's file provider, or
  small data-URI thumbnails as a last resort.
- [Thousands of `<img>` elements] → lazy loading now; virtualization in M2.
- [Hand-picked palette could fail contrast] → check the main text/background pairs against 4.5:1 while building.

## Migration Plan

No data migration. `preferences.json` is created on first change. Rollback is reverting the commit.
