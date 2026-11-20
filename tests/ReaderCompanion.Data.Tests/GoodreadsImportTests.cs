using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReaderCompanion.Core.Goodreads;
using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Scanning;
using ReaderCompanion.Core.Tests;

namespace ReaderCompanion.Data.Tests;

public sealed class GoodreadsImportTests : IDisposable
{
    private readonly TempLibrary _library = new();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private IServiceProvider Scope => _library.Services.CreateScope().ServiceProvider;

    private IGoodreadsImport Import => Scope.GetRequiredService<IGoodreadsImport>();

    private static GoodreadsRow Row(
        string id, string title, string author, ReadStatus? status, string? isbn = null, int? rating = null,
        DateOnly? read = null, string? series = null, decimal? index = null) =>
        new(id, title, [author], isbn, rating, read, new DateOnly(2023, 1, 1), status, []) { Series = series, SeriesIndex = index };

    private int AddOwned(string title, string author, string? isbn = null)
    {
        using var db = _library.NewContext();
        var book = new Book { Title = title, Isbn = isbn };
        new CatalogNames(db).SetAuthors(book, [author]);
        book.Files.Add(TestFiles.File($"/b/{title}.epub"));
        db.Books.Add(book);
        db.SaveChanges();
        return book.Id;
    }

    private Book Load(string title)
    {
        using var db = _library.NewContext();
        return db.Books.AsNoTracking().Include(b => b.Sessions).Include(b => b.Files).Single(b => b.Title == title);
    }

    [Fact]
    public async Task OwnedBooksGetTheirReading_UnmatchedRowsBecomeWanted()
    {
        AddOwned("Mort", "Terry Pratchett");

        var summary = await Import.ImportAsync(
            [Row("1", "MORT", "terry pratchett", ReadStatus.Read, rating: 4, read: new DateOnly(2024, 3, 2)),
             Row("2", "Dune", "Frank Herbert", ReadStatus.WantToRead)], Token);

        Assert.Equal((1, 1), (summary.Matched, summary.AddedWanted));
        var mort = Load("Mort");
        Assert.Equal((ReadStatus.Read, 4), (mort.ReadStatus, mort.Rating));
        Assert.Equal(new DateOnly(2024, 3, 2), Assert.Single(mort.Sessions).FinishedOn);
        var dune = Load("Dune");
        Assert.Equal(ReadStatus.WantToRead, dune.ReadStatus);
        Assert.Empty(dune.Files);
        Assert.Equal("2", dune.GoodreadsId);
    }

    [Fact]
    public async Task MatchesByIsbn_EvenWithADifferentTitle()
    {
        AddOwned("Mort (Discworld 4)", "Pratchett", "9780552131063");

        var summary = await Import.ImportAsync([Row("1", "Mort", "Terry Pratchett", ReadStatus.Read, isbn: "9780552131063")], Token);

        Assert.Equal(1, summary.Matched);
        Assert.Equal(ReadStatus.Read, Load("Mort (Discworld 4)").ReadStatus);
    }

    [Fact]
    public async Task SecondImport_ChangesNothing()
    {
        AddOwned("Mort", "Terry Pratchett");
        GoodreadsRow[] rows =
            [Row("1", "Mort", "Terry Pratchett", ReadStatus.Read, rating: 4, read: new DateOnly(2024, 3, 2)),
             Row("2", "Dune", "Frank Herbert", ReadStatus.WantToRead)];
        await Import.ImportAsync(rows, Token);

        var again = await Import.ImportAsync(rows, Token);

        Assert.Equal((2, 0, 0), (again.Matched, again.AddedWanted, again.Updated));
        using var db = _library.NewContext();
        Assert.Equal(2, db.Books.Count());
        Assert.Equal(1, db.ReadingSessions.Count());
    }

