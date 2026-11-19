using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ReaderCompanion.Data.Tests;

public sealed class StartupMigrationTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("rc-tests-").FullName;

    [Fact]
    public void Migrate_CreatesMissingParentFolderAndDatabase()
    {
        var path = Path.Combine(_root, "nested", "deeper", "library.db");

        MigrateAt(path);

        Assert.True(File.Exists(path));
        Assert.Equal(DefinedMigrations(path), AppliedMigrations(path));
    }

    [Fact]
    public void Migrate_PathWithSpacesAndUnicode_Works()
    {
        var path = Path.Combine(_root, "José Ü folder", "library.db");

        MigrateAt(path);

        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Migrate_RunTwice_IsNoOp()
    {
        var path = Path.Combine(_root, "library.db");

        MigrateAt(path);
        MigrateAt(path);

        Assert.Equal(DefinedMigrations(path), AppliedMigrations(path));
    }

    private static ServiceProvider BuildProvider(string path) =>
        new ServiceCollection().AddLibraryData(path).BuildServiceProvider();

    private static void MigrateAt(string path)
    {
        using var provider = BuildProvider(path);
        provider.MigrateLibraryDatabase();
    }

    private static List<string> AppliedMigrations(string path)
    {
        using var provider = BuildProvider(path);
        using var scope = provider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<LibraryDbContext>().Database.GetAppliedMigrations().ToList();
    }

    private static List<string> DefinedMigrations(string path)
    {
        using var provider = BuildProvider(path);
        using var scope = provider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<LibraryDbContext>().Database.GetMigrations().ToList();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_root, recursive: true);
    }
}
