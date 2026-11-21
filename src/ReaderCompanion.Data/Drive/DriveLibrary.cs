using Microsoft.EntityFrameworkCore;
using ReaderCompanion.Core.Drive;
using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Metadata;
using ReaderCompanion.Core.Scanning;

namespace ReaderCompanion.Data;

/// <summary>The library's Drive side. A Drive scan works like a local one: the same reconciler and catalog writer,
/// with Drive's checksums for content and downloads only for files that must be read.</summary>
internal sealed class DriveLibrary(
    IDbContextFactory<LibraryDbContext> contexts,
    LibraryDataPaths paths,
    LibraryWriteGate gate,
    IDriveAccount account,
    GoogleDriveFiles files) : IDriveLibrary
{
    public async Task<IReadOnlyList<DriveSource>> ListSourcesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        return await db.DriveSources.AsNoTracking().OrderBy(s => s.Kind).ThenBy(s => s.Name).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DriveFolder>> ListFoldersAsync(string? parentId, CancellationToken cancellationToken = default) =>
        [.. (await files.ListChildrenAsync(parentId ?? "root", foldersOnly: true, cancellationToken)).Select(f => new DriveFolder(f.Id, f.Name))];

    public async Task<LinkFolderResult> AddFolderAsync(string folderId, string path, CancellationToken cancellationToken = default)
    {
        var name = path.Trim('/');
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var folders = await db.DriveSources.Where(s => s.Kind == DriveSourceKind.Folder).ToListAsync(cancellationToken);
        if (folders.Any(f => f.DriveId == folderId))
        {
            return new LinkFolderResult(false, "This Drive folder is already added.");
        }

        if (folders.FirstOrDefault(f => DrivePath.IsSameOrUnder(name, f.Name)) is { } covering)
        {
            return new LinkFolderResult(false, $"Already covered by the Drive folder {covering.Name}.");
        }

        var replaced = folders.Where(f => DrivePath.IsSameOrUnder(f.Name, name)).ToList();
        db.DriveSources.RemoveRange(replaced);
        db.DriveSources.Add(new DriveSource { DriveId = folderId, Name = name, Kind = DriveSourceKind.Folder, AddedUtc = DateTime.UtcNow });
        await db.SaveChangesAsync(cancellationToken);
        return new LinkFolderResult(true) { Replaced = [.. replaced.Select(f => f.Name)] };
    }

    public async Task<int> AddPickedFilesAsync(CancellationToken cancellationToken = default)
    {
        var picked = await account.PickFilesAsync(cancellationToken);
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var known = (await db.DriveSources.Select(s => s.DriveId).ToListAsync(cancellationToken)).ToHashSet();
        var added = 0;
        foreach (var id in picked.Where(known.Add))
        {
            var name = (await files.GetAsync(id, cancellationToken))?.Name ?? id;
            db.DriveSources.Add(new DriveSource { DriveId = id, Name = name, Kind = DriveSourceKind.File, AddedUtc = DateTime.UtcNow });
            added++;
        }

        await db.SaveChangesAsync(cancellationToken);
        return added;
    }

    public async Task RemoveSourceAsync(int sourceId, CancellationToken cancellationToken = default)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        if (await db.DriveSources.FindAsync([sourceId], cancellationToken) is not { } source)
        {
            return;
        }

        var others = await db.DriveSources.Where(s => s.Id != source.Id).ToListAsync(cancellationToken);
        var present = await db.BookFiles.Where(f => f.DriveFileId != null && f.Status == FileStatus.Present).ToListAsync(cancellationToken);
        foreach (var file in present.Where(f => Covers(source, f) && !others.Any(o => Covers(o, f))))
        {
            file.Status = FileStatus.Missing;
        }

        db.DriveSources.Remove(source);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ScanSummary> ScanAsync(IProgress<ScanProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        if (!gate.TryEnter(LibraryWork.Scan))
        {
            throw new InvalidOperationException(gate.BusyMessage);
        }

        var temp = Directory.CreateTempSubdirectory("rc-drive-");
        try
        {
            // Off the caller's thread: the catalog is written synchronously, like a local scan.
            return await Task.Run(() => ScanCoreAsync(temp.FullName, progress, cancellationToken), cancellationToken);
        }
        finally
        {
            gate.Exit();
            temp.Delete(recursive: true);
        }
    }

    public async Task<string> DownloadAsync(int bookFileId, CancellationToken cancellationToken = default)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var file = await db.BookFiles.AsNoTracking().FirstOrDefaultAsync(f => f.Id == bookFileId, cancellationToken);
        if (file?.DriveFileId is not { } driveId)
        {
            throw new InvalidOperationException("This file is not on Google Drive.");
        }

        if (!driveId.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
        {
            throw new DriveUnavailableException("This Drive file has an id the app does not accept."); // it names a local file
        }

        // The cached copy is named after the file's current version on Drive, so an edit there is never missed.
        var item = await files.GetAsync(driveId, cancellationToken)
            ?? throw new DriveUnavailableException($"{Path.GetFileName(DrivePath.Display(file.Path))} is no longer on Google Drive.");
        var version = item.Sha256 is { Length: >= 16 } sha ? sha[..16] : $"{item.ModifiedUtc.Ticks:x}-{item.Size:x}";
        var cache = Path.Combine(paths.DataFolder, "drive-cache");
        // The extension comes from the book's known format, never from the name on Drive (which could have been
        // renamed to something the system would run).
        var extension = file.Format == BookFormat.Pdf ? ".pdf" : ".epub";
        var local = Path.Combine(cache, $"{driveId}-{version}{extension}");
        if (File.Exists(local))
        {
            return local;
        }

        Directory.CreateDirectory(cache);
        foreach (var old in Directory.EnumerateFiles(cache, driveId + "-*"))
        {
            TryDelete(old); // earlier versions of this file
        }

        await DownloadToAsync(driveId, local, cancellationToken);
        return local;
    }

    private async Task<ScanSummary> ScanCoreAsync(string temp, IProgress<ScanProgress>? progress, CancellationToken cancellationToken)
    {
        List<DriveSource> sources;
        List<KnownFile> known, local;
        await using (var db = await contexts.CreateDbContextAsync(cancellationToken))
        {
            sources = await db.DriveSources.AsNoTracking().ToListAsync(cancellationToken);
            (local, known) = KnownFiles.Load(db);
        }

        // Everything is listed before anything changes: a source that cannot be read stops the scan here.
        var listed = await ListAsync(sources, cancellationToken);
        var knownByPath = known.ToDictionary(k => k.Path, StringComparer.Ordinal);
        var found = new List<FoundFile>(listed.Count);
        var filesRead = 0;
        foreach (var (item, display) in listed.Values)
        {
            var path = DrivePath.Make(item.Id, display);
            knownByPath.TryGetValue(path, out var k);
            var hash = item.Sha256 ?? (ScanReconciler.IsUnchanged(k, item.Size, item.ModifiedUtc) ? k!.ContentHash : null);
            if (hash is null)
            {
                // No checksum from Drive: download to hash, and delete at once (it is downloaded again if it is read).
                var copy = await DownloadToTempAsync(item, temp, cancellationToken);
                await using (var stream = File.OpenRead(copy))
                {
                    hash = ContentHash.Of(stream);
                }

                File.Delete(copy);
                filesRead++;
            }

            found.Add(new FoundFile(path, item.Size, item.ModifiedUtc, hash, item.Id));
        }

        var plan = ScanReconciler.Plan(known, found, [], local);
        var toRead = plan.Actions.Count(a => CatalogWriter.FileToRead(a) is not null);
        IReadOnlyList<UnreadableFile> unreadable = [];
        if (plan.Actions.Any(a => a is not KeepFile))
        {
            // One file at a time: downloaded, read, then deleted, so a first import never needs room for the whole
            // Drive library.
            string? current = null;
            await using var db = await contexts.CreateDbContextAsync(cancellationToken);
            var writer = new CatalogWriter(db, paths.CoversFolder, DateTime.UtcNow, _ => File.OpenRead(current!));
            var read = 0;
            foreach (var action in plan.Actions)
            {
                if (CatalogWriter.FileToRead(action) is { } file)
                {
                    progress?.Report(new ScanProgress(read++, toRead));
                    current = await DownloadToTempAsync(listed[file.DriveFileId!].Item, temp, cancellationToken);
                    filesRead++;

                    writer.Apply(action);
                    File.Delete(current);
                }
                else
                {
                    writer.Apply(action);
                }
            }

            await db.SaveChangesAsync(cancellationToken);
            unreadable = writer.Unreadable;
            progress?.Report(new ScanProgress(toRead, toRead));
        }

        int Count<T>() => plan.Actions.OfType<T>().Count();
        return new ScanSummary
        {
            Added = Count<AddBook>() + Count<SplitToNewBook>(),
            Moved = Count<MoveFile>(),
            Updated = Count<UpdateContent>() + Count<ReassignFile>(),
            MarkedMissing = Count<MarkMissing>(),
            FilesRead = filesRead,
            Unreadable = unreadable,
        };
    }

    /// <summary>Every supported file in the sources, once, by Drive id, with its display path.</summary>
    private async Task<Dictionary<string, (DriveItem Item, string Display)>> ListAsync(
        List<DriveSource> sources, CancellationToken cancellationToken)
    {
        var listed = new Dictionary<string, (DriveItem, string)>(StringComparer.Ordinal);
        foreach (var source in sources)
        {
            try
            {
                if (source.Kind == DriveSourceKind.File)
                {
                    await ListPickedFileAsync(source, listed, cancellationToken);
                }
                else
                {
                    await ListFolderAsync(source, listed, cancellationToken);
                }
            }
            catch (DriveUnavailableException e)
            {
                throw new DriveUnavailableException($"“{source.Name}” could not be read on Google Drive, so nothing was changed. {e.Message}", e);
            }
        }

        return listed;
    }

    // A picked file gone from Drive is simply not found (and so marked missing).
    private async Task ListPickedFileAsync(
        DriveSource source, Dictionary<string, (DriveItem, string)> listed, CancellationToken cancellationToken)
    {
        if (await files.GetAsync(source.DriveId, cancellationToken) is { IsFolder: false } item && BookMetadataReader.IsSupported(item.Name))
        {
            listed.TryAdd(item.Id, (item, item.Name));
        }
    }

    private async Task ListFolderAsync(
        DriveSource source, Dictionary<string, (DriveItem, string)> listed, CancellationToken cancellationToken)
    {
        var folders = new Queue<(string Id, string Display)>([(source.DriveId, source.Name)]);
        while (folders.TryDequeue(out var folder))
        {
            foreach (var child in await files.ListChildrenAsync(folder.Id, foldersOnly: false, cancellationToken))
            {
                var display = $"{folder.Display}/{child.Name}";
                if (child.IsFolder)
                {
                    folders.Enqueue((child.Id, display));
                }
                else if (BookMetadataReader.IsSupported(child.Name))
                {
                    listed.TryAdd(child.Id, (child, display));
                }
            }
        }
    }

    private static bool Covers(DriveSource source, BookFile file) => source.Kind == DriveSourceKind.File
        ? file.DriveFileId == source.DriveId
        : DrivePath.IsUnder(file.Path, source.Name);

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Still open in a reader app: left for next time.
        }
    }

    private async Task<string> DownloadToTempAsync(DriveItem item, string temp, CancellationToken cancellationToken)
    {
        var local = Path.Combine(temp, item.Id + ".download"); // read by the app only, never opened by the system
        await DownloadToAsync(item.Id, local, cancellationToken);
        return local;
    }

    /// <summary>Downloads beside <paramref name="local"/> and moves it in place, so a broken download never looks
    /// complete.</summary>
    private async Task DownloadToAsync(string driveId, string local, CancellationToken cancellationToken)
    {
        var partial = $"{local}.{Guid.NewGuid():N}.partial"; // two opens of one file never share a partial
        try
        {
            await using (var stream = File.Create(partial))
            {
                await files.DownloadAsync(driveId, stream, cancellationToken);
            }

            File.Move(partial, local, overwrite: true);
        }
        finally
        {
            File.Delete(partial);
        }
    }
}
