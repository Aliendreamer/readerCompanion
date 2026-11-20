using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Preferences;
using ReaderCompanion.Core.Scanning;

namespace ReaderCompanion.Data;

public static class DataServiceCollectionExtensions
{
    public static IServiceCollection AddLibraryData(this IServiceCollection services, string databasePath)
    {
        var connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();
        services.AddSingleton(new LibraryDataPaths(databasePath));
        services.AddSingleton<ILibraryScanner, LibraryScanner>();
        services.AddScoped<ILibraryFolders, LibraryFolders>();
        services.AddScoped<ILibraryBooks, LibraryBooks>();
        services.AddScoped<IDuplicates, Duplicates>();
        services.AddScoped<IReading, Reading>();
        services.AddScoped<IReadingStats, ReadingStatsLoader>();
        services.AddSingleton<IAppPreferences, JsonAppPreferences>();
        services.AddSingleton(sp => new CoverStore(sp.GetRequiredService<LibraryDataPaths>()));
        // The factory gives services a fresh context per operation (desktop scopes live as long as the app);
        // it also registers LibraryDbContext itself as scoped.
        return services.AddDbContextFactory<LibraryDbContext>(options => options.UseSqlite(connectionString));
    }

    public static void MigrateLibraryDatabase(this IServiceProvider services)
    {
        Directory.CreateDirectory(services.GetRequiredService<LibraryDataPaths>().DataFolder);

        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<LibraryDbContext>().Database.Migrate();
    }
}
