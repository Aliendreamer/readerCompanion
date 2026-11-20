using Microsoft.EntityFrameworkCore;
using ReaderCompanion.Core.Library;

namespace ReaderCompanion.Data;

internal sealed class LibraryBooks(IDbContextFactory<LibraryDbContext> contexts) : ILibraryBooks
{
    public async Task<IReadOnlyList<BookListItem>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var books = await db.Books.AsNoTracking()
            .OrderBy(b => EF.Functions.Collate(b.Title, "NOCASE"))
            .Select(b => new BookListItem(
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
            })
            .ToListAsync(cancellationToken);

        // SQLite cannot de-duplicate inside a nested collection, so formats are made distinct here.
        return [.. books.Select(b => b with { Formats = [.. b.Formats.Distinct().Order()] })];
    }
}
