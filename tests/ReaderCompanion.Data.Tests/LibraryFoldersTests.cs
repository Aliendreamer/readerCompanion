using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Scanning;
using ReaderCompanion.Core.Tests;

namespace ReaderCompanion.Data.Tests;

public sealed class LibraryFoldersTests : IDisposable
{
    private readonly TempLibrary _library = new();

    private ILibraryFolders Folders => _library.Services.GetRequiredService<ILibraryFolders>();

    private CancellationToken Ct => TestContext.Current.CancellationToken;

    private string NewDir(string relative) => Directory.CreateDirectory(Path.Combine(_library.Root, relative)).FullName;

    [Fact]
    public async Task Link_NewFolder_IsListed()
    {
        var books = NewDir("books");

        var result = await Folders.LinkAsync(books + Path.DirectorySeparatorChar, Ct);

        Assert.True(result.Linked);
        Assert.Equal([books], await Folders.ListAsync(Ct));
    }

    [Fact]
    public async Task Link_SameFolderTwice_IsRefused()
    {
        var books = NewDir("books");
        await Folders.LinkAsync(books, Ct);

        var result = await Folders.LinkAsync(books, Ct);

        Assert.False(result.Linked);
        Assert.Contains("already linked", result.RefusedReason);
        Assert.Single(await Folders.ListAsync(Ct));
    }

    [Fact]
    public async Task Link_FolderInsideLinkedFolder_IsRefused()
    {
        var books = NewDir("books");
        var fantasy = NewDir("books/fantasy");
        await Folders.LinkAsync(books, Ct);

        var result = await Folders.LinkAsync(fantasy, Ct);

        Assert.False(result.Linked);
        Assert.Contains(books, result.RefusedReason);
    }

    [Fact]
    public async Task Link_MissingFolder_IsRefused()
    {
        var result = await Folders.LinkAsync(Path.Combine(_library.Root, "nope"), Ct);

        Assert.False(result.Linked);
        Assert.Contains("not found", result.RefusedReason);
    }

    [Fact]
    public async Task Link_ParentOfLinkedFolder_ReplacesIt_KeepingItsBooks()
    {
        var books = NewDir("books");
        var fantasy = NewDir("books/fantasy");
        using (var epub = TestEpub.Build("<dc:title>Kept</dc:title>"))
        {
            File.WriteAllBytes(Path.Combine(fantasy, "kept.epub"), epub.ToArray());
        }

        await Folders.LinkAsync(fantasy, Ct);
        await _library.Services.GetRequiredService<ILibraryScanner>().ScanAsync(cancellationToken: Ct);

        var result = await Folders.LinkAsync(books, Ct);

        Assert.True(result.Linked);
        Assert.Equal([fantasy], result.Replaced);
        Assert.Equal([books], await Folders.ListAsync(Ct));
        using var db = _library.NewContext();
        Assert.Equal(FileStatus.Present, db.BookFiles.Single().Status);
    }

    [Fact]
    public async Task Unlink_MarksItsFilesMissing_KeepsBooks()
    {
        var books = NewDir("books");
        var other = NewDir("other");
        using var db = _library.NewContext();
        db.Books.Add(BookWithFile("In Books", Path.Combine(books, "a.epub")));
        db.Books.Add(BookWithFile("Elsewhere", Path.Combine(other, "b.epub")));
        db.SaveChanges();
        await Folders.LinkAsync(books, Ct);
        await Folders.LinkAsync(other, Ct);

        await Folders.UnlinkAsync(books, Ct);

        Assert.Equal([other], await Folders.ListAsync(Ct));
        using var check = _library.NewContext();
        var files = check.BookFiles.Include(f => f.Book).ToList();
        Assert.Equal(2, files.Count);
        Assert.Equal(FileStatus.Missing, files.Single(f => f.Book.Title == "In Books").Status);
        Assert.Equal(FileStatus.Present, files.Single(f => f.Book.Title == "Elsewhere").Status);
    }

    [Fact]
    public async Task Unlink_Relink_Scan_Unlink_MarksFilesMissingAgain()
    {
        var books = NewDir("books");
        using (var epub = TestEpub.Build("<dc:title>Round Trip</dc:title>"))
        {
            File.WriteAllBytes(Path.Combine(books, "a.epub"), epub.ToArray());
        }

        using var scope = _library.Services.CreateScope(); // one long-lived scope, like the desktop app
        var folders = scope.ServiceProvider.GetRequiredService<ILibraryFolders>();
        var scanner = _library.Services.GetRequiredService<ILibraryScanner>();
        await folders.LinkAsync(books, Ct);
        await scanner.ScanAsync(cancellationToken: Ct);
        await folders.UnlinkAsync(books, Ct);
        await folders.LinkAsync(books, Ct);
        await scanner.ScanAsync(cancellationToken: Ct);

        await folders.UnlinkAsync(books, Ct);

        using var check = _library.NewContext();
        Assert.Equal(FileStatus.Missing, check.BookFiles.Single().Status);
    }

    [Fact]
    public async Task Link_SameFolderDifferentCase_FollowsPlatform()
    {
        var books = NewDir("books");
        await Folders.LinkAsync(books, Ct);
        var upper = Path.Combine(_library.Root, "BOOKS");
        if (!OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(upper);
        }

        var result = await Folders.LinkAsync(upper, Ct);

        Assert.Equal(!OperatingSystem.IsWindows(), result.Linked);
    }

    private static Book BookWithFile(string title, string path) => new()
    {
        Title = title,
        Files =
        [
            new BookFile
            {
                Path = path,
                ContentHash = title,
                Status = FileStatus.Present,
                LastWriteUtc = DateTime.UtcNow,
                LastSeenUtc = DateTime.UtcNow,
            },
        ],
    };

    public void Dispose() => _library.Dispose();
}
