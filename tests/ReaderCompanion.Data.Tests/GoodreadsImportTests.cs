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

    [Fact]
    public async Task AnotherEdition_IsTheSameBook()
    {
        AddOwned("Mort", "Terry Pratchett", "9780552131063");

        var summary = await Import.ImportAsync(
            [Row("1", "Mort", "Terry Pratchett", ReadStatus.Read, isbn: "9780061020681", read: new DateOnly(2024, 3, 2))], Token);

        Assert.Equal((1, 0, 0), (summary.Matched, summary.AddedWanted, summary.ForReview));
        var mort = Load("Mort");
        Assert.Equal((ReadStatus.Read, "1", "9780552131063"), (mort.ReadStatus, mort.GoodreadsId, mort.Isbn)); // keeps its own ISBN
    }

    [Fact]
    public async Task ATitleWithoutASharedAuthor_WaitsForReview_ThenConfirmOrRejectDecides()
    {
        AddOwned("Mort", "Unknown");
        AddOwned("Dune", "Unknown");
        GoodreadsRow[] rows =
            [Row("1", "Mort", "Terry Pratchett", ReadStatus.Read, read: new DateOnly(2024, 3, 2)),
             Row("2", "Dune", "Frank Herbert", ReadStatus.Read)];

        var summary = await Import.ImportAsync(rows, Token);
        var again = await Import.ImportAsync(rows, Token);

        Assert.Equal((0, 0, 2), (summary.Matched, summary.AddedWanted, summary.ForReview));
        Assert.Equal(2, again.ForReview);
        var pending = await Import.PendingAsync(Token);
        Assert.Equal(["Dune", "Mort"], pending.Select(p => p.Title).Order());
        Assert.Equal(ReadStatus.NotStarted, Load("Mort").ReadStatus);

        await Import.ConfirmAsync(pending.Single(p => p.Title == "Mort").Id, Token);
        await Import.RejectAsync(pending.Single(p => p.Title == "Dune").Id, Token);

        Assert.Empty(await Import.PendingAsync(Token));
        Assert.Equal(ReadStatus.Read, Load("Mort").ReadStatus);
        Assert.Equal("1", Load("Mort").GoodreadsId);
        using var db = _library.NewContext();
        Assert.Equal(2, db.Books.Count(b => b.Title == "Dune"));
        Assert.Equal(0, (await Import.ImportAsync(rows, Token)).ForReview); // answered rows now match by id
    }

    [Fact]
    public async Task SubtitleOrAuthorWrittenDifferently_StillMatches()
    {
        AddOwned("Designing Data-Intensive Applications", "Kleppmann, Martin");
        AddOwned("Node.js Design Patterns", "Mario Casciaro & Luciano Mammino");
        AddOwned("The Pragmatic Programmer: your journey to mastery, 20th Anniversary Edition", "Andrew Hunt");

        var summary = await Import.ImportAsync(
            [Row("1", "Designing Data-Intensive Applications: The Big Ideas Behind Reliable, Scalable, and Maintainable Systems",
                 "Martin Kleppmann", ReadStatus.Read, isbn: "9781098119027"),
             Row("2", "Node.js Design Patterns: Master a series of patterns", "Mario Casciaro", ReadStatus.Read),
             Row("3", "The Pragmatic Programmer: From Journeyman to Master", "Andy Hunt", ReadStatus.Read, isbn: "9780201616224")],
            Token);

        Assert.Equal((3, 0, 0), (summary.Matched, summary.AddedWanted, summary.ForReview));
        Assert.Equal(ReadStatus.Read, Load("Designing Data-Intensive Applications").ReadStatus);
        Assert.Equal("2", Load("Node.js Design Patterns").GoodreadsId);
    }

    [Fact]
    public async Task SameMainTitle_WithoutASharedAuthor_WaitsForReview()
    {
        var owned = AddOwned("Refactoring: Improving the Design of Existing Code \\( PDFDrive.com \\).epub", "Unknown");

        var summary = await Import.ImportAsync(
            [Row("1", "Refactoring: Improving the Design of Existing Code", "Martin Fowler", ReadStatus.Read, isbn: "9780201485677")], Token);

        Assert.Equal((0, 0, 1), (summary.Matched, summary.AddedWanted, summary.ForReview));
        Assert.Equal(owned, Assert.Single(await Import.PendingAsync(Token)).BookId);
    }

    [Fact]
    public async Task SameMainTitleAndAuthor_WithAnotherEditionsIsbn_IsTheSameBook()
    {
        AddOwned("Effective TypeScript", "Dan Vanderkam", "9781098155063");

        var summary = await Import.ImportAsync(
            [Row("1", "Effective TypeScript: 62 Specific Ways", "Dan Vanderkam", ReadStatus.Read, isbn: "9781492053705")], Token);

        Assert.Equal((1, 0, 0), (summary.Matched, summary.AddedWanted, summary.ForReview));
    }

    [Fact]
    public async Task ABookAlreadyLinkedToGoodreads_IsNotMatchedByMainTitleAgain()
    {
        AddOwned("Clean Code", "Robert C. Martin");

        var summary = await Import.ImportAsync(
            [Row("1", "Clean Code: A Handbook of Agile Software Craftsmanship", "Robert C. Martin", ReadStatus.Read),
             Row("2", "Clean Code: The Audiobook", "Robert C. Martin", ReadStatus.WantToRead)], Token);

        Assert.Equal((1, 1, 0), (summary.Matched, summary.AddedWanted, summary.ForReview));
        Assert.Equal("1", Load("Clean Code").GoodreadsId);
    }

    /// <summary>The user's own export, kept encrypted in the repository: runs only when it is present and decrypted.</summary>
    [Fact]
    public async Task TheRealExport_ImportsEveryRow()
    {
        var export = RealExport();
        if (export is null)
        {
            return;
        }

        using var reader = new StreamReader(export);
        var rows = GoodreadsCsv.Parse(reader);
        var summary = await Import.ImportAsync(rows, Token);

        Assert.NotEmpty(rows);
        Assert.Equal(rows.Count, summary.Matched + summary.AddedWanted + summary.ForReview + summary.Skipped.Count);
    }

    private static string? RealExport()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            var path = Path.Combine(folder.FullName, "goodreads_library_export.csv");
            if (File.Exists(path))
            {
                using var stream = File.OpenRead(path);
                var start = new byte[7];
                return stream.Read(start) == 7 && System.Text.Encoding.ASCII.GetString(start) == "Book Id" ? path : null;
            }
        }

        return null;
    }

    [Fact]
    public async Task Shelves_BecomeCollections_OnlyWhenTurnedOn()
    {
        var preferences = _library.Services.GetRequiredService<ReaderCompanion.Core.Preferences.IAppPreferences>();
        var row = Row("2", "Dune", "Frank Herbert", ReadStatus.WantToRead) with { Shelves = ["book-club"] };

        await Import.ImportAsync([row], Token);
        Assert.Empty(Collections("Dune"));

        preferences.Save(preferences.Current with { GoodreadsShelvesAsCollections = true });
        await Import.ImportAsync([row], Token);

        Assert.Equal(["book-club"], Collections("Dune"));
        Assert.NotNull(preferences.Current.GoodreadsLastImportUtc);
    }

    [Fact]
    public async Task TwoGoodreadsEntriesForOneBook_TheFurthestStatusWins_WhateverTheOrder()
    {
        // Seen in a real export: "Twelve Months" as an old to-read entry and as a read one.
        AddOwned("Twelve Months", "Jim Butcher", "9780593199350");
        AddOwned("Mort", "Terry Pratchett");

        var summary = await Import.ImportAsync(
            [Row("56143881", "Twelve Months", "Jim  Butcher", ReadStatus.WantToRead),
             Row("230337028", "Twelve Months", "Jim  Butcher", ReadStatus.Read, isbn: "9780593199336", read: new DateOnly(2026, 6, 27)),
             Row("2", "Mort", "Terry Pratchett", ReadStatus.Read, read: new DateOnly(2024, 3, 2)),
             Row("3", "Mort", "Terry Pratchett", ReadStatus.WantToRead)],
            Token);

        Assert.Equal(0, summary.ForReview);
        Assert.Empty(await Import.PendingAsync(Token));
        var twelve = Load("Twelve Months");
        Assert.Equal(ReadStatus.Read, twelve.ReadStatus);
        Assert.Equal(new DateOnly(2026, 6, 27), Assert.Single(twelve.Sessions).FinishedOn);
        var mort = Load("Mort");
        Assert.Equal(ReadStatus.Read, mort.ReadStatus); // the later to-read entry does not move it back
        Assert.Single(mort.Sessions);
    }

    [Fact]
    public async Task AWantedBooksSeries_IsCorrectedByTheNextImport_AndTheWrongCollectionGoes()
    {
        // An earlier import read "(Discworld, #8; City Watch, #1)" as the series "Discworld, #8; City Watch" #1.
        using (var db = _library.NewContext())
        {
            var names = new CatalogNames(db);
            var book = new Book { Title = "Guards! Guards!", GoodreadsId = "1", Series = names.Series("Discworld, #8; City Watch"), SeriesIndex = 1 };
            names.SetAuthors(book, ["Terry Pratchett"]);
            db.Books.Add(book);
            db.SaveChanges();
        }

        await Import.ImportAsync([Row("1", "Guards! Guards!", "Terry Pratchett", ReadStatus.WantToRead, series: "Discworld", index: 8)], Token);

        using var check = _library.NewContext();
        var guards = check.Books.Include(b => b.Series).Single(b => b.Title == "Guards! Guards!");
        Assert.Equal(("Discworld", 8m), (guards.Series!.Name, guards.SeriesIndex!.Value));
        Assert.DoesNotContain(check.Collections, c => c.Name.Contains('#', StringComparison.Ordinal));
    }

    [Fact]
    public async Task ABookLinkedToTheToReadEntry_BecomesRead_WhenTheReadEntryComesFirst()
    {
        // The real export's order: the read entry first, then the to-read entry the book was linked to earlier.
        AddOwned("Twelve Months", "Jim Butcher", "9780593199350");
        await Import.ImportAsync([Row("56143881", "Twelve Months", "Jim Butcher", ReadStatus.WantToRead)], Token);

        await Import.ImportAsync(
            [Row("230337028", "Twelve Months", "Jim Butcher", ReadStatus.Read, isbn: "9780593199336", read: new DateOnly(2026, 6, 27)),
             Row("56143881", "Twelve Months", "Jim Butcher", ReadStatus.WantToRead)],
            Token);

        Assert.Equal(ReadStatus.Read, Load("Twelve Months").ReadStatus);
    }

    private List<string> Collections(string title)
    {
        using var db = _library.NewContext();
        return [.. db.Books.AsNoTracking().Where(b => b.Title == title).SelectMany(b => b.Collections.Select(c => c.Collection.Name))];
    }

    public void Dispose() => _library.Dispose();
}
