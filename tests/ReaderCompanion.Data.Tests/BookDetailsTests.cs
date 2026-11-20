using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReaderCompanion.Core.Library;

namespace ReaderCompanion.Data.Tests;

public sealed class BookDetailsTests : IDisposable
{
    private readonly TempLibrary _library = new();

    private ILibraryBooks Books => _library.Services.CreateScope().ServiceProvider.GetRequiredService<ILibraryBooks>();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private int AddMort()
    {
        using var db = _library.NewContext();
        var mort = new Book { Title = "Mort", Language = "en", Isbn = "978-0552131063", SeriesIndex = 4 };
        mort.Series = new Collection { Name = "Discworld", NormalizedName = "DISCWORLD", Kind = CollectionKind.Series };
        mort.Authors.Add(new BookAuthor { Author = new Author { Name = "Terry Pratchett", NormalizedName = "TERRY PRATCHETT" } });
        mort.Genres.Add(new BookGenre { Genre = new Genre { Name = "Fantasy", NormalizedName = "FANTASY" } });
        mort.Files.Add(TestFiles.File("/books/mort.epub"));
        mort.Files.Add(TestFiles.File("/old/mort.pdf", FileStatus.Missing, BookFormat.Pdf));
        db.Books.Add(mort);
        db.SaveChanges();
        return mort.Id;
    }

    private Book Load(int id)
    {
        using var db = _library.NewContext();
        return db.Books.AsNoTracking()
            .Include(b => b.Series)
            .Include(b => b.Authors).ThenInclude(a => a.Author)
            .Include(b => b.Genres).ThenInclude(g => g.Genre)
            .Single(b => b.Id == id);
    }

    private static BookEdit Edit(
        string title = "Mort",
        string[]? authors = null,
        string? series = "Discworld",
        decimal? index = 4,
        string[]? genres = null) =>
        new(title, authors ?? ["Terry Pratchett"], series, index, genres ?? ["Fantasy"], "en", "978-0552131063");

    // ---------- 1.3 ----------

    [Fact]
    public async Task Get_GivesDetailsAndFiles()
    {
        var id = AddMort();

        var details = await Books.GetAsync(id, Token);

        Assert.NotNull(details);
        Assert.Equal("Mort", details.Book.Title);
        Assert.Equal(["Terry Pratchett"], details.Book.Authors);
        Assert.Equal("Discworld", details.Book.Series);
        Assert.Equal(["Fantasy"], details.Book.Genres);
        Assert.Equal("en", details.Language);
        Assert.Equal("978-0552131063", details.Isbn);
        Assert.False(details.Edited);
        Assert.Equal(
            [new BookFileInfo("/books/mort.epub", BookFormat.Epub, 1234, Present: true),
             new BookFileInfo("/old/mort.pdf", BookFormat.Pdf, 1234, Present: false)],
            details.Files);
    }

    [Fact]
    public async Task GetMany_GivesKnownBooksInIdOrder()
    {
        var id = AddMort();

        var many = await Books.GetManyAsync([999, id], Token);

        Assert.Equal([id], many.Select(d => d.Book.Id));
        Assert.Equal(2, many[0].Files.Count);
    }

    [Fact]
    public async Task Get_UnknownBook_GivesNull()
    {
        Assert.Null(await Books.GetAsync(999, Token));
    }

    // ---------- 1.4 ----------

    [Fact]
    public async Task Update_SavesFields_AndMarksTheBookEdited()
    {
        var id = AddMort();

        await Books.UpdateAsync(id, new BookEdit("  Mort  ", ["Terry Pratchett", "Neil Gaiman"], "Discworld", 4.5m,
            ["Fantasy", "Humour"], "en-GB", "123"), Token);

        var book = Load(id);
        Assert.Equal("Mort", book.Title);
        Assert.Equal(["Terry Pratchett", "Neil Gaiman"], book.Authors.OrderBy(a => a.Order).Select(a => a.Author.Name));
        Assert.Equal(["Fantasy", "Humour"], book.Genres.Select(g => g.Genre.Name).Order());
        Assert.Equal(4.5m, book.SeriesIndex);
        Assert.Equal("en-GB", book.Language);
        Assert.Equal("123", book.Isbn);
        Assert.NotNull(book.EditedUtc);
        Assert.True((await Books.GetAsync(id, Token))!.Edited);
    }

    [Fact]
    public async Task Update_MatchesExistingAuthor_IgnoringCaseAndSpaces()
    {
        var id = AddMort();
        int other;
        using (var db = _library.NewContext())
        {
            var book = new Book { Title = "Guards! Guards!" };
            db.Books.Add(book);
            db.SaveChanges();
            other = book.Id;
        }

        await Books.UpdateAsync(other, Edit("Guards! Guards!", ["terry  pratchett"], series: null, index: null), Token);

        using var check = _library.NewContext();
        Assert.Equal(1, check.Authors.Count());
        Assert.Equal("Terry Pratchett", Load(other).Authors.Single().Author.Name);
        Assert.Equal(id, check.Books.Single(b => b.Title == "Mort").Id);
    }

    [Fact]
    public async Task Update_BlankSeries_ClearsSeriesAndNumber()
    {
        var id = AddMort();

        await Books.UpdateAsync(id, Edit(series: "  ", index: 4), Token);

        var book = Load(id);
        Assert.Null(book.Series);
        Assert.Null(book.SeriesIndex);
    }

    [Fact]
    public async Task Update_BlankAuthorsAndRepeats_AreDropped()
    {
        var id = AddMort();

        await Books.UpdateAsync(id, Edit(authors: ["", "Neil Gaiman", "neil gaiman"], genres: [" "]), Token);

        var book = Load(id);
        Assert.Equal(["Neil Gaiman"], book.Authors.Select(a => a.Author.Name));
        Assert.Empty(book.Genres);
    }

    [Fact]
    public async Task Update_EmptyTitle_IsRejected()
    {
        var id = AddMort();

        await Assert.ThrowsAsync<ArgumentException>(() => Books.UpdateAsync(id, Edit(title: "  "), Token));
        Assert.Equal("Mort", Load(id).Title);
    }

    [Fact]
    public async Task Update_UnknownBook_GivesFalse()
    {
        Assert.False(await Books.UpdateAsync(999, Edit(), Token));
    }

    public void Dispose() => _library.Dispose();
}
