using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ReaderCompanion.Data;

public static class DataServiceCollectionExtensions
{
    public static IServiceCollection AddLibraryData(this IServiceCollection services, string databasePath)
    {
        var connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();
        services.AddSingleton(new LibraryDatabaseLocation(databasePath));
        return services.AddDbContext<LibraryDbContext>(options => options.UseSqlite(connectionString));
    }

    public static void MigrateLibraryDatabase(this IServiceProvider services)
    {
        var location = services.GetRequiredService<LibraryDatabaseLocation>();
        var folder = Path.GetDirectoryName(Path.GetFullPath(location.Path));
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }

        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<LibraryDbContext>().Database.Migrate();
    }

    private sealed record LibraryDatabaseLocation(string Path);
}
