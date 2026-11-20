using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Scanning;
using ReaderCompanion.Core.Tests;

namespace ReaderCompanion.Data.Tests;

public sealed class LibraryScannerTests : IDisposable
{
    private readonly TempLibrary _library = new();
    private readonly string _books;

    public LibraryScannerTests()
    {
        _books = Path.Combine(_library.Root, "books");
        Directory.CreateDirectory(_books);
        using var db = _library.NewContext();
        db.LibraryFolders.Add(new LibraryFolder { Path = _books, AddedUtc = DateTime.UtcNow });
        db.SaveChanges();
    }

    private ILibraryScanner Scanner => _library.Services.GetRequiredService<ILibraryScanner>();

    private Task<ScanSummary> Scan() => Scanner.ScanAsync(cancellationToken: TestContext.Current.CancellationToken);

    private string WriteEpub(string relativePath, string title, string extraMetadata = "")
    {
        var path = Path.Combine(_books, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var epub = TestEpub.Build($"<dc:title>{title}</dc:title>{extraMetadata}");
        File.WriteAllBytes(path, epub.ToArray());
        return path;
    }

    private List<Book> Books()
    {
        using var db = _library.NewContext();
        return db.Books
            .Include(b => b.Files)
            .Include(b => b.Series)
            .Include(b => b.Authors).ThenInclude(a => a.Author)
            .Include(b => b.Genres).ThenInclude(g => g.Genre)
            .AsNoTracking()
            .ToList();
    }

    [Fact]
    public async Task Scan_NestedAndMixedFiles_AddsOnlyEbooks()
    {
        WriteEpub("a/b/c/book.EPUB", "Deep");
        File.WriteAllText(Path.Combine(_books, "notes.txt"), "not a book");
        await File.WriteAllBytesAsync(Path.Combine(_books, "doc.pdf"), PdfBytes(), TestContext.Current.CancellationToken);

        var summary = await Scan();

        Assert.Equal(2, summary.Added);
        Assert.Equal(["Deep", "doc"], Books().Select(b => b.Title).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Rescan_KeepsTheDateABookWasAdded()
    {
        var path = WriteEpub("a.epub", "Kept");
        await Scan();
        var added = Assert.Single(Books()).DateAddedUtc;

        File.Move(path, Path.Combine(_books, "moved.epub"));
        await Scan();

        Assert.Equal(added, Assert.Single(Books()).DateAddedUtc);
    }

    [Fact]
    public async Task Scan_TwoCopies_OneBookWithTwoFiles()
    {
        var original = WriteEpub("a.epub", "Copy Me");
        Directory.CreateDirectory(Path.Combine(_books, "backup"));
        File.Copy(original, Path.Combine(_books, "backup", "a.epub"));

        await Scan();

        var book = Assert.Single(Books());
        Assert.Equal(2, book.Files.Count);
    }

    [Fact]
    public async Task Rescan_RenamedFile_KeepsSameBook()
    {
        var path = WriteEpub("old-name.epub", "Renamed");
        await Scan();
        var bookId = Assert.Single(Books()).Id;

        File.Move(path, Path.Combine(_books, "new-name.epub"));
        var summary = await Scan();

        var book = Assert.Single(Books());
        Assert.Equal(bookId, book.Id);
        Assert.EndsWith("new-name.epub", Assert.Single(book.Files).Path);
        Assert.Equal(1, summary.Moved);
        Assert.Equal(0, summary.Added);
    }

    [Fact]
    public async Task Rescan_DeletedFile_MarkedMissing_BookKept()
    {
        var path = WriteEpub("gone.epub", "Gone");
        WriteEpub("stays.epub", "Stays");
        await Scan();

        File.Delete(path);
        var summary = await Scan();

        var book = Books().Single(b => b.Title == "Gone");
        Assert.Equal(FileStatus.Missing, Assert.Single(book.Files).Status);
        Assert.Equal(1, summary.MarkedMissing);
    }

    [Fact]
    public async Task Rescan_FileRestoredElsewhere_PresentAgain()
    {
        var path = WriteEpub("back.epub", "Back");
        await Scan();
        var bytes = await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);
        File.Delete(path);
        await Scan();

        Directory.CreateDirectory(Path.Combine(_books, "restored"));
        await File.WriteAllBytesAsync(Path.Combine(_books, "restored", "back.epub"), bytes, TestContext.Current.CancellationToken);
        await Scan();

        var file = Assert.Single(Assert.Single(Books()).Files);
        Assert.Equal(FileStatus.Present, file.Status);
        Assert.Contains("restored", file.Path);
    }

    [Fact]
    public async Task Rescan_UnavailableFolder_FilesNotMarkedMissing_AndReported()
    {
        WriteEpub("a.epub", "Offline");
        await Scan();

        Directory.Move(_books, _books + "-unplugged");
        var summary = await Scan();

        Assert.Equal(FileStatus.Present, Assert.Single(Assert.Single(Books()).Files).Status);
        Assert.Equal([_books], summary.UnavailableFolders);
        Assert.Equal(0, summary.MarkedMissing);
    }

    [Fact]
    public async Task Rescan_FolderNowEmpty_TreatedAsUnavailable()
    {
        var path = WriteEpub("a.epub", "On A Mount");
        await Scan();

        File.Delete(path); // looks exactly like an unmounted drive's empty mount point
        var summary = await Scan();

        Assert.Equal(FileStatus.Present, Assert.Single(Assert.Single(Books()).Files).Status);
        Assert.Equal([_books], summary.UnavailableFolders);
    }

    [Fact]
    public async Task Scan_CoversFolderNotWritable_KeepsBookWithoutCover()
    {
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(_library.DatabasePath)!, "covers"), "a file, not a folder");
        using (var epub = TestEpub.Build("<dc:title>No Cover Space</dc:title>",
            """<item id="c" href="c.jpg" media-type="image/jpeg" properties="cover-image"/>""", ("OEBPS/c.jpg", [1, 2, 3])))
        {
            File.WriteAllBytes(Path.Combine(_books, "a.epub"), epub.ToArray());
        }

        var summary = await Scan();

        var book = Assert.Single(Books());
        Assert.Equal("No Cover Space", book.Title);
        Assert.Null(book.CoverFileName);
        Assert.Equal(1, summary.Added);
    }