    [Fact]
    public async Task UsersOwnStatusAndRating_AreKept_ButGoodreadsChangesStillApplyWhereUntouched()
    {
        var mort = AddOwned("Mort", "Terry Pratchett");
        await Import.ImportAsync([Row("1", "Mort", "Terry Pratchett", ReadStatus.WantToRead, rating: 3)], Token);
        var reading = Scope.GetRequiredService<IReading>();
        await reading.SetStatusAsync(mort, ReadStatus.Reading, new DateOnly(2026, 1, 1), Token);

        await Import.ImportAsync([Row("1", "Mort", "Terry Pratchett", ReadStatus.Read, rating: 5, read: new DateOnly(2026, 2, 1))], Token);

        var book = Load("Mort");
        Assert.Equal(ReadStatus.Reading, book.ReadStatus); // the user's choice
        Assert.Equal(5, book.Rating); // still what Goodreads set before, so it follows Goodreads
        Assert.Contains(book.Sessions, s => s.FinishedOn == new DateOnly(2026, 2, 1) && s.FromGoodreads);
    }

    [Fact]
    public async Task ScanFindingAWantedBook_MakesItOwned_AndKeepsItsHistory()
    {
        await Import.ImportAsync([Row("2", "Dune", "Frank Herbert", ReadStatus.WantToRead)], Token);
        var books = Path.Combine(_library.Root, "books");
        Directory.CreateDirectory(books);
        using (var epub = TestEpub.Build("<dc:title>Dune</dc:title><dc:creator>Frank Herbert</dc:creator>"))
        {
            File.WriteAllBytes(Path.Combine(books, "dune.epub"), epub.ToArray());
        }

        using (var db = _library.NewContext())
        {
            db.LibraryFolders.Add(new LibraryFolder { Path = books, AddedUtc = DateTime.UtcNow });
            db.SaveChanges();
        }

        await _library.Services.GetRequiredService<ILibraryScanner>().ScanAsync(cancellationToken: Token);

        using var check = _library.NewContext();
        var dune = check.Books.AsNoTracking().Include(b => b.Files).Single();
        Assert.Single(dune.Files);
        Assert.Equal(ReadStatus.WantToRead, dune.ReadStatus);
        Assert.Equal("2", dune.GoodreadsId);
    }

    [Fact]
    public async Task ARowRepeatedInTheExport_IsSkippedOnce_AndReported()
    {
        var row = Row("2", "Dune", "Frank Herbert", ReadStatus.WantToRead);

        var first = await Import.ImportAsync([row, row], Token);
        var second = await Import.ImportAsync([row, row], Token);

        Assert.Equal((1, 1), (first.AddedWanted, first.Skipped.Count));
        Assert.Equal((1, 1), (second.Matched, second.Skipped.Count));
    }

    [Fact]
    public async Task AReadTheUserLoggedNearTheSameDate_IsNotDuplicated()
    {
        var mort = AddOwned("Mort", "Terry Pratchett");
        await Scope.GetRequiredService<IReading>().SetStatusAsync(mort, ReadStatus.Read, new DateOnly(2024, 3, 2), Token);

        await Import.ImportAsync([Row("1", "Mort", "Terry Pratchett", ReadStatus.Read, read: new DateOnly(2024, 3, 1))], Token);

        Assert.Single(Load("Mort").Sessions);
    }

    [Fact]
    public async Task WantedBooks_GetTheirSeries_AndAScannedFileFillsWhatTheyMiss()
    {
        await Import.ImportAsync([Row("3", "Mistborn", "Brandon Sanderson", ReadStatus.WantToRead, series: "Mistborn", index: 1)], Token);
        var books = Path.Combine(_library.Root, "books");
        Directory.CreateDirectory(books);
        using (var epub = TestEpub.Build("<dc:title>Mistborn</dc:title><dc:creator>Brandon Sanderson</dc:creator><dc:language>en</dc:language>"))
        {
            File.WriteAllBytes(Path.Combine(books, "mistborn.epub"), epub.ToArray());
        }

        using (var db = _library.NewContext())
        {
            db.LibraryFolders.Add(new LibraryFolder { Path = books, AddedUtc = DateTime.UtcNow });
            db.SaveChanges();
        }

        await _library.Services.GetRequiredService<ILibraryScanner>().ScanAsync(cancellationToken: Token);

        using var check = _library.NewContext();
        var book = check.Books.AsNoTracking().Include(b => b.Series).Include(b => b.Files).Single();
        Assert.Equal(("Mistborn", 1m), (book.Series!.Name, book.SeriesIndex));
        Assert.Equal("en", book.Language);
        Assert.Single(book.Files);
    }

    public void Dispose() => _library.Dispose();
}
