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
        // Tests must never touch the real system trash: a recording trash is registered first.
        // Nor Google Drive: an in-memory one, linked with read and write access.
        Services = new ServiceCollection()
            .AddSingleton<ReaderCompanion.Core.Library.ITrash>(Trash)
            .AddSingleton<ReaderCompanion.Core.Drive.IDriveAccount>(DriveAccount)
            .AddSingleton(new GoogleDriveFiles(DriveAccount, Drive.Client()))
            .AddLibraryData(DatabasePath)
            .BuildServiceProvider();
        Services.MigrateLibraryDatabase();
    }

    public string Root { get; }

    public RecordingTrash Trash { get; } = new();

    internal FakeDrive Drive { get; } = new();

    internal TestDriveAccount DriveAccount { get; } = new();

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

/// <summary>A trash that records what it was given and deletes nothing; <see cref="Works"/> false makes it refuse.</summary>
public sealed class RecordingTrash : ReaderCompanion.Core.Library.ITrash
{
    public List<string> Trashed { get; } = [];

    public bool Works { get; set; } = true;

    public bool MoveToTrash(string path)
    {
        if (Works)
        {
            Trashed.Add(path);
        }

        return Works;
    }
}
