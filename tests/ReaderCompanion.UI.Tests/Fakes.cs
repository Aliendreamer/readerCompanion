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
    public Task<IReadOnlyList<BookListItem>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<BookListItem>>([.. books]);
}

internal static class TestServices
{
    /// <summary>Registers every service the app's pages and layout use, with fakes.</summary>
    public static void AddAppFakes(
        this IServiceCollection services,
        List<BookListItem>? books = null,
        FakePreferences? preferences = null,
        FakeSystemTheme? systemTheme = null)
    {
        services.AddSingleton<ILibraryBooks>(new FakeBooks(books ?? []));
        services.AddSingleton<ILibraryFolders>(new FakeFolders());
        services.AddSingleton<ILibraryScanner>(new FakeScanner());
        services.AddSingleton<IFolderPicker>(new FakePicker());
        services.AddSingleton<IAppPreferences>(preferences ?? new FakePreferences());
        services.AddSingleton<ISystemTheme>(systemTheme ?? new FakeSystemTheme());
        services.AddSingleton<ICoverUrls>(new FakeCoverUrls());
    }
}
