using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using ReaderCompanion.Core.Library;

namespace ReaderCompanion.Data.Tests;

public sealed class BackupsTests : IDisposable
{
    private readonly TempLibrary _library = new();
    private readonly string _folder;
    private static readonly DateTime Noon = new(2026, 11, 20, 12, 0, 0, DateTimeKind.Local);

    public BackupsTests()
    {
        _folder = Path.Combine(_library.Root, "backups");
        Directory.CreateDirectory(_folder);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private IBackups Backups => _library.Services.GetRequiredService<IBackups>();

    private void AddBook(string title)
    {
        using var db = _library.NewContext();
        db.Books.Add(new Book { Title = title });
        db.SaveChanges();
    }

    private List<string> Titles()
    {
        using var db = _library.NewContext();
        return [.. db.Books.AsNoTracking().Select(b => b.Title).OrderBy(t => t)];
    }

    [Fact]
    public async Task Backup_WritesANamedCopy_AndListShowsItNewestFirst()
    {
        AddBook("Mort");

        var first = await Backups.BackupAsync(_folder, Noon, Token);
        var second = await Backups.BackupAsync(_folder, Noon, Token); // same second
        var third = await Backups.BackupAsync(_folder, Noon.AddMinutes(1), Token);
        File.WriteAllText(Path.Combine(_folder, "photo.jpg"), "not a backup");

        Assert.Equal("readerCompanion-2026-11-20-120000.db", Path.GetFileName(first.Path));
        Assert.Equal("readerCompanion-2026-11-20-120000-2.db", Path.GetFileName(second.Path));
        Assert.Equal([third.Path, second.Path, first.Path], Backups.List(_folder).Select(b => b.Path));
        Assert.True(first.SizeBytes > 0);
        Assert.Equal(Noon, first.CreatedLocal);
    }

    [Fact]
    public async Task Restore_BringsBackTheBackup_AfterBackingUpTheCurrentLibrary()
    {
        AddBook("Mort");
        var backup = await Backups.BackupAsync(_folder, Noon, Token);
        AddBook("Dune");

        var safety = await Backups.RestoreAsync(backup.Path, Noon.AddHours(1), Token);

        Assert.Equal(["Mort"], Titles());
        Assert.EndsWith("-before-restore.db", safety.Path, StringComparison.Ordinal);
        Assert.Contains(safety.Path, Backups.List(_folder).Select(b => b.Path));
    }

    [Fact]
    public async Task Restore_UpgradesAnOlderBackup()
    {
        var old = Path.Combine(_folder, "readerCompanion-2025-01-01-000000.db");
        using (var provider = new ServiceCollection().AddLibraryData(old).BuildServiceProvider())
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
            db.GetInfrastructure().GetRequiredService<IMigrator>().Migrate("20261119111251_AddLibrary");
            db.Database.ExecuteSqlRaw("INSERT INTO Books (Id, Title) VALUES (1, 'Old');");
        }

        SqliteConnection.ClearAllPools();

        await Backups.RestoreAsync(old, Noon, Token);

        var book = Assert.Single(await _library.Services.CreateScope().ServiceProvider.GetRequiredService<ILibraryBooks>().ListAsync(Token));
        Assert.Equal("Old", book.Title);
        Assert.Equal(ReadStatus.NotStarted, book.ReadStatus);
    }

    [Fact]
    public async Task Restore_RefusesAFileThatIsNotALibrary()
    {
        AddBook("Mort");
        var bogus = Path.Combine(_folder, "readerCompanion-2026-01-01-000000.db");
        File.WriteAllText(bogus, "not sqlite at all");

        await Assert.ThrowsAsync<InvalidDataException>(() => Backups.RestoreAsync(bogus, Noon, Token));

        Assert.Equal(["Mort"], Titles());
    }

    [Fact]
    public async Task Prune_KeepsTheNewestN_AndNeverTouchesOtherFiles()
    {
        for (var i = 0; i < 4; i++)
        {
            await Backups.BackupAsync(_folder, Noon.AddMinutes(i), Token);
        }

        var photo = Path.Combine(_folder, "photo.jpg");
        File.WriteAllText(photo, "keep me");

        Backups.Prune(_folder, 2);

        Assert.Equal(
            ["readerCompanion-2026-11-20-120300.db", "readerCompanion-2026-11-20-120200.db"],
            Backups.List(_folder).Select(b => Path.GetFileName(b.Path)));
        Assert.True(File.Exists(photo));
    }

    [Fact]
    public async Task Prune_NeverDeletesTheCopyTakenBeforeARestore()
    {
        AddBook("Mort");
        var backup = await Backups.BackupAsync(_folder, Noon, Token);
        var safety = await Backups.RestoreAsync(backup.Path, Noon.AddMinutes(1), Token);
        for (var i = 2; i < 5; i++)
        {
            await Backups.BackupAsync(_folder, Noon.AddMinutes(i), Token);
        }

        Backups.Prune(_folder, 1);

        Assert.True(File.Exists(safety.Path));
        Assert.Equal(2, Backups.List(_folder).Count);
    }

    [Fact]
    public async Task Restore_IsRefused_WhileAScanRuns()
    {
        AddBook("Mort");
        var backup = await Backups.BackupAsync(_folder, Noon, Token);
        var gate = _library.Services.GetRequiredService<LibraryWriteGate>();
        Assert.True(gate.TryEnter(LibraryWork.Scan));
        try
        {
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Backups.RestoreAsync(backup.Path, Noon, Token));
            Assert.Contains("scan is running", error.Message);
        }
        finally
        {
            gate.Exit();
        }

        Assert.Equal(["Mort"], Titles());
    }

    [Fact]
    public async Task List_SkipsFilesThatOnlyLookLikeBackups()
    {
        var real = await Backups.BackupAsync(_folder, Noon, Token);
        File.WriteAllText(Path.Combine(_folder, "readerCompanion-2024-13-45-999999.db"), "x");
        File.WriteAllText(Path.Combine(_folder, "readerCompanion-2024-01-01-000000-99999999999.db"), "x");

        Assert.Equal([real.Path], Backups.List(_folder).Select(b => b.Path));
    }

    public void Dispose() => _library.Dispose();
}
