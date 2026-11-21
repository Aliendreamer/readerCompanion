using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReaderCompanion.Core.Drive;
using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Tests;

namespace ReaderCompanion.Data.Tests;

public sealed class DriveLibraryTests : IDisposable
{
    private readonly TempLibrary _library = new();
    private readonly FakeDrive _drive = new();
    private readonly TestDriveAccount _account = new();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private DriveLibrary Drive => new(
        _library.Services.GetRequiredService<IDbContextFactory<LibraryDbContext>>(),
        _library.Services.GetRequiredService<LibraryDataPaths>(),
        _library.Services.GetRequiredService<LibraryWriteGate>(),
        _account,
        new GoogleDriveFiles(_account, _drive.Client()));

    private static byte[] Epub(string title) => TestEpub.Build($"<dc:title>{title}</dc:title>").ToArray();

    private List<Book> Books()
    {
        using var db = _library.NewContext();
        return [.. db.Books.Include(b => b.Files).AsNoTracking().OrderBy(b => b.Title)];
    }

    private async Task<string> AddBooksFolderAsync()
    {
        var books = _drive.Folder("Books");
        await Drive.AddFolderAsync(books, "Books", Token);
        return books;
    }

    [Fact]
    public async Task Folders_AreBrowsed_AndAdded()
    {
        var books = _drive.Folder("Books");
        _drive.Folder("Fantasy", books);
        _drive.File("loose.epub", "root", Epub("Loose"));

        Assert.Equal(["Books"], (await Drive.ListFoldersAsync(null, Token)).Select(f => f.Name));
        var fantasy = Assert.Single(await Drive.ListFoldersAsync(books, Token));
        var result = await Drive.AddFolderAsync(fantasy.Id, "Books/Fantasy", Token);

        Assert.True(result.Linked);
        var source = Assert.Single(await Drive.ListSourcesAsync(Token));
        Assert.Equal(("Books/Fantasy", DriveSourceKind.Folder), (source.Name, source.Kind));
    }

    [Fact]
    public async Task AFolderAddedTwice_OrInsideAnother_IsRefused_AndAParentReplacesItsChildren()
    {
        var books = _drive.Folder("Books");
        var fantasy = _drive.Folder("Fantasy", books);
        await Drive.AddFolderAsync(fantasy, "Books/Fantasy", Token);

        Assert.False((await Drive.AddFolderAsync(fantasy, "Books/Fantasy", Token)).Linked);
        var parent = await Drive.AddFolderAsync(books, "Books", Token);
        Assert.Equal(["Books/Fantasy"], parent.Replaced);
        var inside = await Drive.AddFolderAsync(fantasy, "Books/Fantasy", Token);
        Assert.Contains("Books", inside.RefusedReason, StringComparison.Ordinal);
        Assert.Equal(["Books"], (await Drive.ListSourcesAsync(Token)).Select(s => s.Name));
    }

    [Fact]
    public async Task PickedFiles_AreAddedOnce_WithTheirNames()
    {
        var a = _drive.File("A.epub", "root", Epub("A"));
        _account.Picks.AddRange([a, a]);

        Assert.Equal(1, await Drive.AddPickedFilesAsync(Token));
        Assert.Equal(0, await Drive.AddPickedFilesAsync(Token));

        var source = Assert.Single(await Drive.ListSourcesAsync(Token));
        Assert.Equal(("A.epub", DriveSourceKind.File, a), (source.Name, source.Kind, source.DriveId));
    }

