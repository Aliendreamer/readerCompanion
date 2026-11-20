using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Organising;
using ReaderCompanion.Core.Preferences;
using ReaderCompanion.Core.Scanning;

namespace ReaderCompanion.Data;

public static class DataServiceCollectionExtensions
{
    public static IServiceCollection AddLibraryData(this IServiceCollection services, string databasePath)
    {
        var connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();
        var paths = new LibraryDataPaths(databasePath);
        services.AddSingleton(paths);
        services.AddSingleton<ILibraryLocation>(paths);
        services.AddSingleton<LibraryWriteGate>();
        services.AddSingleton<ILibraryScanner, LibraryScanner>();
        services.AddScoped<IOrganiser, Organiser>();
        services.AddScoped<ILibraryFolders, LibraryFolders>();
        services.AddScoped<ILibraryBooks, LibraryBooks>();
        services.AddScoped<IDuplicates, Duplicates>();
        services.AddScoped<IReading, Reading>();
        services.AddScoped<IReadingStats, ReadingStatsLoader>();
        services.AddSingleton<IBackups, Backups>();
        services.AddSingleton<ICoverFetcher>(sp => new OpenLibraryCovers(
            sp.GetRequiredService<IDbContextFactory<LibraryDbContext>>(),
            sp.GetRequiredService<LibraryDataPaths>(),
            CoverHttp.Value,
            Task.Delay));
        services.AddScoped<ReaderCompanion.Core.Goodreads.IGoodreadsImport, GoodreadsImport>();
        services.TryAddSingleton(_ => PlatformTrash.ForThisMachine()); // tests register their own first
        services.AddSingleton<IAppPreferences, JsonAppPreferences>();
        services.AddSingleton(sp => new CoverStore(sp.GetRequiredService<LibraryDataPaths>()));
        // The factory gives services a fresh context per operation (desktop scopes live as long as the app);
        // it also registers LibraryDbContext itself as scoped.
        return services.AddDbContextFactory<LibraryDbContext>(options => options.UseSqlite(connectionString));
    }

    // One client for the app's life; a generic agent name, nothing about the user.
    private static readonly Lazy<HttpClient> CoverHttp = new(() =>
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("readerCompanion");
        return client;
    });

    public static void MigrateLibraryDatabase(this IServiceProvider services)
    {
        Directory.CreateDirectory(services.GetRequiredService<LibraryDataPaths>().DataFolder);

        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<LibraryDbContext>().Database.Migrate();
    }
}
