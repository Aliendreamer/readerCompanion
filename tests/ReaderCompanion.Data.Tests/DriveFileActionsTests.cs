using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReaderCompanion.Core.Drive;
using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Organising;

namespace ReaderCompanion.Data.Tests;

/// <summary>Trash and organise on Google Drive (an in-memory Drive linked with read and write access).</summary>
public sealed class DriveFileActionsTests : IDisposable
{
    private readonly TempLibrary _library = new();
    private readonly string _books;

    public DriveFileActionsTests() => _books = Drive.Folder("Books");

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private FakeDrive Drive => _library.Drive;

    private string Root => DrivePath.Make(_books, "Books");

    private ILibraryBooks Books => _library.Services.CreateScope().ServiceProvider.GetRequiredService<ILibraryBooks>();

    private IOrganiser Organiser => _library.Services.CreateScope().ServiceProvider.GetRequiredService<IOrganiser>();

    /// <summary>A book with a file on Drive at <paramref name="name"/> inside <paramref name="parent"/>; returns the
    /// Drive id.</summary>
    private string AddDriveBook(string name, string title, string parent, string? author = null, string? genre = null, string? localCopy = null)
    {
        var id = Drive.File(name, parent, [1, 2, 3]);
        using var db = _library.NewContext();
        var names = new CatalogNames(db);
        var book = new Book { Title = title };
        names.SetAuthors(book, author is null ? [] : [author]);
        names.SetGenres(book, genre is null ? [] : [genre]);
        var file = TestFiles.File(DrivePath.Make(id, Drive.PathOf(id)));
        file.DriveFileId = id;
        book.Files.Add(file);
        if (localCopy is not null)
        {
            book.Files.Add(TestFiles.File(localCopy));
        }

        db.Books.Add(book);
        db.SaveChanges();
        return id;
    }

    private BookFile FileOf(string driveId)
    {
        using var db = _library.NewContext();
        return db.BookFiles.AsNoTracking().Single(f => f.DriveFileId == driveId);
    }

    private int FileIdOf(string driveId) => FileOf(driveId).Id;

    // ---------- trash ----------

    [Fact]
    public async Task Trash_ADriveCopy_GoesToDrivesTrash_AndTheBookKeepsItsOtherFile()
    {
        var id = AddDriveBook("a.epub", "Alpha", _books, localCopy: "/books/a.epub");

        Assert.Equal(TrashResult.Trashed, await Books.TrashFileAsync(FileIdOf(id), Token));

        Assert.True(Drive.Items[id].Trashed);
        Assert.Empty(_library.Trash.Trashed);
        using var db = _library.NewContext();
        Assert.Equal("/books/a.epub", db.BookFiles.Single().Path);
    }

    [Fact]
    public async Task Trash_WithReadOnlyAccess_IsRefused()
    {
        var id = AddDriveBook("a.epub", "Alpha", _books, localCopy: "/books/a.epub");
        _library.DriveAccount.Link = new DriveLink(DriveAccess.WholeDriveReadOnly, null, DateTime.UtcNow);

        Assert.Equal(TrashResult.ReadOnly, await Books.TrashFileAsync(FileIdOf(id), Token));

        Assert.False(Drive.Items[id].Trashed);
        Assert.NotNull(FileOf(id));
    }

    [Fact]
    public async Task Trash_RefusedByDrive_ChangesNothing()
    {
        var id = AddDriveBook("a.epub", "Alpha", _books, localCopy: "/books/a.epub");
        Drive.AllowsWrites = false;

        Assert.Equal(TrashResult.TrashFailed, await Books.TrashFileAsync(FileIdOf(id), Token));

        Assert.NotNull(FileOf(id));
    }

    // ---------- organise ----------

    [Fact]
    public async Task Preview_PlansTheDriveFolder()
    {
        AddDriveBook("hobbit_final.epub", "The Hobbit", _books, "J.R.R. Tolkien", "Fantasy");
        AddDriveBook("elsewhere.epub", "Dune", Drive.Folder("Other"), "Frank Herbert", "SF");

        var move = Assert.Single(await Organiser.PreviewAsync(Root, OrganiseLayout.GenreAuthorTitle, Token));

        Assert.Equal("Books/Fantasy/Tolkien, J.R.R/The Hobbit.epub", DrivePath.Display(move.To));
    }

