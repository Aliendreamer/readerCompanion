using Microsoft.EntityFrameworkCore;
using ReaderCompanion.Core.Library;

namespace ReaderCompanion.Data.Tests;

public sealed class LibraryModelTests : IDisposable
{
    private readonly TempLibrary _library = new();

    [Fact]
    public void Book_WithAuthorsGenresSeriesAndFiles_RoundTrips()
    {
        using (var db = _library.NewContext())
        {
            var book = new Book { Title = "Good Omens", Language = "en", Isbn = "9780060853983", SeriesIndex = 1.5m };
            book.Series = new Collection { Name = "Omens", NormalizedName = "OMENS", Kind = CollectionKind.Series };
            book.Authors.Add(new BookAuthor { Order = 0, Author = new Author { Name = "Terry Pratchett", NormalizedName = "TERRY PRATCHETT" } });
            book.Authors.Add(new BookAuthor { Order = 1, Author = new Author { Name = "Neil Gaiman", NormalizedName = "NEIL GAIMAN" } });
            book.Genres.Add(new BookGenre { Genre = new Genre { Name = "Fantasy", NormalizedName = "FANTASY" } });
            book.Genres.Add(new BookGenre { Genre = new Genre { Name = "Humour", NormalizedName = "HUMOUR" } });
            book.Files.Add(TestFiles.File("/books/a.epub", format: BookFormat.Epub, sizeBytes: 10));
            book.Files.Add(TestFiles.File("/books/a.pdf", format: BookFormat.Pdf, sizeBytes: 10));
            db.Books.Add(book);
            db.SaveChanges();
        }

        using (var db = _library.NewContext())
        {
            var book = db.Books
                .Include(b => b.Series)
                .Include(b => b.Authors).ThenInclude(a => a.Author)
                .Include(b => b.Genres).ThenInclude(g => g.Genre)
                .Include(b => b.Files)
                .Single();

            Assert.Equal("Good Omens", book.Title);
            Assert.Equal(1.5m, book.SeriesIndex);
            Assert.Equal("Omens", book.Series!.Name);
            Assert.Equal(["Terry Pratchett", "Neil Gaiman"], book.Authors.OrderBy(a => a.Order).Select(a => a.Author.Name));
            Assert.Equal(["Fantasy", "Humour"], book.Genres.Select(g => g.Genre.Name).Order());
            Assert.Equal(["/books/a.epub", "/books/a.pdf"], book.Files.Select(f => f.Path).Order());
        }
    }

    [Fact]
    public void DuplicateAuthorNormalizedName_Throws()
    {
        using var db = _library.NewContext();
        db.Authors.Add(new Author { Name = "Le Guin", NormalizedName = "LE GUIN" });
        db.Authors.Add(new Author { Name = "le guin", NormalizedName = "LE GUIN" });

        Assert.Throws<DbUpdateException>(() => db.SaveChanges());
    }

    public void Dispose() => _library.Dispose();
}
