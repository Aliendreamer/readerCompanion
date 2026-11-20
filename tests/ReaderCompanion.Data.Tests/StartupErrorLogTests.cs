using Microsoft.Extensions.DependencyInjection;
using ReaderCompanion.Core.Library;

namespace ReaderCompanion.Data.Tests;

public sealed class StartupErrorLogTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("rc-startup-").FullName;
    private static readonly DateTime When = new(2026, 11, 20, 9, 30, 0);

    private static Exception Thrown()
    {
        try
        {
            throw new InvalidOperationException("database disk image is malformed");
        }
        catch (InvalidOperationException e)
        {
            return e;
        }
    }

    [Fact]
    public void Write_PutsTheDetailsInTheDataFolder_AndGivesAShortMessage()
    {
        var (logPath, message) = StartupErrorLog.Write(Thrown(), _root, When);

        Assert.Equal(Path.Combine(_root, "startup-error.log"), logPath);
        var log = File.ReadAllText(logPath!);
        Assert.Contains("2026-11-20 09:30:00", log);
        Assert.Contains("database disk image is malformed", log);
        Assert.Contains("at ReaderCompanion.Data.Tests", log); // the stack trace
        Assert.Contains("could not start", message);
        Assert.Contains("database disk image is malformed", message);
        Assert.Contains(logPath!, message);
    }

    [Fact]
    public void UnusableFolder_FallsBackToTemp()
    {
        var blocked = Path.Combine(_root, "file-not-folder");
        File.WriteAllText(blocked, "x");

        var (logPath, _) = StartupErrorLog.Write(Thrown(), blocked, When);

        Assert.StartsWith(Path.GetTempPath(), logPath, StringComparison.Ordinal);
        Assert.NotEqual(Path.Combine(Path.GetTempPath(), StartupErrorLog.FileName), logPath); // never a shared, guessable name
        Assert.True(File.Exists(logPath));
        File.Delete(logPath!);
    }

    [Fact]
    public void LibraryLocation_IsTheDatabasesFolder()
    {
        using var library = new TempLibrary();

        Assert.Equal(Path.GetDirectoryName(library.DatabasePath), library.Services.GetRequiredService<ILibraryLocation>().DataFolder);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
