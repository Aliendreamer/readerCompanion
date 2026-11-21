using Microsoft.JSInterop;

namespace ReaderCompanion.UI;

/// <summary>Listens for F11 and Escape through <c>window.js</c>.</summary>
internal sealed class JsWindowKeys(IJSRuntime js) : IWindowKeys
{
    public async ValueTask<IAsyncDisposable> ListenAsync(Action<string> onKey)
    {
        var module = await js.InvokeAsync<IJSObjectReference>("import", "./_content/ReaderCompanion.UI/window.js");
        var listener = DotNetObjectReference.Create(new Listener(onKey));
        var handle = await module.InvokeAsync<IJSObjectReference>("listen", listener);
        return new JsSubscription<Listener>(handle, listener);
    }

    internal sealed class Listener(Action<string> onKey)
    {
        [JSInvokable]
        public void OnWindowKey(string key) => onKey(key);
    }
}
