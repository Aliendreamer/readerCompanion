using Microsoft.EntityFrameworkCore;
using ReaderCompanion.Core.Library;

namespace ReaderCompanion.Data.Tests;

/// <summary>At start, genres not on the list are mapped onto it and release-tagged series names are cleaned.</summary>
public sealed class CatalogCleanupTests : IDisposable
{
    private readonly TempLibrary _library = new();

    private void AddBook(string title, bool edited, params string[] genres)
    {
        using var db = _library.NewContext();
        var book = new Book { Title = title, EditedUtc = edited ? DateTime.UtcNow : null };
        new CatalogNames(db).SetGenres(book, genres);
        db.Books.Add(book);
        db.SaveChanges();
    }

    private List<string> Genres(string title)
    {
        using var db = _library.NewContext();
        return [.. db.Books.Include(b => b.Genres).ThenInclude(l => l.Genre).Single(b => b.Title == title).Genres.Select(l => l.Genre.Name).Order()];
    }

    [Fact]
    public void ScannedBooksAreMapped_EditedOnesKept_UnusedGenresRemoved()
    {
        AddBook("Dune", edited: false, "Fiction", "SciFi", "Star Wars");
        AddBook("Mine", edited: true, "My Picks");
        AddBook("Clean", edited: false, "Fantasy");

        CatalogCleanup.Run(_library.Services);

        Assert.Equal(["Science Fiction"], Genres("Dune"));
        Assert.Equal(["My Picks"], Genres("Mine"));
        Assert.Equal(["Fantasy"], Genres("Clean"));
        using var db = _library.NewContext();
        Assert.Equal(["Fantasy", "My Picks", "Science Fiction"], db.Genres.Select(g => g.Name).Order());
    }

    [Fact]
    public void AGenreOnTheListWrittenDifferently_TakesTheListsSpelling()
    {
        AddBook("Teen", edited: false, "young adult");

        CatalogCleanup.Run(_library.Services);

        Assert.Equal(["Young Adult"], Genres("Teen"));
    }

    [Fact]
    public void AReleaseTaggedSeries_IsCleaned_AndTheOldCollectionGoes()
    {
        using (var db = _library.NewContext())
        {
            var names = new CatalogNames(db);
            db.Books.Add(new Book { Title = "Storm Front", Series = names.Series("Serified.Dresden Files.SciFan.(#01 of 14)._.ICA Serified") });
            db.Books.Add(new Book { Title = "Fool Moon", Series = names.Series("Serified.Dresden Files.SciFan.(#02 of 14)._.ICA Serified") });
            db.SaveChanges();
        }

        CatalogCleanup.Run(_library.Services);

        using var check = _library.NewContext();
        Assert.Equal(
            [("Fool Moon", "Dresden Files", 2m), ("Storm Front", "Dresden Files", 1m)],
            check.Books.Include(b => b.Series).OrderBy(b => b.Title).AsEnumerable().Select(b => (b.Title, b.Series!.Name, b.SeriesIndex!.Value)));
        Assert.Equal(["Dresden Files"], check.Collections.Select(c => c.Name));
    }

    public void Dispose() => _library.Dispose();
}
