using Microsoft.EntityFrameworkCore;
using ReaderCompanion.Core.Drive;
using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Organising;
using ReaderCompanion.Core.Scanning;

namespace ReaderCompanion.Data;

/// <summary>Organises a local library folder or a Drive folder (a root written as a Drive path). The orchestration,
/// the log and the library updates are shared; an <see cref="IMover"/> does the moves where the files are.</summary>
internal sealed class Organiser(
    IDbContextFactory<LibraryDbContext> contexts, LibraryWriteGate gate, IDriveAccount drive, GoogleDriveFiles driveFiles) : IOrganiser
{
    public async Task<IReadOnlyList<Move>> PreviewAsync(
        string root, OrganiseLayout layout, IProgress<OrganiseProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        progress?.Report(new OrganiseProgress(OrganiseStage.Planning));
        var onDrive = DrivePath.IsDrive(root);
        var folder = onDrive ? DrivePath.Display(root) : LibraryPaths.Normalize(root);
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var present = await db.BookFiles.AsNoTracking()
            .Where(f => f.Status == FileStatus.Present && (f.DriveFileId != null) == onDrive)
            .OrderBy(f => f.Path)
            .Select(f => new PlannedFile(
                f.Id,
                f.Path,
                f.Book.Title,
                f.Book.Authors.OrderBy(a => a.Order).Select(a => a.Author.Name).ToList(),
                f.Book.Series != null ? f.Book.Series.Name : null,
                f.Book.SeriesIndex,
                f.Book.Genres.Select(g => g.Genre.Name).OrderBy(n => n).ToList(),
                f.Format))
            .ToListAsync(cancellationToken);
        if (!onDrive)
        {
            return OrganisePlanner.Plan(folder, layout, [.. present.Where(f => LibraryPaths.IsSameOrUnder(f.Path, folder))], File.Exists);
        }

        // Only files really in this Drive folder's tree count (another source may have a folder of the same name), and
        // any file there takes its name. The planner works on file-system paths: plan the display paths with this
        // system's separator, then turn the results back into Drive paths.
        var tree = new DriveMover(root, driveFiles);
        await tree.LoadAsync(cancellationToken);
        var inside = present.Where(f => tree.Holds(f.Path)).ToDictionary(f => f.FileId);
        static string Local(string display) => display.Replace('/', Path.DirectorySeparatorChar);
        var planned = inside.Values.Select(f => f with { Path = Local(DrivePath.Display(f.Path)) }).ToList();
        var taken = tree.FilePaths.Select(Local).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return [.. OrganisePlanner.Plan(Local(folder), layout, planned, taken.Contains).Select(m => new Move(
            m.FileId,
            inside[m.FileId].Path,
            DrivePath.Make(DrivePath.Id(inside[m.FileId].Path), m.To.Replace(Path.DirectorySeparatorChar, '/'))))];
    }

    public Task<OrganiseResult> ApplyAsync(
        string root, IReadOnlyList<Move> moves, IProgress<OrganiseProgress>? progress = null, CancellationToken cancellationToken = default) =>
        WithGateAsync(async () =>
        {
            var mover = MoverFor(root);
            await mover.LoadAsync(cancellationToken);

            // Every move stays inside the chosen folder; anything else is refused, never attempted.
            var inside = moves.Where(m => mover.Holds(m.From) && mover.Holds(m.To)).ToList();
            var outside = moves.Except(inside).Select(m => new MoveFailure(m.From, "This move would leave the library folder.")).ToList();
            if (inside.Count == 0)
            {
                return new OrganiseResult(0, outside);
            }

            await using var db = await contexts.CreateDbContextAsync(cancellationToken);
            var operation = new FileOperation { CreatedUtc = DateTime.UtcNow, Root = mover.Root };
            operation.Moves.AddRange(inside.Select(m => new FileMove { BookFileId = m.FileId, From = m.From, To = m.To }));
            db.FileOperations.Add(operation);
            await db.SaveChangesAsync(cancellationToken); // the log exists before any file moves

            var moved = await MoveAllAsync(
                db, mover, [.. operation.Moves.Select(m => (m, m.From, m.To))], done: true, Stage(progress, OrganiseStage.Moving), cancellationToken);
            progress?.Report(new OrganiseProgress(OrganiseStage.Tidying));
            await mover.RemoveEmptyFoldersAsync(operation.Moves.Where(m => m.Done).Select(m => m.From), cancellationToken);
            return moved with { Failed = [.. outside, .. moved.Failed] };
        });

    public async Task<LastOrganise?> LastAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        return await Undoable(db).AsNoTracking()
            .Select(o => new LastOrganise(o.CreatedUtc, o.Root, o.Moves.Count(m => m.Done)))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<OrganiseResult> UndoLastAsync(IProgress<OrganiseProgress>? progress = null, CancellationToken cancellationToken = default) =>
        WithGateAsync(async () =>
        {
            await using var db = await contexts.CreateDbContextAsync(cancellationToken);
            if (await Undoable(db).Include(o => o.Moves).FirstOrDefaultAsync(cancellationToken) is not { } operation)
            {
                return new OrganiseResult(0, []);
            }

            var mover = MoverFor(operation.Root);
            await mover.LoadAsync(cancellationToken);
            var done = operation.Moves.Where(m => m.Done).OrderByDescending(m => m.Id).ToList();
            var result = await MoveAllAsync(
                db, mover, [.. done.Select(m => (m, m.To, m.From))], done: false, Stage(progress, OrganiseStage.MovingBack), cancellationToken);
            if (done.All(m => !m.Done)) // only when every file is back; otherwise the rest can be undone later
            {
                operation.UndoneUtc = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
            }

            progress?.Report(new OrganiseProgress(OrganiseStage.Tidying));
            await mover.RemoveEmptyFoldersAsync(done.Where(m => !m.Done).Select(m => m.To), cancellationToken); // the ones moved back
            return result;
        });

    /// <summary>The mover for a root. A Drive root needs a link that may change files.</summary>
    private IMover MoverFor(string root)
    {
        if (!DrivePath.IsDrive(root))
        {
            return new LocalMover(LibraryPaths.Normalize(root));
        }

        return drive.Link is { CanOrganise: true }
            ? new DriveMover(root, driveFiles)
            : throw new InvalidOperationException("Organising a Drive folder needs the Google Drive access “Whole Drive, read and write”.");
    }

    /// <summary>The latest organise with moves still to undo.</summary>
    private static IQueryable<FileOperation> Undoable(LibraryDbContext db) =>
        db.FileOperations.Where(o => o.UndoneUtc == null && o.Moves.Any(m => m.Done)).OrderByDescending(o => o.Id);

    /// <summary>Moves each file and points its library record at the new path, one at a time so the log always matches
    /// the files. A file that is gone, replaced, or whose target is taken is reported, not moved. Sets each moved log's
    /// <see cref="FileMove.Done"/> to <paramref name="done"/>.</summary>
    private static async Task<OrganiseResult> MoveAllAsync(
        LibraryDbContext db, IMover mover, IReadOnlyList<(FileMove Log, string From, string To)> moves, bool done,
        Action<int, int>? report, CancellationToken cancellationToken)
    {
        var moved = 0;
        var failed = new List<MoveFailure>();
        report?.Invoke(0, moves.Count);
        foreach (var (index, (log, from, to)) in moves.Index())
        {
            if (await TryMoveAsync(db, mover, log, from, to, done, cancellationToken) is { } reason)
            {
                failed.Add(new MoveFailure(from, reason));
            }
            else
            {
                moved++;
            }

            report?.Invoke(index + 1, moves.Count);
        }

        return new OrganiseResult(moved, failed);
    }

    private static Action<int, int>? Stage(IProgress<OrganiseProgress>? progress, OrganiseStage stage) =>
        progress is null ? null : (done, total) => progress.Report(new OrganiseProgress(stage, done, total));

    /// <summary>Moves one file; returns why it could not, or null when it did.</summary>
    private static async Task<string?> TryMoveAsync(
        LibraryDbContext db, IMover mover, FileMove log, string from, string to, bool done, CancellationToken cancellationToken)
    {
        var path = await db.BookFiles.Where(f => f.Id == log.BookFileId).Select(f => f.Path).FirstOrDefaultAsync(cancellationToken);
        if (path is null || !LibraryPaths.Comparer.Equals(path, from))
        {
            return "The library no longer has this file here.";
        }

        if (await mover.MoveAsync(from, to, cancellationToken) is { } reason)
        {
            return reason;
        }

        // Two direct updates (not the change tracker), so each move costs the same however many came before. If the
        // library cannot be updated, the file goes back, so the files and the library never disagree.
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await db.BookFiles.Where(f => f.Id == log.BookFileId).ExecuteUpdateAsync(u => u.SetProperty(f => f.Path, to), cancellationToken);
            await db.Set<FileMove>().Where(m => m.Id == log.Id).ExecuteUpdateAsync(u => u.SetProperty(m => m.Done, done), cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            return await mover.MoveAsync(to, from, CancellationToken.None) is { } stuck
                ? $"The library could not be updated ({e.Message}) and the file could not be moved back ({stuck}); it is now at {to}. A scan will follow it."
                : $"The library could not be updated, so the file was left where it was: {e.Message}";
        }

        log.Done = done;
        return null;
    }

    private async Task<OrganiseResult> WithGateAsync(Func<Task<OrganiseResult>> work)
    {
        if (!gate.TryEnter(LibraryWork.Organise))
        {
            throw new InvalidOperationException(gate.BusyMessage);
        }

        try
        {
            return await Task.Run(work);
        }
        finally
        {
            gate.Exit();
        }
    }

    /// <summary>Moves files where they live, inside one root.</summary>
    private interface IMover
    {
        /// <summary>The root as the operation log keeps it.</summary>
        string Root { get; }

        /// <summary>Learns what is in the root, before <see cref="Holds"/> is asked.</summary>
        Task LoadAsync(CancellationToken cancellationToken);

        bool Holds(string path);

        /// <summary>Moves one file; returns why it could not, or null when it did. Never overwrites.</summary>
        Task<string?> MoveAsync(string from, string to, CancellationToken cancellationToken);

        /// <summary>Removes folders under the root that the moves emptied, deepest first; never the root.</summary>
        Task RemoveEmptyFoldersAsync(IEnumerable<string> vacated, CancellationToken cancellationToken);
    }

    private sealed class LocalMover(string root) : IMover
    {
        public string Root => root;

        public Task LoadAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public bool Holds(string path) => !DrivePath.IsDrive(path) && LibraryPaths.IsSameOrUnder(path, root);

        public Task<string?> MoveAsync(string from, string to, CancellationToken cancellationToken)
        {
            if (!File.Exists(from))
            {
                return Task.FromResult<string?>("The file is no longer there.");
            }

            if (File.Exists(to))
            {
                return Task.FromResult<string?>("Another file already has that name.");
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(to)!);
                File.Move(from, to, overwrite: false);
                return Task.FromResult<string?>(null);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                return Task.FromResult<string?>(e.Message);
            }
        }

        public Task RemoveEmptyFoldersAsync(IEnumerable<string> vacated, CancellationToken cancellationToken)
        {
            var folders = vacated
                .SelectMany(path => Ancestors(Path.GetDirectoryName(path)!))
                .Distinct(LibraryPaths.Comparer)
                .OrderByDescending(f => f.Length);
            foreach (var folder in folders)
            {
                try
                {
                    if (Directory.Exists(folder) && !Directory.EnumerateFileSystemEntries(folder).Any())
                    {
                        Directory.Delete(folder);
                    }
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    // A folder we cannot remove is left as it is.
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>The folder and its parents up to, but not including, the root.</summary>
        private IEnumerable<string> Ancestors(string folder)
        {
            for (var current = folder; !LibraryPaths.Comparer.Equals(current, root) && LibraryPaths.IsSameOrUnder(current, root);
                 current = Path.GetDirectoryName(current)!)
            {
                yield return current;
            }
        }
    }

    /// <summary>Moves Drive files inside a Drive folder: one rename-and-move call per file, creating folders as needed.
    /// The folder's tree is listed once (<see cref="LoadAsync"/>) and kept current for the operation.</summary>
    private sealed class DriveMover(string root, GoogleDriveFiles files) : IMover
    {
        private readonly string _rootName = DrivePath.Display(root);
        private readonly Dictionary<string, string> _folderIds = new(StringComparer.Ordinal) { [DrivePath.Display(root)] = DrivePath.Id(root) };
        private readonly Dictionary<string, List<DriveItem>> _children = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _filePaths = new(StringComparer.Ordinal); // file id → display path

        public string Root => root;

        /// <summary>The display paths of every file in the tree, books or not.</summary>
        public IEnumerable<string> FilePaths => _filePaths.Values;

        /// <summary>True for a Drive file that is in this folder's tree (not merely under a folder of the same name).</summary>
        public bool Holds(string path) =>
            DrivePath.IsUnder(path, _rootName) && _filePaths.ContainsKey(DrivePath.Id(path));

        public async Task LoadAsync(CancellationToken cancellationToken)
        {
            var folders = new Queue<(string Id, string Display)>([(DrivePath.Id(root), _rootName)]);
            while (folders.TryDequeue(out var folder))
            {
                foreach (var child in await ChildrenAsync(folder.Id, cancellationToken))
                {
                    var display = $"{folder.Display}/{child.Name}";
                    if (child.IsFolder)
                    {
                        _folderIds.TryAdd(display, child.Id);
                        folders.Enqueue((child.Id, display));
                    }
                    else
                    {
                        _filePaths[child.Id] = display;
                    }
                }
            }
        }

        public async Task<string?> MoveAsync(string from, string to, CancellationToken cancellationToken)
        {
            var id = DrivePath.Id(from);
            if (DrivePath.Id(to) != id)
            {
                return "This move would change which Drive file it is.";
            }

            try
            {
                // Listed with the tree, normally; otherwise (moved meanwhile) asked of Drive.
                var item = _children.Values.SelectMany(c => c).FirstOrDefault(c => c.Id == id)
                    ?? await files.GetAsync(id, cancellationToken);
                if (item is null)
                {
                    return "The file is no longer there.";
                }

                var (folder, name) = DrivePath.Split(DrivePath.Display(to));
                var folderId = await EnsureFolderAsync(folder, cancellationToken);
                var siblings = await ChildrenAsync(folderId, cancellationToken);
                if (siblings.Any(s => s.Id != id && string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)))
                {
                    return "Another file already has that name.";
                }

                await files.MoveAsync(id, name, folderId, item.Parents, cancellationToken);
                foreach (var parent in item.Parents.Where(_children.ContainsKey))
                {
                    _children[parent].RemoveAll(c => c.Id == id);
                }

                siblings.Add(item with { Name = name, Parents = [folderId] });
                _filePaths[id] = DrivePath.Display(to);
                return null;
            }
            catch (DriveUnavailableException e)
            {
                return e.Message;
            }
        }

        public async Task RemoveEmptyFoldersAsync(IEnumerable<string> vacated, CancellationToken cancellationToken)
        {
            var folders = vacated
                .SelectMany(path => Ancestors(DrivePath.Split(DrivePath.Display(path)).Folder))
                .Distinct(StringComparer.Ordinal)
                .OrderByDescending(f => f.Length);
            foreach (var folder in folders)
            {
                try
                {
                    // Listed afresh: something else may have put a file there meanwhile.
                    if (await FindFolderAsync(folder, cancellationToken) is { } id
                        && (await files.ListChildrenAsync(id, foldersOnly: false, cancellationToken)).Count == 0)
                    {
                        await files.TrashAsync(id, cancellationToken); // recoverable from Drive's trash
                        _folderIds.Remove(folder);
                    }
                }
                catch (DriveUnavailableException)
                {
                    // A folder we cannot remove is left as it is.
                }
            }
        }

        /// <summary>The id of the folder at a display path under the root, or null when there is none.</summary>
        private async Task<string?> FindFolderAsync(string folder, CancellationToken cancellationToken)
        {
            if (_folderIds.TryGetValue(folder, out var known))
            {
                return known;
            }

            if (!DrivePath.IsSameOrUnder(folder, _rootName))
            {
                return null;
            }

            var (parentPath, name) = DrivePath.Split(folder);
            if (await FindFolderAsync(parentPath, cancellationToken) is not { } parentId)
            {
                return null;
            }

            var id = (await ChildrenAsync(parentId, cancellationToken)).FirstOrDefault(c => c.IsFolder && c.Name == name)?.Id;
            return id is null ? null : _folderIds[folder] = id;
        }

        /// <summary>The id of the folder at a display path under the root, creating it and its parents as needed.</summary>
        private async Task<string> EnsureFolderAsync(string folder, CancellationToken cancellationToken)
        {
            if (await FindFolderAsync(folder, cancellationToken) is { } id)
            {
                return id;
            }

            var (parentPath, name) = DrivePath.Split(folder);
            var parentId = await EnsureFolderAsync(parentPath, cancellationToken);
            id = await files.CreateFolderAsync(name, parentId, cancellationToken);
            (await ChildrenAsync(parentId, cancellationToken)).Add(new DriveItem(id, name, true, 0, default, null, [parentId]));
            return _folderIds[folder] = id;
        }

        private async Task<List<DriveItem>> ChildrenAsync(string folderId, CancellationToken cancellationToken) =>
            _children.TryGetValue(folderId, out var known)
                ? known
                : _children[folderId] = [.. await files.ListChildrenAsync(folderId, foldersOnly: false, cancellationToken)];

        private IEnumerable<string> Ancestors(string folder)
        {
            for (var current = folder; current != _rootName && DrivePath.IsSameOrUnder(current, _rootName); current = DrivePath.Split(current).Folder)
            {
                yield return current;
            }
        }
    }
}
