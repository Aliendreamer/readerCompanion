using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Scanning;

namespace ReaderCompanion.Data;

internal sealed partial class Backups(
    LibraryDataPaths paths, IDbContextFactory<LibraryDbContext> contexts, ILibraryScanner scanner) : IBackups
{
    private const string Stamp = "yyyy-MM-dd-HHmmss";
    private const string BeforeRestoreTag = "-before-restore";

    public BackupInfo Backup(string folder, DateTime nowLocal) => Write(folder, nowLocal, beforeRestore: false);

    public Task<BackupInfo> BackupAsync(string folder, DateTime nowLocal, CancellationToken cancellationToken = default) =>
        Task.Run(() => Backup(folder, nowLocal), cancellationToken);

    public IReadOnlyList<BackupInfo> List(string folder)
    {
        if (!Directory.Exists(folder))
        {
            return [];
        }

        var found = new List<(BackupInfo Info, int Counter)>();
        foreach (var path in Directory.EnumerateFiles(folder, "readerCompanion-*.db"))
        {
            // Skip names that only look like a backup (an impossible date, a huge counter).
            if (BackupName().Match(Path.GetFileName(path)) is { Success: true } match
                && DateTime.TryParseExact(match.Groups["stamp"].Value, Stamp, CultureInfo.InvariantCulture, DateTimeStyles.None, out var created)
                && (!match.Groups["n"].Success || int.TryParse(match.Groups["n"].Value, CultureInfo.InvariantCulture, out _)))
            {
                var counter = match.Groups["n"].Success ? int.Parse(match.Groups["n"].Value, CultureInfo.InvariantCulture) : 1;
                found.Add((new BackupInfo(path, created, new FileInfo(path).Length, match.Groups["restore"].Success), counter));
            }
        }

        return [.. found.OrderByDescending(x => x.Info.CreatedLocal).ThenByDescending(x => x.Counter).Select(x => x.Info)];
    }

    public async Task<BackupInfo> RestoreAsync(string backupPath, DateTime nowLocal, CancellationToken cancellationToken = default)
    {
        if (scanner.IsScanning)
        {
            throw new InvalidOperationException("Wait for the scan to finish before restoring.");
        }

        var safety = await Task.Run(() =>
        {
            EnsureLibrary(backupPath);
            var safety = Write(Path.GetDirectoryName(Path.GetFullPath(backupPath))!, nowLocal, beforeRestore: true);
            using var source = Open(backupPath, readOnly: true);
            using var live = Open(paths.DatabasePath, readOnly: false);
            source.BackupDatabase(live);
            return safety;
        }, cancellationToken);

        SqliteConnection.ClearAllPools(); // the app's pooled connections must not keep reading the old content
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken); // an older backup gets the newer schema
        return safety;
    }

    public void Prune(string folder, int keep)
    {
        foreach (var old in List(folder).Where(b => !b.BeforeRestore).Skip(Math.Max(keep, 1)))
        {
            File.Delete(old.Path);
        }
    }

    private BackupInfo Write(string folder, DateTime nowLocal, bool beforeRestore)
    {
        Directory.CreateDirectory(folder);
        var stamp = nowLocal.ToString(Stamp, CultureInfo.InvariantCulture);
        var path = Path.Combine(folder, Name(stamp, 1, beforeRestore));
        for (var n = 2; File.Exists(path); n++)
        {
            path = Path.Combine(folder, Name(stamp, n, beforeRestore));
        }

        using (var live = Open(paths.DatabasePath, readOnly: true))
        using (var copy = Open(path, readOnly: false))
        {
            live.BackupDatabase(copy);
        }

        return new BackupInfo(path, DateTime.ParseExact(stamp, Stamp, CultureInfo.InvariantCulture), new FileInfo(path).Length, beforeRestore);
    }

    /// <summary>A backup's file name; <see cref="BackupName"/> reads it back.</summary>
    private static string Name(string stamp, int counter, bool beforeRestore) =>
        $"readerCompanion-{stamp}{(counter > 1 ? $"-{counter}" : "")}{(beforeRestore ? BeforeRestoreTag : "")}.db";

    /// <summary>Throws unless the file is a readerCompanion library (has the migrations history and the books).</summary>
    private static void EnsureLibrary(string path)
    {
        var notABackup = $"{Path.GetFileName(path)} is not a readerCompanion backup.";
        try
        {
            using var connection = Open(path, readOnly: true);
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT count(*) FROM sqlite_master WHERE type = 'table' AND name IN ('__EFMigrationsHistory', 'Books')";
            if (Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) != 2)
            {
                throw new InvalidDataException(notABackup);
            }
        }
        catch (SqliteException e)
        {
            throw new InvalidDataException(notABackup, e);
        }
    }

    // Not pooled: these connections touch one-off files and must let go of them when closed.
    private static SqliteConnection Open(string path, bool readOnly)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = readOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWriteCreate,
            Pooling = false,
        }.ToString());
        connection.Open();
        return connection;
    }

    [GeneratedRegex(@"^readerCompanion-(?<stamp>\d{4}-\d{2}-\d{2}-\d{6})(-(?<n>\d+))?(?<restore>-before-restore)?\.db$")]
    private static partial Regex BackupName();
}
