namespace ReaderCompanion.UI;

/// <summary>The system's light/dark preference as the web view reports it.</summary>
public interface ISystemTheme
{
    ValueTask<bool> PrefersDarkAsync();

    /// <summary>Calls <paramref name="onChange"/> whenever the system preference changes, until the returned
    /// subscription is disposed.</summary>
    ValueTask<IAsyncDisposable> ListenAsync(Action<bool> onChange);
}
