using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Metadata;

namespace ReaderCompanion.Data;

internal sealed class LibraryBooks(IDbContextFactory<LibraryDbContext> contexts) : ILibraryBooks
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
        Formats = b.Files.Select(f => f.Format).ToList(),
        DateAddedUtc = b.DateAddedUtc,
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

    public async Task<BookDetails?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var item = await db.Books.AsNoTracking().Where(b => b.Id == id).Select(ToListItem).SingleOrDefaultAsync(cancellationToken);
        if (item is null)
        {
            return null;
        }

        var rest = await db.Books.AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => new
            {
                b.Language,
                b.Isbn,
                Edited = b.EditedUtc != null,
                Files = b.Files.OrderBy(f => f.Path)
                    .Select(f => new BookFileInfo(f.Path, f.Format, f.SizeBytes, f.Status == FileStatus.Present))
                    .ToList(),
            })
            .SingleAsync(cancellationToken);
        return new BookDetails(DistinctFormats(item), rest.Language, rest.Isbn, rest.Edited, rest.Files);
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

    // SQLite cannot de-duplicate inside a nested collection, so formats are made distinct here.
    private static BookListItem DistinctFormats(BookListItem book) => book with { Formats = [.. book.Formats.Distinct().Order()] };
}
