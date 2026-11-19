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

    private static BookFile File(string path, FileStatus status) => new()
    {
        Path = path,
        ContentHash = path,
        Status = status,
        LastWriteUtc = DateTime.UtcNow,
        LastSeenUtc = DateTime.UtcNow,
    };

    public void Dispose() => _library.Dispose();
}
