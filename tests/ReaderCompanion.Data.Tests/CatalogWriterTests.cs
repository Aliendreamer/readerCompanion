using ReaderCompanion.Core.Scanning;

namespace ReaderCompanion.Data.Tests;

public sealed class CatalogWriterTests : IDisposable
{
    private readonly TempLibrary _library = new();

    [Fact]
    public void FileGoneBeforeItIsRead_BookKeptAsUnreadable()
    {
        using var db = _library.NewContext();
        var writer = new CatalogWriter(db, Path.Combine(_library.Root, "covers"), DateTime.UtcNow);
        var vanished = new FoundFile(Path.Combine(_library.Root, "vanished.epub"), 10, DateTime.UtcNow, "h1");

        writer.Apply(new AddBook([vanished]));
        db.SaveChanges();

        Assert.Equal("vanished", db.Books.Single().Title);
        Assert.Equal(vanished.Path, Assert.Single(writer.Unreadable).Path);
    }

    [Fact]
    public void NewBook_IsDatedWithTheScanTime()
    {
        using var db = _library.NewContext();
        var scanTime = new DateTime(2026, 3, 4, 5, 6, 7, DateTimeKind.Utc);
        var writer = new CatalogWriter(db, Path.Combine(_library.Root, "covers"), scanTime);
        var found = new FoundFile(Path.Combine(_library.Root, "gone.epub"), 10, DateTime.UtcNow, "h1");

        writer.Apply(new AddBook([found]));
        db.SaveChanges();

        Assert.Equal(scanTime, db.Books.Single().DateAddedUtc);
    }

    [Fact]
    public void DriveFiles_AreReadThroughTheOpener_AndKeepTheirDriveId()
    {
        using var db = _library.NewContext();
        var epub = ReaderCompanion.Core.Tests.TestEpub.Build("<dc:title>From Drive</dc:title>").ToArray();
        var path = ReaderCompanion.Core.Drive.DrivePath.Make("d1", "Books/x.epub");
        var opened = new List<string>();
        var writer = new CatalogWriter(db, Path.Combine(_library.Root, "covers"), DateTime.UtcNow, p =>
        {
            opened.Add(p);
            return new MemoryStream(epub);
        });

        writer.Apply(new AddBook([new FoundFile(path, epub.Length, DateTime.UtcNow, "h1", "d1")]));
        db.SaveChanges();

        Assert.Equal([path], opened);
        var book = db.Books.Single();
        Assert.Equal("From Drive", book.Title);
        var file = db.BookFiles.Single();
        Assert.Equal(("d1", path), (file.DriveFileId, file.Path));
    }

    public void Dispose() => _library.Dispose();
}
