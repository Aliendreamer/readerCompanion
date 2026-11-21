using System.Net;
using System.Net.Sockets;
using System.Text;
using ReaderCompanion.Core.Drive;

namespace ReaderCompanion.Data;

/// <summary>Runs a sign-in in the user's browser and returns Google's answer.</summary>
internal interface IBrowserSignIn
{
    /// <summary>Makes the request for a redirect address (<paramref name="newRequest"/>), opens it in the browser and
    /// waits for the redirect.</summary>
    Task<(SignInRequest Request, SignInAnswer Answer)> SignInAsync(
        Func<string, SignInRequest> newRequest, CancellationToken cancellationToken);
}

/// <summary>Receives Google's redirect on <c>http://127.0.0.1:&lt;free port&gt;/</c>, as Google asks of desktop
/// apps. Only the request carrying this sign-in's state is taken; anything else (a favicon, a stray request) gets a
/// 404.</summary>
internal sealed class LoopbackSignIn(IWebBrowser browser, TimeSpan timeout) : IBrowserSignIn
{
    private const string Page = """
        <!doctype html><html><head><meta charset="utf-8"><title>readerCompanion</title></head>
        <body style="font-family:sans-serif;margin:3rem">
        <h1>readerCompanion</h1><p>You can close this tab and go back to the app.</p></body></html>
        """;

    public async Task<(SignInRequest Request, SignInAnswer Answer)> SignInAsync(
        Func<string, SignInRequest> newRequest, CancellationToken cancellationToken)
    {
        var redirectUri = $"http://127.0.0.1:{FreePort()}/";
        var request = newRequest(redirectUri);
        using var listener = new HttpListener();
        listener.Prefixes.Add(redirectUri);
        listener.Start();

        if (!browser.Open(request.AuthorizationUrl))
        {
            throw new DriveSignInException("The browser could not be opened to sign in to Google.");
        }

        using var waiting = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        waiting.CancelAfter(timeout);
        using var stop = waiting.Token.Register(listener.Stop);
        try
        {
            while (true)
            {
                var context = await listener.GetContextAsync();
                var answer = GoogleOAuth.ReadRedirect(request, context.Request.Url?.Query ?? "");
                if (context.Request.Url?.AbsolutePath != "/" || answer.Error == "state_mismatch") // not this sign-in's answer
                {
                    context.Response.StatusCode = 404;
                    context.Response.Close();
                    continue;
                }

                var page = Encoding.UTF8.GetBytes(Page);
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.OutputStream.WriteAsync(page, CancellationToken.None);
                context.Response.Close();
                return (request, answer);
            }
        }
        catch (Exception e) when (waiting.IsCancellationRequested && e is HttpListenerException or ObjectDisposedException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new DriveSignInException("Signing in to Google did not finish in time. Try again.");
        }
    }

    private static int FreePort()
    {
        using var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        return ((IPEndPoint)probe.LocalEndpoint).Port;
    }
}
