using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Scanning;
using ReaderCompanion.Core.Tests;

namespace ReaderCompanion.Data.Tests;

public sealed class UserCollectionsTests : IDisposable
{
    private readonly TempLibrary _library = new();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private ILibraryBooks Books => _library.Services.CreateScope().ServiceProvider.GetRequiredService<ILibraryBooks>();

    private int Add(string title)
    {
        using var db = _library.NewContext();
        var book = new Book { Title = title };
        book.Files.Add(TestFiles.File($"/b/{title}.epub"));
        db.Books.Add(book);
        db.SaveChanges();
        return book.Id;
    }

    private List<Collection> UserCollections()
    {
        using var db = _library.NewContext();
        return [.. db.Collections.AsNoTracking().Where(c => c.Kind == CollectionKind.User)];
    }

    [Fact]
    public async Task Add_CreatesTheCollection_AndListAndDetailsShowIt()
    {
        var mort = Add("Mort");

        Assert.True(await Books.AddToCollectionAsync(mort, "  Book  club ", Token));

        Assert.Equal("Book club", Assert.Single(UserCollections()).Name);
        Assert.Equal(["Book club"], (await Books.GetAsync(mort, Token))!.Book.Collections);
        Assert.Equal(["Book club"], Assert.Single(await Books.ListAsync(Token)).Collections);
    }

    [Fact]
    public async Task Add_ReusesACollectionIgnoringCase_AndNeverLinksTwice()
    {
        var mort = Add("Mort");
        var dune = Add("Dune");

        await Books.AddToCollectionAsync(mort, "Book club", Token);
        await Books.AddToCollectionAsync(dune, "book CLUB", Token);
        await Books.AddToCollectionAsync(dune, "Book club", Token);

        Assert.Single(UserCollections());
        Assert.Equal(["Book club"], (await Books.GetAsync(dune, Token))!.Book.Collections);
    }

    [Fact]
    public async Task UserCollection_AndSeries_CanShareAName()
    {
        var mort = Add("Mort");
        using (var db = _library.NewContext())
        {
            db.Books.Single().Series = new Collection { Name = "Discworld", NormalizedName = "DISCWORLD", Kind = CollectionKind.Series };
            db.SaveChanges();
        }

        await Books.AddToCollectionAsync(mort, "Discworld", Token);

        var details = (await Books.GetAsync(mort, Token))!;
        Assert.Equal("Discworld", details.Book.Series);
        Assert.Equal(["Discworld"], details.Book.Collections);
    }

    [Fact]
    public async Task Remove_Unlinks_AndDeletesAnEmptyCollection()
    {
        var mort = Add("Mort");
        var dune = Add("Dune");
        await Books.AddToCollectionAsync(mort, "Book club", Token);
        await Books.AddToCollectionAsync(dune, "Book club", Token);

        Assert.True(await Books.RemoveFromCollectionAsync(mort, "book club", Token));
        Assert.Single(UserCollections());
        await Books.RemoveFromCollectionAsync(dune, "Book club", Token);

        Assert.Empty(UserCollections());
        Assert.Empty((await Books.GetAsync(dune, Token))!.Book.Collections);
    }

    [Fact]
    public async Task UnknownBook_GivesFalse_BlankName_IsRejected()
    {
        Assert.False(await Books.AddToCollectionAsync(999, "X", Token));
        Assert.False(await Books.RemoveFromCollectionAsync(999, "X", Token));
        await Assert.ThrowsAsync<ArgumentException>(() => Books.AddToCollectionAsync(Add("Mort"), "  ", Token));
    }

    [Fact]
    public async Task Rescan_KeepsCollections()
    {
        var folder = Path.Combine(_library.Root, "books");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "a.epub");
        using (var epub = TestEpub.Build("<dc:title>First</dc:title>"))
        {
            File.WriteAllBytes(path, epub.ToArray());
        }

        using (var db = _library.NewContext())
        {
            db.LibraryFolders.Add(new LibraryFolder { Path = folder, AddedUtc = DateTime.UtcNow });
            db.SaveChanges();
        }

        var scanner = _library.Services.GetRequiredService<ILibraryScanner>();
        await scanner.ScanAsync(cancellationToken: Token);
        var id = Assert.Single(await Books.ListAsync(Token)).Id;
        await Books.AddToCollectionAsync(id, "Favourites", Token);

        using (var epub = TestEpub.Build("<dc:title>Second</dc:title>"))
        {
            File.WriteAllBytes(path, epub.ToArray());
        }

        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(1));
        await scanner.ScanAsync(cancellationToken: Token);

        var book = Assert.Single(await Books.ListAsync(Token));
        Assert.Equal("Second", book.Title);
        Assert.Equal(["Favourites"], book.Collections);
    }

    public void Dispose() => _library.Dispose();
}
