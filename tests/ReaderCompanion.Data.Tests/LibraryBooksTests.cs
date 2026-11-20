using Microsoft.Extensions.DependencyInjection;
using ReaderCompanion.Core.Library;

namespace ReaderCompanion.Data.Tests;

public sealed class LibraryBooksTests : IDisposable
{
    private readonly TempLibrary _library = new();

    [Fact]
    public async Task List_GivesTitleOrderedBooks_WithOrderedAuthors_Series_AndMissingFlag()
    {
        using (var db = _library.NewContext())
        {
            var omens = new Book { Title = "Good Omens", SeriesIndex = 2 };
            omens.Series = new Collection { Name = "Omens", NormalizedName = "OMENS", Kind = CollectionKind.Series };
            omens.Authors.Add(new BookAuthor { Order = 1, Author = new Author { Name = "Neil Gaiman", NormalizedName = "NG" } });
            omens.Authors.Add(new BookAuthor { Order = 0, Author = new Author { Name = "Terry Pratchett", NormalizedName = "TP" } });
            omens.Files.Add(File("/a.epub", FileStatus.Missing));
            var anon = new Book { Title = "Anonymous" };
            anon.Files.Add(File("/b.epub", FileStatus.Missing));
            anon.Files.Add(File("/c.epub", FileStatus.Present));
            db.Books.AddRange(omens, anon);
            db.SaveChanges();
        }

        var books = await _library.Services.GetRequiredService<ILibraryBooks>().ListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["Anonymous", "Good Omens"], books.Select(b => b.Title));
        Assert.Empty(books[0].Authors);
        Assert.False(books[0].FileMissing);
        Assert.Equal(["Terry Pratchett", "Neil Gaiman"], books[1].Authors);
        Assert.Equal("Omens", books[1].Series);
        Assert.Equal(2m, books[1].SeriesIndex);
        Assert.True(books[1].FileMissing);
    }

    [Fact]
    public async Task BookWithNoFiles_IsShownAsMissing()
    {
        using (var db = _library.NewContext())
        {
            db.Books.Add(new Book { Title = "Fileless" });
            db.SaveChanges();
        }

        var books = await _library.Services.GetRequiredService<ILibraryBooks>().ListAsync(TestContext.Current.CancellationToken);

        Assert.True(Assert.Single(books).FileMissing);
    }

    [Fact]
    public async Task List_GivesEachBooksCoverFileName()
    {
        using (var db = _library.NewContext())
        {
            db.Books.AddRange(new Book { Title = "Covered", CoverFileName = "abc.jpg" }, new Book { Title = "Plain" });
            db.SaveChanges();
        }

        var books = await _library.Services.GetRequiredService<ILibraryBooks>().ListAsync(TestContext.Current.CancellationToken);

        Assert.Equal("abc.jpg", books.Single(b => b.Title == "Covered").CoverFileName);
        Assert.Null(books.Single(b => b.Title == "Plain").CoverFileName);
    }

    [Fact]
    public async Task List_GivesGenres_DistinctFormats_AndDateAdded()
    {
        var added = new DateTime(2025, 6, 7, 8, 9, 10, DateTimeKind.Utc);
        using (var db = _library.NewContext())
        {
            var dune = new Book { Title = "Dune", DateAddedUtc = added };
            dune.Genres.Add(new BookGenre { Genre = new Genre { Name = "Science Fiction", NormalizedName = "SF" } });
            dune.Genres.Add(new BookGenre { Genre = new Genre { Name = "Adventure", NormalizedName = "ADV" } });
            dune.Files.Add(File("/dune.epub", FileStatus.Present));
            dune.Files.Add(File("/copy/dune.epub", FileStatus.Present));
            dune.Files.Add(File("/dune.pdf", FileStatus.Present, BookFormat.Pdf));
            db.Books.Add(dune);
            db.SaveChanges();
        }

        var book = Assert.Single(await _library.Services.GetRequiredService<ILibraryBooks>().ListAsync(TestContext.Current.CancellationToken));

        Assert.Equal(["Adventure", "Science Fiction"], book.Genres);
        Assert.Equal([BookFormat.Epub, BookFormat.Pdf], book.Formats);
        Assert.Equal(added, book.DateAddedUtc);
    }

    private static BookFile File(string path, FileStatus status, BookFormat format = BookFormat.Epub) => new()
    {
        Path = path,
        Format = format,
        ContentHash = path,
        Status = status,
        LastWriteUtc = DateTime.UtcNow,
        LastSeenUtc = DateTime.UtcNow,
    };

    public void Dispose() => _library.Dispose();
}
