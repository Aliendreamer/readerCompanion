using Microsoft.EntityFrameworkCore;
using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Organising;
using ReaderCompanion.Core.Scanning;

namespace ReaderCompanion.Data;

internal sealed class Organiser(IDbContextFactory<LibraryDbContext> contexts, LibraryWriteGate gate) : IOrganiser
{
    public async Task<IReadOnlyList<Move>> PreviewAsync(string root, OrganiseLayout layout, CancellationToken cancellationToken = default)
    {
        var folder = LibraryPaths.Normalize(root);
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var files = (await db.BookFiles.AsNoTracking()
                .Where(f => f.Status == FileStatus.Present)
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
                .ToListAsync(cancellationToken))
            .Where(f => LibraryPaths.IsSameOrUnder(f.Path, folder))
            .ToList();
        return OrganisePlanner.Plan(folder, layout, files, File.Exists);
    }

    public Task<OrganiseResult> ApplyAsync(string root, IReadOnlyList<Move> moves, CancellationToken cancellationToken = default) =>
        WithGateAsync(async () =>
        {
            var folder = LibraryPaths.Normalize(root);

            // Every move stays inside the chosen folder; anything else is refused, never attempted.
            var inside = moves.Where(m => LibraryPaths.IsSameOrUnder(m.From, folder) && LibraryPaths.IsSameOrUnder(m.To, folder)).ToList();
            var outside = moves.Except(inside).Select(m => new MoveFailure(m.From, "This move would leave the library folder.")).ToList();
            if (inside.Count == 0)
            {
                return new OrganiseResult(0, outside);
            }

            await using var db = await contexts.CreateDbContextAsync(cancellationToken);
            var operation = new FileOperation { CreatedUtc = DateTime.UtcNow, Root = folder };
            operation.Moves.AddRange(inside.Select(m => new FileMove { BookFileId = m.FileId, From = m.From, To = m.To }));
            db.FileOperations.Add(operation);
            await db.SaveChangesAsync(cancellationToken); // the log exists before any file moves

            var moved = await MoveAllAsync(db, operation.Moves.Select(m => (m, m.From, m.To)), done: true, cancellationToken);
            var result = moved with { Failed = [.. outside, .. moved.Failed] };
            RemoveEmptyFolders(folder, operation.Moves.Where(m => m.Done).Select(m => m.From));
            return result;
        });

    public async Task<LastOrganise?> LastAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        return await Undoable(db).AsNoTracking()
            .Select(o => new LastOrganise(o.CreatedUtc, o.Root, o.Moves.Count(m => m.Done)))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<OrganiseResult> UndoLastAsync(CancellationToken cancellationToken = default) =>
        WithGateAsync(async () =>
        {
            await using var db = await contexts.CreateDbContextAsync(cancellationToken);
            if (await Undoable(db).Include(o => o.Moves).FirstOrDefaultAsync(cancellationToken) is not { } operation)
            {
                return new OrganiseResult(0, []);
            }

            var done = operation.Moves.Where(m => m.Done).OrderByDescending(m => m.Id).ToList();
            var result = await MoveAllAsync(db, done.Select(m => (m, m.To, m.From)), done: false, cancellationToken);
            if (done.All(m => !m.Done)) // only when every file is back; otherwise the rest can be undone later
            {
                operation.UndoneUtc = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
            }
            RemoveEmptyFolders(operation.Root, done.Where(m => !m.Done).Select(m => m.To)); // the ones moved back
            return result;
        });

    /// <summary>The latest organise with moves still to undo.</summary>
    private static IQueryable<FileOperation> Undoable(LibraryDbContext db) =>
        db.FileOperations.Where(o => o.UndoneUtc == null && o.Moves.Any(m => m.Done)).OrderByDescending(o => o.Id);

    /// <summary>Moves each file and points its library record at the new path, one at a time so the log always matches
    /// the disk. A file that is gone, replaced, or whose target is taken is reported, not moved. Sets each moved log's
    /// <see cref="FileMove.Done"/> to <paramref name="done"/>.</summary>
    private static async Task<OrganiseResult> MoveAllAsync(
        LibraryDbContext db, IEnumerable<(FileMove Log, string From, string To)> moves, bool done, CancellationToken cancellationToken)
    {
        var moved = 0;
        var failed = new List<MoveFailure>();
        foreach (var (log, from, to) in moves)
        {
            if (await TryMoveAsync(db, log, from, to, done, cancellationToken) is { } reason)
            {
                failed.Add(new MoveFailure(from, reason));
            }
            else
            {
                moved++;
            }
        }

        return new OrganiseResult(moved, failed);
    }

    /// <summary>Moves one file; returns why it could not, or null when it did.</summary>
    private static async Task<string?> TryMoveAsync(
        LibraryDbContext db, FileMove log, string from, string to, bool done, CancellationToken cancellationToken)
    {
        var path = await db.BookFiles.Where(f => f.Id == log.BookFileId).Select(f => f.Path).FirstOrDefaultAsync(cancellationToken);
        if (path is null || !LibraryPaths.Comparer.Equals(path, from))
        {
            return "The library no longer has this file here.";
        }

        if (!File.Exists(from))
        {
            return "The file is no longer there.";
        }

        if (File.Exists(to))
        {
            return "Another file already has that name.";
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(to)!);
            File.Move(from, to, overwrite: false);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return e.Message;
        }

        // Two direct updates (not the change tracker), so each move costs the same however many came before. If the
        // library cannot be updated, the file goes back, so the disk and the library never disagree.
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await db.BookFiles.Where(f => f.Id == log.BookFileId).ExecuteUpdateAsync(u => u.SetProperty(f => f.Path, to), cancellationToken);
            await db.Set<FileMove>().Where(m => m.Id == log.Id).ExecuteUpdateAsync(u => u.SetProperty(m => m.Done, done), cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            File.Move(to, from, overwrite: false);
            return $"The library could not be updated, so the file was left where it was: {e.Message}";
        }

        log.Done = done;
        return null;
    }

    /// <summary>Removes folders under <paramref name="root"/> that the moves emptied, deepest first; never the root.</summary>
    private static void RemoveEmptyFolders(string root, IEnumerable<string> vacated)
    {
        var folders = vacated
            .SelectMany(path => Ancestors(Path.GetDirectoryName(path)!, root))
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
    }

    /// <summary>The folder and its parents up to, but not including, the (normalised) root.</summary>
    private static IEnumerable<string> Ancestors(string folder, string root)
    {
        for (var current = folder; !LibraryPaths.Comparer.Equals(current, root) && LibraryPaths.IsSameOrUnder(current, root);
             current = Path.GetDirectoryName(current)!)
        {
            yield return current;
        }
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
}
