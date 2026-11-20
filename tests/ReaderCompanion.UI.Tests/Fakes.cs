using Microsoft.Extensions.DependencyInjection;
using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Preferences;
using ReaderCompanion.Core.Scanning;

namespace ReaderCompanion.UI.Tests;

internal sealed class FakePicker(params string?[] answers) : IFolderPicker
{
    private readonly Queue<string?> _answers = new(answers);

    public int Calls { get; private set; }

    public Task<string?> PickFolderAsync()
    {
        Calls++;
        return Task.FromResult(_answers.Count > 0 ? _answers.Dequeue() : null);
    }
}

internal sealed class FakeFolders : ILibraryFolders
{
    public List<string> Linked { get; } = [];

    public LinkFolderResult? NextResult { get; set; }

    public int LinkCalls { get; private set; }

    public Task<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>([.. Linked]);

    public Task<LinkFolderResult> LinkAsync(string path, CancellationToken cancellationToken = default)
    {
        LinkCalls++;
        var result = NextResult ?? new LinkFolderResult(true);
        if (result.Linked)
        {
            Linked.Add(path);
        }

        return Task.FromResult(result);
    }

    public Task UnlinkAsync(string path, CancellationToken cancellationToken = default)
    {
        Linked.Remove(path);
        return Task.CompletedTask;
    }
}

/// <summary>A scanner the test drives by hand: report progress, then finish.</summary>
internal sealed class FakeScanner : ILibraryScanner
{
    private TaskCompletionSource<ScanSummary>? _running;
    private IProgress<ScanProgress>? _progress;

    public bool IsScanning => _running is not null;

    public Task<ScanSummary> ScanAsync(IProgress<ScanProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        _progress = progress;
        _running = new TaskCompletionSource<ScanSummary>();
        return _running.Task;
    }

    public void Report(int processed, int found) => _progress!.Report(new ScanProgress(processed, found));

    public void Finish(ScanSummary summary)
    {
        var running = _running!;
        _running = null;
        running.SetResult(summary);
    }
}

internal sealed class FakePreferences(AppPreferences? initial = null) : IAppPreferences
{
    public AppPreferences Current { get; private set; } = initial ?? new AppPreferences();

    public int Saves { get; private set; }

    public event Action? Changed;

    public void Save(AppPreferences preferences)
    {
        Saves++;
        Current = preferences;
        Changed?.Invoke();
    }
}

internal sealed class FakeSystemTheme(bool prefersDark = false) : ISystemTheme
{
    private Action<bool>? _listener;

    /// <summary>Makes every call fail, as when the web view's script can't be loaded.</summary>
    public bool Broken { get; set; }

    public bool Listening => _listener is not null;

    public ValueTask<bool> PrefersDarkAsync() =>
        Broken ? throw new Microsoft.JSInterop.JSException("theme.js failed") : ValueTask.FromResult(prefersDark);

    public ValueTask<IAsyncDisposable> ListenAsync(Action<bool> onChange)
    {
        if (Broken)
        {
            throw new Microsoft.JSInterop.JSException("theme.js failed");
        }

        _listener = onChange;
        return ValueTask.FromResult<IAsyncDisposable>(new Subscription(() => _listener = null));
    }

    private sealed class Subscription(Action stop) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            stop();
            return ValueTask.CompletedTask;
        }
    }

    public void Change(bool dark)
    {
        prefersDark = dark;
        _listener?.Invoke(dark);
    }
}

internal sealed class FakeCoverUrls : ICoverUrls
{
    public string UrlFor(string coverFileName) => "test-cover://" + coverFileName;
}

internal sealed class FakeBooks(List<BookListItem> books) : ILibraryBooks
{
    /// <summary>Files per book id; books without an entry have none.</summary>
    public Dictionary<int, IReadOnlyList<BookFileInfo>> Files { get; } = [];

    public List<(int Id, BookEdit Edit)> Updates { get; } = [];

    /// <summary>When true, saving throws, as when the database is busy.</summary>
    public bool FailUpdates { get; set; }

    public Task<IReadOnlyList<BookListItem>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<BookListItem>>([.. books]);

    public Task<BookDetails?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(books.FirstOrDefault(b => b.Id == id) is { } book
            ? new BookDetails(book, "en", "978-0", Updates.Any(u => u.Id == id), Files.GetValueOrDefault(id, []))
            : null);

    public Task<bool> UpdateAsync(int id, BookEdit edit, CancellationToken cancellationToken = default)
    {
        if (FailUpdates)
        {
            throw new InvalidOperationException("database is locked");
        }

        var index = books.FindIndex(b => b.Id == id);
        if (index < 0)
        {
            return Task.FromResult(false);
        }

        Updates.Add((id, edit));
        var series = string.IsNullOrWhiteSpace(edit.Series) ? null : edit.Series.Trim();
        books[index] = books[index] with
        {
            Title = edit.Title.Trim(),
            Authors = Clean(edit.Authors),
            Series = series,
            SeriesIndex = series is null ? null : edit.SeriesIndex,
            Genres = Clean(edit.Genres),
        };
        return Task.FromResult(true);
    }

    // The real library cleans names; a rough stand-in is enough here.
    private static string[] Clean(IEnumerable<string> names) =>
        [.. names.Select(n => n.Trim()).Where(n => n.Length > 0)];
}

internal sealed class FakeLauncher : IFileLauncher
{
    public List<string> Opened { get; } = [];

    public List<string> Shown { get; } = [];

    /// <summary>When false, every launch fails, as when no app handles the file.</summary>
    public bool Works { get; set; } = true;

    public bool Open(string path)
    {
        Opened.Add(path);
        return Works;
    }

    public bool ShowInFolder(string path)
    {
        Shown.Add(path);
        return Works;
    }
}

internal static class TestServices
{
    /// <summary>Registers every service the app's pages and layout use, with fakes.</summary>
    public static void AddAppFakes(
        this IServiceCollection services,
        List<BookListItem>? books = null,
        FakePreferences? preferences = null,
        FakeSystemTheme? systemTheme = null,
        FakeBooks? libraryBooks = null,
        FakeLauncher? launcher = null)
    {
        services.AddSingleton<ILibraryBooks>(libraryBooks ?? new FakeBooks(books ?? []));
        services.AddSingleton<IFileLauncher>(launcher ?? new FakeLauncher());
        services.AddSingleton<ILibraryFolders>(new FakeFolders());
        services.AddSingleton<ILibraryScanner>(new FakeScanner());
        services.AddSingleton<IFolderPicker>(new FakePicker());
        services.AddSingleton<IAppPreferences>(preferences ?? new FakePreferences());
        services.AddSingleton<ISystemTheme>(systemTheme ?? new FakeSystemTheme());
        services.AddSingleton<ICoverUrls>(new FakeCoverUrls());
    }
}
