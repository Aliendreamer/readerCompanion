using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Metadata;

namespace ReaderCompanion.Data;

/// <summary>Fixes, at start, what earlier versions stored before scans cleaned it: genres not on
/// <see cref="GenreMap"/>'s list are mapped onto it, and release-tagged series names (<see cref="SeriesName"/>) are
/// cleaned. Books the user edited are left alone; genres and series no book uses any more are removed. Once a library
/// is clean the queries find nothing, so this costs a few lookups per start.</summary>
internal static class CatalogCleanup
{
    public static void Run(IServiceProvider services)
    {
        using var db = services.GetRequiredService<IDbContextFactory<LibraryDbContext>>().CreateDbContext();
        var listed = GenreMap.All.ToList();
        var names = new CatalogNames(db);
        foreach (var book in db.Books.Include(b => b.Genres).ThenInclude(l => l.Genre)
                     .Where(b => b.EditedUtc == null && b.Genres.Any(l => !listed.Contains(l.Genre.Name))).ToList())
        {
            names.SetGenres(book, GenreMap.Map([.. book.Genres.Select(l => l.Genre.Name)]));
        }

        // "(#" is the mark of the release-tagged form; SeriesName decides whether a name really is one.
        foreach (var book in db.Books.Include(b => b.Series)
                     .Where(b => b.EditedUtc == null && b.Series != null && b.Series.Name.Contains("(#")).ToList())
        {
            var (series, index) = SeriesName.Clean(book.Series!.Name, book.SeriesIndex);
            book.Series = names.Series(series);
            book.SeriesIndex = index;
        }

        // A genre kept from before under another spelling ("young adult") takes the list's.
        foreach (var genre in db.Genres.Where(g => !listed.Contains(g.Name)).ToList())
        {
            if (listed.FirstOrDefault(n => NameNormalizer.Normalize(n) == genre.NormalizedName) is { } spelling)
            {
                genre.Name = spelling;
            }
        }

        db.SaveChanges();
        db.Genres.Where(g => !db.Books.Any(b => b.Genres.Any(l => l.GenreId == g.Id))).ExecuteDelete();
        db.Collections.Where(c => c.Kind == CollectionKind.Series && !db.Books.Any(b => b.SeriesId == c.Id)).ExecuteDelete();
    }
}