    [Fact]
    public async Task Rescan_ReplacedContent_UpdatesSameBook()
    {
        var path = WriteEpub("a.epub", "First Title");
        await Scan();
        var bookId = Assert.Single(Books()).Id;

        WriteEpub("a.epub", "Corrected Title");
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(1));
        var summary = await Scan();

        var book = Assert.Single(Books());
        Assert.Equal(bookId, book.Id);
        Assert.Equal("Corrected Title", book.Title);
        Assert.Equal(1, summary.Updated);
    }

    [Fact]
    public async Task Scan_UnreadableFile_KeptAndListedInSummary()
    {
        WriteEpub("good.epub", "Good");
        File.WriteAllBytes(Path.Combine(_books, "broken.epub"), [1, 2, 3]);

        var summary = await Scan();

        Assert.Equal(["Good", "broken"], Books().Select(b => b.Title).Order(StringComparer.Ordinal));
        Assert.EndsWith("broken.epub", Assert.Single(summary.Unreadable).Path);
        Assert.True(Books().Single(b => b.Title == "broken").Files.Single().Unreadable);
    }

    [Fact]
    public async Task Rescan_NoChanges_ReadsNothing()
    {
        WriteEpub("a.epub", "Stable");
        WriteEpub("b.epub", "Steady");
        await Scan();

        var summary = await Scan();

        Assert.Equal(0, summary.FilesRead);
        Assert.Equal(0, summary.Added + summary.Moved + summary.Updated + summary.MarkedMissing);
    }

    [Fact]
    public async Task Scan_AuthorsGenresAndSeries_AreShared()
    {
        WriteEpub("1.epub", "Colour of Magic", """<dc:creator>Terry Pratchett</dc:creator><dc:subject>Fantasy</dc:subject><meta name="calibre:series" content="Discworld"/><meta name="calibre:series_index" content="1"/>""");
        WriteEpub("2.epub", "Light Fantastic", """<dc:creator>terry  pratchett</dc:creator><dc:subject>fantasy</dc:subject><meta name="calibre:series" content="discworld"/><meta name="calibre:series_index" content="2"/>""");
        WriteEpub("3.epub", "Equal Rites", """<dc:creator>Terry Pratchett</dc:creator><meta name="calibre:series" content="Discworld"/><meta name="calibre:series_index" content="3"/>""");

        await Scan();

        using var db = _library.NewContext();
        Assert.Equal("Terry Pratchett", Assert.Single(db.Authors).Name);
        Assert.Equal("Fantasy", Assert.Single(db.Genres).Name);
        Assert.Equal("Discworld", Assert.Single(db.Collections).Name);
        Assert.Equal(["Colour of Magic", "Light Fantastic", "Equal Rites"],
            Books().OrderBy(b => b.SeriesIndex).Select(b => b.Title));
    }

    [Fact]
    public async Task Scan_EpubCover_IsStoredInCoversFolder()
    {
        byte[] cover = [0xFF, 0xD8, 0xFF, 9, 9];
        var path = Path.Combine(_books, "covered.epub");
        using (var epub = TestEpub.Build("<dc:title>Covered</dc:title>",
            """<item id="c" href="c.jpg" media-type="image/jpeg" properties="cover-image"/>""", ("OEBPS/c.jpg", cover)))
        {
            File.WriteAllBytes(path, epub.ToArray());
        }

        await Scan();

        var book = Assert.Single(Books());
        var coverPath = Path.Combine(Path.GetDirectoryName(_library.DatabasePath)!, "covers", book.CoverFileName!);
        Assert.Equal(cover, await File.ReadAllBytesAsync(coverPath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Scan_ReportsProgressUpToFilesFound()
    {
        WriteEpub("a.epub", "A");
        WriteEpub("b.epub", "B");
        var reports = new List<ScanProgress>();

        await Scanner.ScanAsync(new SyncProgress(reports.Add), TestContext.Current.CancellationToken);

        Assert.Equal(new ScanProgress(2, 2), reports.Last());
    }

    [Fact]
    public async Task Scan_WhileAnotherIsRunning_IsRefused()
    {
        WriteEpub("a.epub", "A");
        Task<ScanSummary>? second = null;

        await Scanner.ScanAsync(new SyncProgress(_ => second ??= Scanner.ScanAsync()), TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(() => second!);
    }

    private static byte[] PdfBytes()
    {
        var builder = new UglyToad.PdfPig.Writer.PdfDocumentBuilder();
        builder.AddPage(UglyToad.PdfPig.Content.PageSize.A4);
        return builder.Build();
    }

    private sealed class SyncProgress(Action<ScanProgress> report) : IProgress<ScanProgress>
    {
        public void Report(ScanProgress value) => report(value);
    }

    public void Dispose() => _library.Dispose();
}
