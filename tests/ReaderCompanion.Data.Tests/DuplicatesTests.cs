using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReaderCompanion.Core.Library;

namespace ReaderCompanion.Data.Tests;

public sealed class DuplicatesTests : IDisposable
{
    private readonly TempLibrary _library = new();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private IDuplicates Duplicates => _library.Services.CreateScope().ServiceProvider.GetRequiredService<IDuplicates>();

    private int Add(string title, string? author, string path, BookFormat format = BookFormat.Epub,
        string[]? genres = null, DateTime? added = null, string? cover = null, string? isbn = null)
    {
        using var db = _library.NewContext();
        var names = new CatalogNames(db);
        var book = new Book { Title = title, Isbn = isbn, CoverFileName = cover, DateAddedUtc = added ?? DateTime.UtcNow };
        names.SetAuthors(book, author is null ? [] : [author]);
        names.SetGenres(book, genres ?? []);
        book.Files.Add(TestFiles.File(path, format: format));
        db.Books.Add(book);
        db.SaveChanges();
        return book.Id;
    }

    private List<Book> Books()
    {
        using var db = _library.NewContext();
        return db.Books.AsNoTracking()
            .Include(b => b.Files)
            .Include(b => b.Genres).ThenInclude(g => g.Genre)
            .Include(b => b.Authors).ThenInclude(a => a.Author)
            .OrderBy(b => b.Id)
            .ToList();
    }

    [Fact]
    public async Task Find_GroupsLikelyDuplicates_WithTheirDetails()
    {
        var epub = Add("Mort", "Terry Pratchett", "/b/mort.epub");
        var pdf = Add("MORT", "terry pratchett", "/b/mort.pdf", BookFormat.Pdf);
        Add("Dune", "Frank Herbert", "/b/dune.epub");

        var group = Assert.Single(await Duplicates.FindAsync(Token));

        Assert.Equal([epub, pdf], group.Books.Select(b => b.Book.Id));
        Assert.Equal("/b/mort.pdf", Assert.Single(group.Books[1].Files).Path);
    }

    [Fact]
    public async Task Dismiss_HidesTheGroup_AfterARestart()
    {
        var a = Add("Mort", "Terry Pratchett", "/b/a.epub");
        var b = Add("Mort", "Terry Pratchett", "/b/b.epub");

        await Duplicates.DismissAsync([b, a], Token);

        using var restarted = new ServiceCollection().AddLibraryData(_library.DatabasePath).BuildServiceProvider();
        Assert.Empty(await restarted.CreateScope().ServiceProvider.GetRequiredService<IDuplicates>().FindAsync(Token));
    }

    [Fact]
    public async Task Merge_MovesFiles_CombinesGenres_KeepsEarliestDateAndACover()
    {
        var early = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var keep = Add("Mort", "Terry Pratchett", "/b/mort.epub", genres: ["Fantasy"], added: new DateTime(2025, 1, 1));
        var other = Add("MORT", "Pratchett", "/b/mort.pdf", BookFormat.Pdf, ["Humour", "fantasy"], early, cover: "c.jpg");

        Assert.True(await Duplicates.MergeAsync(keep, [other], Token));

        var book = Assert.Single(Books());
        Assert.Equal(keep, book.Id);
        Assert.Equal("Mort", book.Title);
        Assert.Equal(["Terry Pratchett"], book.Authors.Select(a => a.Author.Name));
        Assert.Equal(["/b/mort.epub", "/b/mort.pdf"], book.Files.Select(f => f.Path).Order());
        Assert.Equal(["Fantasy", "Humour"], book.Genres.Select(g => g.Genre.Name).Order());
        Assert.Equal(early, book.DateAddedUtc);
        Assert.Equal("c.jpg", book.CoverFileName);
    }

    [Fact]
    public async Task Merge_UnknownBook_ChangesNothing()
    {
        var keep = Add("Mort", "Terry Pratchett", "/b/a.epub");
        var other = Add("Mort", "Terry Pratchett", "/b/b.epub");

        Assert.False(await Duplicates.MergeAsync(keep, [other, 999], Token));

        Assert.Equal(2, Books().Count);
    }

    [Fact]
    public async Task Merge_RepointsDismissalsOfTheMergedBooks()
    {
        var a = Add("Mort", "Terry Pratchett", "/b/a.epub");
        var b = Add("Mort", "Terry Pratchett", "/b/b.epub");
        var c = Add("Mort", "Terry Pratchett", "/b/c.epub");
        await Duplicates.DismissAsync([b, c], Token);

        await Duplicates.MergeAsync(a, [b], Token);

        using var db = _library.NewContext();
        Assert.Equal([(a, c)], db.DismissedDuplicates.AsEnumerable().Select(p => (p.BookIdA, p.BookIdB)));
    }

    [Fact]
    public async Task Merge_CarriesNotDuplicateAnswersOverToTheKeptBook()
    {
        var keep = Add("Mort", "Terry Pratchett", "/b/a.epub");
        var other = Add("Mort", "Terry Pratchett", "/b/b.epub");
        var third = Add("Mort", "Terry Pratchett", "/b/c.epub");
        await Duplicates.DismissAsync([other, third], Token);

        await Duplicates.MergeAsync(keep, [other], Token);

        Assert.Empty(await Duplicates.FindAsync(Token));
    }

    [Fact]
    public async Task Dismiss_IgnoresBooksThatAreGone()
    {
        var a = Add("Mort", "Terry Pratchett", "/b/a.epub");
        var b = Add("Mort", "Terry Pratchett", "/b/b.epub");

        await Duplicates.DismissAsync([a, b, 999], Token);

        using var db = _library.NewContext();
        Assert.Equal([(a, b)], db.DismissedDuplicates.AsEnumerable().Select(p => (p.BookIdA, p.BookIdB)));
    }

    [Fact]
    public async Task Merge_MarksTheKeptBookEdited()
    {
        var keep = Add("Mort", "Terry Pratchett", "/b/a.epub");
        var other = Add("Mort", "Terry Pratchett", "/b/b.pdf", BookFormat.Pdf);

        await Duplicates.MergeAsync(keep, [other], Token);

        Assert.NotNull(Assert.Single(Books()).EditedUtc);
    }

    public void Dispose() => _library.Dispose();
}