    [Fact]
    public async Task Apply_MovesOnDrive_CreatingFolders_TrashingEmptiedOnes_AndUndoPutsItBack()
    {
        var old = Drive.Folder("Old", _books);
        var id = AddDriveBook("hobbit_final.epub", "The Hobbit", old, "J.R.R. Tolkien", "Fantasy");
        var moves = await Organiser.PreviewAsync(Root, OrganiseLayout.GenreAuthorTitle, Token);

        var result = await Organiser.ApplyAsync(Root, moves, Token);

        Assert.Equal((1, 0), (result.Moved, result.Failed.Count));
        Assert.Equal("Books/Fantasy/Tolkien, J.R.R/The Hobbit.epub", Drive.PathOf(id));
        Assert.Equal("Books/Fantasy/Tolkien, J.R.R/The Hobbit.epub", DrivePath.Display(FileOf(id).Path));
        Assert.Equal(["Fantasy", "Tolkien, J.R.R"], Drive.CreatedFolders);
        Assert.True(Drive.Items[old].Trashed);

        var undone = await Organiser.UndoLastAsync(Token);

        Assert.Equal(1, undone.Moved);
        Assert.Equal("Books/Old/hobbit_final.epub", Drive.PathOf(id));
        Assert.Equal("Books/Old/hobbit_final.epub", DrivePath.Display(FileOf(id).Path));
        Assert.Null(await Organiser.LastAsync(Token));
    }

    [Fact]
    public async Task Preview_LeavesOutAnotherDriveFolderWithTheSameName()
    {
        AddDriveBook("hobbit_final.epub", "The Hobbit", _books, "J.R.R. Tolkien", "Fantasy");
        var otherBooks = Drive.Folder("Books"); // a second "Books" in My Drive
        var other = AddDriveBook("dune.epub", "Dune", otherBooks, "Frank Herbert", "SF");

        var moves = await Organiser.PreviewAsync(Root, OrganiseLayout.GenreAuthorTitle, Token);

        Assert.DoesNotContain(moves, m => DrivePath.Id(m.From) == other);
        Assert.Single(moves);
    }

    [Fact]
    public async Task Preview_ANameTakenOnDrive_GetsANumber()
    {
        AddDriveBook("hobbit_final.epub", "The Hobbit", _books, "J.R.R. Tolkien", "Fantasy");
        Drive.File("The Hobbit.epub", Drive.Folder("Tolkien, J.R.R", _books), [9]); // not in the library

        var move = Assert.Single(await Organiser.PreviewAsync(Root, OrganiseLayout.AuthorTitle, Token));

        Assert.Equal("Books/Tolkien, J.R.R/The Hobbit (2).epub", DrivePath.Display(move.To));
    }

    [Fact]
    public async Task Apply_ATakenName_IsReportedAndNotMoved()
    {
        var id = AddDriveBook("hobbit_final.epub", "The Hobbit", _books, "J.R.R. Tolkien", "Fantasy");
        var moves = await Organiser.PreviewAsync(Root, OrganiseLayout.AuthorTitle, Token);
        Drive.File("The Hobbit.epub", Drive.Folder("Tolkien, J.R.R", _books), [9]); // not in the library

        var result = await Organiser.ApplyAsync(Root, moves, Token);

        Assert.Equal("Another file already has that name.", Assert.Single(result.Failed).Reason);
        Assert.Equal("Books/hobbit_final.epub", Drive.PathOf(id));
    }

    [Fact]
    public async Task Apply_WithReadOnlyAccess_IsRefused()
    {
        var id = AddDriveBook("hobbit_final.epub", "The Hobbit", _books, "J.R.R. Tolkien", "Fantasy");
        var moves = await Organiser.PreviewAsync(Root, OrganiseLayout.AuthorTitle, Token);
        _library.DriveAccount.Link = new DriveLink(DriveAccess.WholeDriveReadOnly, null, DateTime.UtcNow);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Organiser.ApplyAsync(Root, moves, Token));

        Assert.Equal("Books/hobbit_final.epub", Drive.PathOf(id));
    }

    public void Dispose() => _library.Dispose();
}
