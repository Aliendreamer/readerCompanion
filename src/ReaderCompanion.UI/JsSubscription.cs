using Microsoft.JSInterop;

namespace ReaderCompanion.UI;

/// <summary>A JS listener handle plus the .NET object it calls back; disposing removes the listener.</summary>
internal sealed class JsSubscription<T>(IJSObjectReference handle, DotNetObjectReference<T> listener) : IAsyncDisposable
    where T : class
{
    public async ValueTask DisposeAsync()
    {
        try
        {
            await handle.InvokeVoidAsync("dispose");
            await handle.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // The window is closing; its listeners go with it.
        }
        finally
        {
            listener.Dispose();
        }
    }
}
