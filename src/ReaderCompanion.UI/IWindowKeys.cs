namespace ReaderCompanion.UI;

/// <summary>The window-level keys the frame handles (F11, Escape), wherever the focus is.</summary>
public interface IWindowKeys
{
    ValueTask<IAsyncDisposable> ListenAsync(Action<string> onKey);
}
