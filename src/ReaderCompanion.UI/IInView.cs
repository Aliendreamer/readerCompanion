using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ReaderCompanion.UI;

/// <summary>Tells when an element comes near the visible part of the page (for showing more of a long list).</summary>
public interface IInView
{
    /// <summary>Calls <paramref name="onInView"/> whenever <paramref name="element"/> is within about a screen of the
    /// view, also right away if it already is; disposing stops watching.</summary>
    ValueTask<IAsyncDisposable> WatchAsync(ElementReference element, Action onInView);
}

/// <summary>Watches through <c>inview.js</c> (an IntersectionObserver).</summary>
internal sealed class JsInView(IJSRuntime js) : IInView
{
    private IJSObjectReference? _module;

    public async ValueTask<IAsyncDisposable> WatchAsync(ElementReference element, Action onInView)
    {
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", "./_content/ReaderCompanion.UI/inview.js");
        var listener = DotNetObjectReference.Create(new Listener(onInView));
        var handle = await _module.InvokeAsync<IJSObjectReference>("watch", element, listener);
        return new JsSubscription<Listener>(handle, listener);
    }

    internal sealed class Listener(Action onInView)
    {
        [JSInvokable]
        public void OnInView() => onInView();
    }
}
