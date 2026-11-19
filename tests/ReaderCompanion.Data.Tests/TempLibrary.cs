using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace ReaderCompanion.Data.Tests;

/// <summary>A migrated library database in its own temp folder, deleted on dispose.</summary>
public sealed class TempLibrary : IDisposable
{
    public TempLibrary()
    {
        Root = Directory.CreateTempSubdirectory("rc-lib-").FullName;
        DatabasePath = Path.Combine(Root, "data", "library.db");
        Services = new ServiceCollection().AddLibraryData(DatabasePath).BuildServiceProvider();
        Services.MigrateLibraryDatabase();
    }

    public string Root { get; }

    public string DatabasePath { get; }

    public ServiceProvider Services { get; }

    public LibraryDbContext NewContext() =>
        Services.CreateScope().ServiceProvider.GetRequiredService<LibraryDbContext>();

    public void Dispose()
    {
        Services.Dispose();
        SqliteConnection.ClearAllPools();
        Directory.Delete(Root, recursive: true);
    }
}
