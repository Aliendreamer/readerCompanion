using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ReaderCompanion.Core.Drive;
using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Metadata;

namespace ReaderCompanion.Data;

internal sealed class LibraryBooks(
    IDbContextFactory<LibraryDbContext> contexts, ITrash trash, IDriveAccount drive, GoogleDriveFiles driveFiles) : ILibraryBooks
{
    private static readonly Expression<Func<Book, BookListItem>> ToListItem = b => new BookListItem(
        b.Id,
        b.Title,
        b.Authors.OrderBy(a => a.Order).Select(a => a.Author.Name).ToList(),
        b.Series != null ? b.Series.Name : null,
        b.SeriesIndex,
        !b.Files.Any(f => f.Status == FileStatus.Present),
        b.CoverFileName)
    {
        Genres = b.Genres.Select(g => g.Genre.Name).OrderBy(n => n).ToList(),
        Collections = b.Collections.Select(c => c.Collection.Name).OrderBy(n => n).ToList(),
        Formats = b.Files.Select(f => f.Format).ToList(),
        DateAddedUtc = b.DateAddedUtc,
        ReadStatus = b.ReadStatus,
        Rating = b.Rating,
    };

    public async Task<IReadOnlyList<BookListItem>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var books = await db.Books.AsNoTracking()
            .OrderBy(b => EF.Functions.Collate(b.Title, "NOCASE"))
            .Select(ToListItem)
            .ToListAsync(cancellationToken);
        return [.. books.Select(DistinctFormats)];
    }

    public async Task<BookDetails?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        (await GetManyAsync([id], cancellationToken)).SingleOrDefault();

    public async Task<IReadOnlyList<BookDetails>> GetManyAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var items = await db.Books.AsNoTracking()
            .Where(b => ids.Contains(b.Id))
            .Select(ToListItem)
            .ToDictionaryAsync(b => b.Id, cancellationToken);
        var rest = await db.Books.AsNoTracking()
            .Where(b => ids.Contains(b.Id))
            .OrderBy(b => b.Id)
            .Select(b => new
            {
                b.Id,
                b.Language,
                b.Isbn,
                Edited = b.EditedUtc != null,
                Files = b.Files.OrderBy(f => f.Path)
                    .Select(f => new BookFileInfo(f.Path, f.Format, f.SizeBytes, f.Status == FileStatus.Present, f.Id) { DriveFileId = f.DriveFileId })
                    .ToList(),
            })
            .ToListAsync(cancellationToken);
        return [.. rest
            .Where(r => items.ContainsKey(r.Id))
            .Select(r => new BookDetails(DistinctFormats(items[r.Id]), r.Language, r.Isbn, r.Edited, r.Files))];
    }

    public async Task<bool> UpdateAsync(int id, BookEdit edit, CancellationToken cancellationToken = default)
    {
        var title = NameNormalizer.Clean(edit.Title) ?? throw new ArgumentException("A book needs a title.", nameof(edit));

        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var book = await db.Books
            .Include(b => b.Authors).ThenInclude(l => l.Author)
            .Include(b => b.Genres).ThenInclude(l => l.Genre)
            .SingleOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (book is null)
        {
            return false;
        }

        var names = new CatalogNames(db);
        book.Title = title;
        book.Series = names.Series(edit.Series);
        book.SeriesIndex = book.Series is null ? null : edit.SeriesIndex;
        names.SetAuthors(book, edit.Authors);
        names.SetGenres(book, edit.Genres);
        book.Language = NameNormalizer.Clean(edit.Language);
        book.Isbn = NameNormalizer.Clean(edit.Isbn);
        book.EditedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<TrashResult> TrashFileAsync(int fileId, CancellationToken cancellationToken = default)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        if (await db.BookFiles.FindAsync([fileId], cancellationToken) is not { } file)
        {
            return TrashResult.NotFound;
        }

        // The book must keep a file that is actually there; a row for a missing file does not count.
        if (!await db.BookFiles.AnyAsync(f => f.BookId == file.BookId && f.Id != fileId && f.Status == FileStatus.Present, cancellationToken))
        {
            return TrashResult.LastFile;
        }

        // The file goes to the trash first (off the UI thread: across disks it is a copy; a Drive file to Drive's
        // trash); only then does the library forget it, so a refused trash changes nothing.
        if (file.DriveFileId is { } driveId)
        {
            if (drive.Link is not { CanWrite: true })
            {
                return TrashResult.ReadOnly;
            }

            try
            {
                await driveFiles.TrashAsync(driveId, cancellationToken);
            }
            catch (DriveUnavailableException)
            {
                return TrashResult.TrashFailed;
            }
        }
        else if (!await Task.Run(() => trash.MoveToTrash(file.Path), cancellationToken))
        {
            return TrashResult.TrashFailed;
        }

        db.BookFiles.Remove(file);
        await db.SaveChangesAsync(CancellationToken.None); // the file is gone: the library must follow, cancelled or not
        return TrashResult.Trashed;
    }

    public async Task<bool> AddToCollectionAsync(int id, string name, CancellationToken cancellationToken = default)
    {
        var clean = NameNormalizer.Clean(name) ?? throw new ArgumentException("A collection needs a name.", nameof(name));
        var key = NameNormalizer.Normalize(clean);

        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var book = await db.Books.Include(b => b.Collections).SingleOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (book is null)
        {
            return false;
        }

        var collection = await db.Collections.SingleOrDefaultAsync(c => c.Kind == CollectionKind.User && c.NormalizedName == key, cancellationToken)
            ?? new Collection { Name = clean, NormalizedName = key, Kind = CollectionKind.User };
        if (book.Collections.All(l => l.CollectionId != collection.Id))
        {
            book.Collections.Add(new BookCollection { Collection = collection });
            await db.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task<bool> RemoveFromCollectionAsync(int id, string name, CancellationToken cancellationToken = default)
    {
        var key = NameNormalizer.Normalize(name);
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var book = await db.Books
            .Include(b => b.Collections).ThenInclude(l => l.Collection)
            .SingleOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (book is null)
        {
            return false;
        }

        if (book.Collections.FirstOrDefault(l => l.Collection.NormalizedName == key) is { } link)
        {
            book.Collections.Remove(link);
            if (!await db.Set<BookCollection>().AnyAsync(l => l.CollectionId == link.CollectionId && l.BookId != id, cancellationToken))
            {
                db.Collections.Remove(link.Collection); // nothing else is in it
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    // SQLite cannot de-duplicate inside a nested collection, so formats are made distinct here; a book is owned
    // when it has any file at all.
    private static BookListItem DistinctFormats(BookListItem book) =>
        book with { Formats = [.. book.Formats.Distinct().Order()], Owned = book.Formats.Count > 0 };
}
