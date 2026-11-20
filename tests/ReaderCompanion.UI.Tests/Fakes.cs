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
    public List<BookListItem> Books => books;

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

    public async Task<IReadOnlyList<BookDetails>> GetManyAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default)
    {
        var found = new List<BookDetails>();
        foreach (var id in ids.Order())
        {
            if (await GetAsync(id, cancellationToken) is { } details)
            {
                found.Add(details);
            }
        }

        return found;
    }

    public Task<bool> AddToCollectionAsync(int id, string name, CancellationToken cancellationToken = default) =>
        ChangeCollections(id, names => [.. names.Append(name.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Order()]);

    public Task<bool> RemoveFromCollectionAsync(int id, string name, CancellationToken cancellationToken = default) =>
        ChangeCollections(id, names => [.. names.Where(n => !n.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase))]);

    private Task<bool> ChangeCollections(int id, Func<IReadOnlyList<string>, string[]> change)
    {
        if (FailUpdates)
        {
            throw new InvalidOperationException("database is locked");
        }

        var index = books.FindIndex(b => b.Id == id);
        if (index >= 0)
        {
            books[index] = books[index] with { Collections = change(books[index].Collections) };
        }

        return Task.FromResult(index >= 0);
    }

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

internal sealed class FakeDuplicates : IDuplicates
{
    public List<DuplicateGroup> Groups { get; } = [];

    public List<(int Keep, IReadOnlyCollection<int> Others)> Merges { get; } = [];

    public List<IReadOnlyCollection<int>> Dismissals { get; } = [];

    /// <summary>When true, merging and dismissing throw, as when the database is busy.</summary>
    public bool Fail { get; set; }

    public Task<IReadOnlyList<DuplicateGroup>> FindAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DuplicateGroup>>([.. Groups]);

    public Task<bool> MergeAsync(int keepId, IReadOnlyCollection<int> otherIds, CancellationToken cancellationToken = default)
    {
        if (Fail)
        {
            throw new InvalidOperationException("database is locked");
        }

        Merges.Add((keepId, otherIds));
        Groups.RemoveAll(g => g.Books.Any(b => b.Book.Id == keepId));
        return Task.FromResult(true);
    }

    public Task DismissAsync(IReadOnlyCollection<int> bookIds, CancellationToken cancellationToken = default)
    {
        Dismissals.Add(bookIds);
        Groups.RemoveAll(g => g.Books.Any(b => bookIds.Contains(b.Book.Id)));
        return Task.CompletedTask;
    }
}

/// <summary>Reading data in memory; status and rating changes also show in the book list.</summary>
internal sealed class FakeReading(List<BookListItem> books) : IReading
{
    private readonly Dictionary<int, (ReadStatus Status, int? Rating, string? Notes, List<ReadingSession> Sessions)> _data = [];
    private int _nextSession = 1;

    public DateOnly? LastToday { get; private set; }

    private (ReadStatus Status, int? Rating, string? Notes, List<ReadingSession> Sessions) Data(int id) =>
        _data.TryGetValue(id, out var d) ? d : _data[id] = (ReadStatus.NotStarted, null, null, []);

    public void AddSession(int bookId, DateOnly? started, DateOnly? finished) =>
        Data(bookId).Sessions.Add(new ReadingSession { Id = _nextSession++, BookId = bookId, StartedOn = started, FinishedOn = finished });

    public Task<ReadingInfo?> GetAsync(int bookId, CancellationToken cancellationToken = default)
    {
        if (books.All(b => b.Id != bookId))
        {
            return Task.FromResult<ReadingInfo?>(null);
        }

        var d = Data(bookId);
        return Task.FromResult<ReadingInfo?>(new ReadingInfo(d.Status, d.Rating, d.Notes,
            [.. d.Sessions.OrderByDescending(s => s.Id).Select(s => new SessionInfo(s.Id, s.StartedOn, s.FinishedOn, s.Abandoned))]));
    }

    public Task<bool> SetStatusAsync(int bookId, ReadStatus status, DateOnly today, CancellationToken cancellationToken = default)
    {
        LastToday = today;
        var d = Data(bookId);
        var before = d.Sessions.Count;
        ReadingLog.ChangeStatus(d.Sessions, status, today);
        foreach (var added in d.Sessions.Skip(before))
        {
            added.Id = _nextSession++;
        }

        _data[bookId] = d with { Status = status };
        Sync(bookId);
        return Task.FromResult(true);
    }

    public Task<bool> SetRatingAsync(int bookId, int? rating, CancellationToken cancellationToken = default)
    {
        _data[bookId] = Data(bookId) with { Rating = rating };
        Sync(bookId);
        return Task.FromResult(true);
    }

    public Task<bool> SetNotesAsync(int bookId, string? notes, CancellationToken cancellationToken = default)
    {
        _data[bookId] = Data(bookId) with { Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim() };
        return Task.FromResult(true);
    }

    public Task<bool> UpdateSessionAsync(int sessionId, DateOnly? startedOn, DateOnly? finishedOn, CancellationToken cancellationToken = default)
    {
        if (ReadingLog.DateError(startedOn, finishedOn) is { } error)
        {
            throw new ArgumentException(error);
        }

        var session = _data.Values.SelectMany(d => d.Sessions).Single(x => x.Id == sessionId);
        session.StartedOn = startedOn;
        session.FinishedOn = finishedOn;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteSessionAsync(int sessionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_data.Values.Any(d => d.Sessions.RemoveAll(x => x.Id == sessionId) > 0));

    private void Sync(int bookId)
    {
        var index = books.FindIndex(b => b.Id == bookId);
        books[index] = books[index] with { ReadStatus = Data(bookId).Status, Rating = Data(bookId).Rating };
    }
}

internal sealed class FakeReadingStats : IReadingStats
{
    public List<StatsBook> Books { get; } = [];

    public bool Fail { get; set; }

    public Task<IReadOnlyList<StatsBook>> LoadAsync(CancellationToken cancellationToken = default) =>
        Fail ? throw new InvalidOperationException("database is locked") : Task.FromResult<IReadOnlyList<StatsBook>>([.. Books]);
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
        FakeLauncher? launcher = null,
        FakeDuplicates? duplicates = null,
        FakeReading? reading = null,
        FakeReadingStats? stats = null)
    {
        services.AddSingleton<IReadingStats>(stats ?? new FakeReadingStats());
        books ??= libraryBooks?.Books ?? [];
        services.AddSingleton<IReading>(reading ?? new FakeReading(books));
        services.AddSingleton<IDuplicates>(duplicates ?? new FakeDuplicates());
        services.AddSingleton<ILibraryBooks>(libraryBooks ?? new FakeBooks(books));
        services.AddSingleton<IFileLauncher>(launcher ?? new FakeLauncher());
        services.AddSingleton<ILibraryFolders>(new FakeFolders());
        services.AddSingleton<ILibraryScanner>(new FakeScanner());
        services.AddSingleton<IFolderPicker>(new FakePicker());
        services.AddSingleton<IAppPreferences>(preferences ?? new FakePreferences());
        services.AddSingleton<ISystemTheme>(systemTheme ?? new FakeSystemTheme());
        services.AddSingleton<ICoverUrls>(new FakeCoverUrls());
    }
}
