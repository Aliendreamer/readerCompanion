using Microsoft.JSInterop;

namespace ReaderCompanion.UI;

/// <summary>Reads <c>prefers-color-scheme</c> through <c>theme.js</c>.</summary>
internal sealed class JsSystemTheme(IJSRuntime js) : ISystemTheme
{
    private IJSObjectReference? _module;

    public async ValueTask<bool> PrefersDarkAsync() => await (await ModuleAsync()).InvokeAsync<bool>("prefersDark");

    public async ValueTask<IAsyncDisposable> ListenAsync(Action<bool> onChange)
    {
        var listener = DotNetObjectReference.Create(new Listener(onChange));
        var handle = await (await ModuleAsync()).InvokeAsync<IJSObjectReference>("listen", listener);
        return new JsSubscription<Listener>(handle, listener);
    }

    // Not cached until it succeeds, so a failed import is retried next time.
    private async ValueTask<IJSObjectReference> ModuleAsync() =>
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", "./_content/ReaderCompanion.UI/theme.js");

    internal sealed class Listener(Action<bool> onChange)
    {
        [JSInvokable]
        public void OnSystemThemeChanged(bool prefersDark) => onChange(prefersDark);
    }
}