    [Fact]
    public async Task Scan_AddsTheBooksInFoldersAndSubfolders_ReadingEachOnce()
    {
        var books = await AddBooksFolderAsync();
        var sub = _drive.Folder("Sub", books);
        var a = _drive.File("A.epub", books, Epub("Alpha"));
        _drive.File("B.epub", sub, Epub("Beta"));
        _drive.File("notes.txt", books, [1, 2]);

        var summary = await Drive.ScanAsync(cancellationToken: Token);

        Assert.Equal(2, summary.Added);
        Assert.Equal(["Alpha", "Beta"], Books().Select(b => b.Title));
        var file = Books()[0].Files.Single();
        Assert.Equal((a, "Books/A.epub"), (file.DriveFileId, DrivePath.Display(file.Path)));
        Assert.Equal(2, _drive.Downloads.Count);

        var again = await Drive.ScanAsync(cancellationToken: Token);

        Assert.Equal((0, 0, 0, 0), (again.Added, again.Moved, again.Updated, again.MarkedMissing));
        Assert.Equal(2, _drive.Downloads.Count); // nothing read again
    }

    [Fact]
    public async Task Scan_FollowsRenames_AndMarksDeletedFilesMissing()
    {
        var books = await AddBooksFolderAsync();
        var a = _drive.File("A.epub", books, Epub("Alpha"));
        var b = _drive.File("B.epub", books, Epub("Beta"));
        await Drive.ScanAsync(cancellationToken: Token);

        _drive.Rename(a, "Alpha.epub");
        _drive.Delete(b);
        var summary = await Drive.ScanAsync(cancellationToken: Token);

        Assert.Equal((1, 1), (summary.Moved, summary.MarkedMissing));
        var (alpha, beta) = (Books()[0].Files.Single(), Books()[1].Files.Single());
        Assert.Equal("Books/Alpha.epub", DrivePath.Display(alpha.Path));
        Assert.Equal(FileStatus.Missing, beta.Status);
        Assert.Equal(2, _drive.Downloads.Count);
    }

    [Fact]
    public async Task Scan_ADriveCopyOfALocalBook_JoinsIt()
    {
        var content = Epub("Alpha");
        using (var db = _library.NewContext())
        {
            var local = TestFiles.File("/books/alpha.epub");
            local.ContentHash = FakeDrive.Sha256(content);
            db.Books.Add(new Book { Title = "Alpha", Files = [local] });
            db.SaveChanges();
        }

        var books = await AddBooksFolderAsync();
        _drive.File("A.epub", books, content);

        var summary = await Drive.ScanAsync(cancellationToken: Token);

        Assert.Equal(0, summary.Added);
        var book = Assert.Single(Books());
        Assert.Equal(2, book.Files.Count);
        Assert.Empty(_drive.Downloads);
    }

    [Fact]
    public async Task Scan_WithoutDriveChecksums_DownloadsToHash()
    {
        _drive.GivesChecksums = false;
        var books = await AddBooksFolderAsync();
        var content = Epub("Alpha");
        _drive.File("A.epub", books, content);

        await Drive.ScanAsync(cancellationToken: Token);

        Assert.Equal(FakeDrive.Sha256(content), Books().Single().Files.Single().ContentHash);
        Assert.Equal(2, _drive.Downloads.Count); // once to hash (deleted at once), once to read
    }

    [Fact]
    public async Task Scan_PickedFiles_AreFollowed_AndMissingOnceGone()
    {
        var a = _drive.File("A.epub", "root", Epub("Alpha"));
        _account.Picks.Add(a);
        await Drive.AddPickedFilesAsync(Token);
        await Drive.ScanAsync(cancellationToken: Token);
        Assert.Equal("A.epub", DrivePath.Display(Books().Single().Files.Single().Path));

        _drive.Delete(a);
        var summary = await Drive.ScanAsync(cancellationToken: Token);

        Assert.Equal(1, summary.MarkedMissing);
    }

    [Fact]
    public async Task Scan_AFolderThatCannotBeRead_StopsAndChangesNothing()
    {
        var books = await AddBooksFolderAsync();
        _drive.File("A.epub", books, Epub("Alpha"));
        await Drive.ScanAsync(cancellationToken: Token);

        _drive.Delete(books);
        var error = await Assert.ThrowsAsync<DriveUnavailableException>(() => Drive.ScanAsync(cancellationToken: Token));

        Assert.Contains("Books", error.Message, StringComparison.Ordinal);
        Assert.Equal(FileStatus.Present, Books().Single().Files.Single().Status);
    }

