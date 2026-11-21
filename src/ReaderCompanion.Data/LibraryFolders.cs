using Microsoft.EntityFrameworkCore;
using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Scanning;

namespace ReaderCompanion.Data;

/// <summary>Each call uses its own short-lived context, so nothing goes stale between calls.</summary>
internal sealed class LibraryFolders(IDbContextFactory<LibraryDbContext> contexts) : ILibraryFolders
{
    public async Task<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        return await db.LibraryFolders.OrderBy(f => f.Path).Select(f => f.Path).ToListAsync(cancellationToken);
    }

    public async Task<LinkFolderResult> LinkAsync(string path, CancellationToken cancellationToken = default)
    {
        var folder = LibraryPaths.Normalize(path);
        if (!Directory.Exists(folder))
        {
            return new LinkFolderResult(false, $"Folder not found: {folder}");
        }

        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var existing = await db.LibraryFolders.ToListAsync(cancellationToken);
        if (existing.Any(f => f.Path.Equals(folder, LibraryPaths.Comparison)))
        {
            return new LinkFolderResult(false, "This folder is already linked.");
        }

        var covering = existing.FirstOrDefault(f => LibraryPaths.IsSameOrUnder(folder, f.Path));
        if (covering is not null)
        {
            return new LinkFolderResult(false, $"Already covered by the linked folder {covering.Path}.");
        }

        var replaced = existing.Where(f => LibraryPaths.IsSameOrUnder(f.Path, folder)).ToList();
        db.LibraryFolders.RemoveRange(replaced);
        db.LibraryFolders.Add(new LibraryFolder { Path = folder, AddedUtc = DateTime.UtcNow });
        await db.SaveChangesAsync(cancellationToken);

        return new LinkFolderResult(true) { Replaced = replaced.Select(f => f.Path).ToList() };
    }

    public async Task UnlinkAsync(string path, CancellationToken cancellationToken = default)
    {
        var folder = LibraryPaths.Normalize(path);
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var linked = (await db.LibraryFolders.ToListAsync(cancellationToken))
            .Where(f => f.Path.Equals(folder, LibraryPaths.Comparison))
            .ToList();
        if (linked.Count == 0)
        {
            return;
        }

        var present = await db.BookFiles.Where(f => f.Status == FileStatus.Present && f.DriveFileId == null).ToListAsync(cancellationToken);
        foreach (var file in present.Where(f => LibraryPaths.IsSameOrUnder(f.Path, folder)))
        {
            file.Status = FileStatus.Missing;
        }

        db.LibraryFolders.RemoveRange(linked);
        await db.SaveChangesAsync(cancellationToken);
    }
}
