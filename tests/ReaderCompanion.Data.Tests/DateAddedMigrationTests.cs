using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace ReaderCompanion.Data.Tests;

public sealed class DateAddedMigrationTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("rc-tests-").FullName;

    [Fact]
    public void ExistingBook_GetsItsOldestFilesLastWriteTime()
    {
        var path = Path.Combine(_root, "library.db");
        using var provider = new ServiceCollection().AddLibraryData(path).BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        var migrator = db.GetInfrastructure().GetRequiredService<IMigrator>();
        migrator.Migrate("20261119111251_AddLibrary");
        db.Database.ExecuteSqlRaw("""
            INSERT INTO Books (Id, Title) VALUES (1, 'Old'), (2, 'Fileless');
            INSERT INTO BookFiles (BookId, Path, Format, SizeBytes, LastWriteUtc, ContentHash, Status, Unreadable, LastSeenUtc)
            VALUES (1, '/new.epub', 'Epub', 1, '2024-05-01 10:00:00', 'a', 'Present', 0, '2026-11-19 12:00:00'),
                   (1, '/old.pdf', 'Pdf', 1, '2021-02-03 04:05:06', 'b', 'Present', 0, '2026-11-19 12:00:00');
            """);

        migrator.Migrate();

        var dates = db.Books.AsNoTracking().ToDictionary(b => b.Title, b => b.DateAddedUtc);
        Assert.Equal(new DateTime(2021, 2, 3, 4, 5, 6), dates["Old"]);
        Assert.True(dates["Fileless"] > new DateTime(2026, 1, 1));
    }

    [Fact]
    public async Task ExistingBooks_AreNotStarted_AfterTheReadingMigration()
    {
        var path = Path.Combine(_root, "reading.db");
        using var provider = new ServiceCollection().AddLibraryData(path).BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        var migrator = db.GetInfrastructure().GetRequiredService<IMigrator>();
        migrator.Migrate(db.Database.GetMigrations().Single(m => m.EndsWith("_AddUserCollections", StringComparison.Ordinal)));
        db.Database.ExecuteSqlRaw("INSERT INTO Books (Id, Title, DateAddedUtc) VALUES (1, 'Old', '2024-01-01 00:00:00');");

        migrator.Migrate();

        var books = await scope.ServiceProvider.GetRequiredService<ReaderCompanion.Core.Library.ILibraryBooks>()
            .ListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(ReaderCompanion.Core.Library.ReadStatus.NotStarted, Assert.Single(books).ReadStatus);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_root, recursive: true);
    }
}