    [Fact]
    public async Task Scan_IsRefused_WhileAnotherScanRuns()
    {
        var gate = _library.Services.GetRequiredService<LibraryWriteGate>();
        Assert.True(gate.TryEnter(LibraryWork.Scan));
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => Drive.ScanAsync(cancellationToken: Token));
        }
        finally
        {
            gate.Exit();
        }
    }

    [Fact]
    public async Task RemovingAFolder_KeepsItsBooks_WithTheirDriveFilesMissing()
    {
        var books = await AddBooksFolderAsync();
        _drive.File("A.epub", books, Epub("Alpha"));
        await Drive.ScanAsync(cancellationToken: Token);

        await Drive.RemoveSourceAsync((await Drive.ListSourcesAsync(Token)).Single().Id, Token);

        Assert.Empty(await Drive.ListSourcesAsync(Token));
        Assert.Equal(FileStatus.Missing, Books().Single().Files.Single().Status);
    }

    [Fact]
    public async Task RemovingASource_KeepsFilesAnotherSourceStillCovers()
    {
        var books = await AddBooksFolderAsync();
        var a = _drive.File("A.epub", books, Epub("Alpha"));
        _account.Picks.Add(a);
        await Drive.AddPickedFilesAsync(Token);
        await Drive.ScanAsync(cancellationToken: Token);

        await Drive.RemoveSourceAsync((await Drive.ListSourcesAsync(Token)).Single(s => s.Kind == DriveSourceKind.File).Id, Token);

        Assert.Equal(FileStatus.Present, Books().Single().Files.Single().Status);
    }

    [Fact]
    public async Task Download_ChangedOnDrive_IsFetchedAgain()
    {
        var books = await AddBooksFolderAsync();
        var a = _drive.File("A.epub", books, Epub("Alpha"));
        await Drive.ScanAsync(cancellationToken: Token);
        var fileId = Books().Single().Files.Single().Id;
        await Drive.DownloadAsync(fileId, Token);

        var edited = Epub("Alpha");
        edited[^1] ^= 0xFF; // same size, other content
        _drive.Items[a] = _drive.Items[a] with { Content = edited };
        var path = await Drive.DownloadAsync(fileId, Token);

        Assert.Equal(edited, await File.ReadAllBytesAsync(path, Token));
    }

    [Fact]
    public async Task Download_RenamedToAnotherTypeOnDrive_StillOpensAsTheBook()
    {
        var books = await AddBooksFolderAsync();
        var a = _drive.File("A.epub", books, Epub("Alpha"));
        await Drive.ScanAsync(cancellationToken: Token);

        _drive.Rename(a, "A.exe");
        var path = await Drive.DownloadAsync(Books().Single().Files.Single().Id, Token);

        Assert.EndsWith(".epub", path, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Scan_PickedFilesThatAreNotBooks_AreLeftOut()
    {
        _account.Picks.Add(_drive.File("tool.exe", "root", [1, 2, 3]));
        await Drive.AddPickedFilesAsync(Token);

        var summary = await Drive.ScanAsync(cancellationToken: Token);

        Assert.Equal(0, summary.Added);
        Assert.Empty(Books());
    }

    [Fact]
    public async Task Download_FetchesOnce_ThenUsesTheCachedCopy()
    {
        var books = await AddBooksFolderAsync();
        var content = Epub("Alpha");
        _drive.File("A.epub", books, content);
        await Drive.ScanAsync(cancellationToken: Token);
        var fileId = Books().Single().Files.Single().Id;
        _drive.Downloads.Clear();

        var path = await Drive.DownloadAsync(fileId, Token);
        var again = await Drive.DownloadAsync(fileId, Token);

        Assert.Equal(path, again);
        Assert.EndsWith(".epub", path, StringComparison.Ordinal);
        Assert.Equal(content, await File.ReadAllBytesAsync(path, Token));
        Assert.Single(_drive.Downloads);
    }

    public void Dispose() => _library.Dispose();
}
