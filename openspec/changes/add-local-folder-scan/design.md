# Design

## Context

- After M0: `Core` is empty; `Data` has `LibraryDbContext` (no entities), `AddLibraryData(path)`,
  `MigrateLibraryDatabase()`; `UI` has `Routes`, `MainLayout`, `Home`; `Desktop` is a Photino host.
- Tests: xUnit v3 + bUnit, run by `dotnet test` (in the agent sandbox: `dotnet run --project <tests>`).
- Photino exposes a native folder dialog: `PhotinoWindow.ShowOpenFolderAsync(...)`.
- Requirements are in `specs/library-scanning` and `specs/ebook-metadata`; motivation in proposal.md.

## Goals / Non-Goals

**Goals:**

- Scan and rescan logic that is testable without a UI, a real disk layout, or the desktop host.
- Rescans of an unchanged few-thousand-book library finish in seconds.

**Non-Goals:**

- Watching folders for changes in the background (rescans are user-started).
- Editing book details, grouped views, search (M2); manual edits surviving rescans arrives with editing in M2.
- Formats beyond EPUB and PDF; cover display beyond what the plain list needs (covers are stored now, shown in M2).
- Cancelling a running scan.

## Decisions

1. **Layering: Core owns the model and abstractions, Data implements them.**
   - `Core`: entities (`Book`, `Author`, `Collection`, `BookFile`, `LibraryFolder`), the metadata readers, name
     normalisation, the pure rescan reconciliation, and service interfaces the UI uses (`ILibraryFolders`,
     `ILibraryScanner`, `ILibraryBooks`).
   - `Data`: EF Core mappings + migration, and the service implementations (file walking, hashing, persistence).
     `Data` references `Core`.
   - `UI` references only `Core`, so components are tested with fakes; `Desktop` wires `Data`.
   - Alternative: everything in `Data` — rejected; reconciliation and metadata parsing would need a database to
     test.

2. **Data model.**
   - `Book`: Id, Title, Language?, Isbn?, SeriesId? → `Collection`, SeriesIndex (decimal?), CoverFileName?.
   - `Author`: Id, Name, NormalizedName (unique). `BookAuthor`: BookId, AuthorId, Order.
   - `Genre`: Id, Name, NormalizedName (unique). `BookGenre`: BookId, GenreId.
   - `Collection`: Id, Name, NormalizedName, Kind (`Series` now; `User` reserved for M2); unique (Kind, NormalizedName).
   - `BookFile`: Id, BookId, Path (unique), Format (`Epub`/`Pdf`), SizeBytes, LastWriteUtc, ContentHash (SHA-256 hex),
     Status (`Present`/`Missing`), Unreadable (bool), LastSeenUtc.
   - `LibraryFolder`: Id, Path (unique), AddedUtc.
   - One migration, `AddLibrary`.

3. **Identity = SHA-256 of file content; skip re-hashing via (path, size, last-write time).** Only new paths or
   changed size/time are hashed. Alternative: identity by path — rejected, loses books on move/rename (spec).
   Alternative: partial/fast hash — rejected, collisions between similar PDFs would merge different books.

4. **Rescan reconciliation is a pure function in Core**:
   `ScanReconciler.Plan(known, found, unavailableRoots) → ScanPlan`, where `known` and `found` are
   `IReadOnlyList<KnownFile>` / `IReadOnlyList<FoundFile>` and `unavailableRoots` is `IReadOnlyCollection<string>`,
   producing: unchanged, moved (same hash, new path), changed (same path, new hash), new, missing, reappeared.
   Rules beyond the spec's scenarios:
   - A changed file whose book still has other present copies with the old content becomes a new book (the old
     content still exists elsewhere); otherwise the book is updated in place.
   - A found file whose hash matches an existing book joins that book (copy, or a reappearing file).
   - Known files under an unavailable root are left untouched.

5. **EPUB parsed with the BCL (ZipArchive + XDocument), not a library.** Read `META-INF/container.xml` → OPF;
   take `dc:title`, `dc:creator` (document order), `dc:language`, `dc:subject` (genres), ISBN from `dc:identifier`
   (opf:scheme ISBN,
   `urn:isbn:` prefix, or a value that is 10/13 digits after removing hyphens), series from EPUB 3
   `belongs-to-collection`/`group-position` or `calibre:series`/`calibre:series_index`, cover from the manifest
   item with `properties="cover-image"` or the EPUB 2 `<meta name="cover">` id. Only the OPF and cover entries are
   read. Alternative: VersOne.Epub — rejected, it loads the whole book and we need four elements.

6. **PDF via PdfPig 0.1.16 (Apache-2.0)**: `PdfDocument.Open` → `Information.Title` / `Information.Author`; author
   split on `;` and `&`. Any exception (corrupt, encrypted) → unreadable.

7. **Names** (authors, genres, series): `NameNormalizer.Normalize` trims, collapses internal whitespace, upper-cases
   invariantly; the result
   is the `NormalizedName` key. The first-seen spelling is kept as the display `Name`.

8. **Covers** are written to `<data folder>/covers/<contenthash>.<ext>` (folder next to `library.db`), referenced
   by `Book.CoverFileName`. Not stored in SQLite, so backups of `library.db` stay small (covers can be re-extracted).

9. **Scanning runs off the UI thread, one at a time.** `ILibraryScanner.ScanAsync(IProgress<ScanProgress>,
   CancellationToken)` returns a `ScanSummary`. A second start while running is refused. Files are enumerated with
   `EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, MatchCasing = CaseInsensitive,
   AttributesToSkip = ReparsePoint }` (symlinked folders are skipped, which also avoids loops). Changes are saved
   in batches of 100 files.

10. **Folder rules.** Paths are compared after `Path.GetFullPath` and trailing-separator trimming; comparison is
    case-insensitive on Windows, case-sensitive elsewhere. A folder that equals or is inside an existing folder is
    refused. (A new folder that *contains* existing ones is allowed; the contained folders are then redundant and
    are removed from the list, with their files kept.) Removing a folder marks the files under it missing.

11. **UI**: a **Sources** page (`/sources`) made of one section per source type, so Google Drive (M7) and Goodreads
    (M6) are added later as new sections with their own link button, not a new page. This change ships the "Local
    folders" section: linked folders, a **Link folder** button (native picker via an `IFolderPicker` interface in
    UI, implemented in Desktop with Photino; a cancelled picker returns null and changes nothing), Unlink, Scan
    with live "N files processed", and the summary. Persistence stays folder-specific (`LibraryFolder`); Drive
    sources need different data (account, remote folder id) and get their own entity in M7. Alternative: a generic
    `LibrarySource { Kind, Location }` now — rejected, it would guess at M7's needs. Home lists
    books: title, authors (or "Unknown author"), "Series #n", and a "missing" marker. Navigation links in
    `MainLayout`.

## Risks / Trade-offs

- [First scan of a large PDF library hashes every byte; could take minutes] → progress is shown; later scans skip
  unchanged files.
- [Two genuinely different editions with identical bytes merge] → by definition they are the same file.
- [Same book as both EPUB and PDF shows twice] → accepted for M1; merging belongs with editing in M2.
- [Network/NAS folders with coarse timestamps] → size + time check still works; worst case re-hash.
- [Symlinked subfolders are not followed] → documented; add the target as its own library folder.

## Migration Plan

New EF migration `AddLibrary` is applied automatically at startup (existing M0 behaviour). Empty database before
this change, so nothing to convert. Rollback: previous build + delete `library.db`.
