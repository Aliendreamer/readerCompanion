using ReaderCompanion.Core.Drive;

namespace ReaderCompanion.Data.Tests;

public sealed class LoopbackSignInTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static SignInRequest NewRequest(string redirectUri) =>
        GoogleOAuth.NewRequest(DriveAccess.PickedFiles, "client", redirectUri);

    [Fact]
    public async Task TheBrowsersRedirect_IsReceived_AndTheTabToldItCanClose()
    {
        var browser = new Browser(async url =>
        {
            var query = url.Query.TrimStart('?').Split('&').Select(p => p.Split('=', 2))
                .ToDictionary(p => p[0], p => Uri.UnescapeDataString(p[1]));
            using var http = new HttpClient();
            (await http.GetAsync(new Uri(query["redirect_uri"] + "favicon.ico"))).Dispose(); // browsers ask for this too
            (await http.GetAsync(new Uri(query["redirect_uri"] + "?code=evil&state=other"))).Dispose(); // not this sign-in's
            return await http.GetStringAsync(new Uri($"{query["redirect_uri"]}?code=c1&state={query["state"]}"));
        });

        var (request, answer) = await new LoopbackSignIn(browser, TimeSpan.FromSeconds(30)).SignInAsync(NewRequest, Token);

        Assert.StartsWith("http://127.0.0.1:", request.RedirectUri, StringComparison.Ordinal);
        Assert.Equal("c1", answer.Code);
        Assert.Contains("close this tab", await browser.Page!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Cancelling_StopsWaiting()
    {
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(Token);
        var browser = new Browser(_ =>
        {
            stop.CancelAfter(50);
            return Task.FromResult("");
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new LoopbackSignIn(browser, TimeSpan.FromSeconds(30)).SignInAsync(NewRequest, stop.Token));
    }

    [Fact]
    public async Task NoAnswerInTime_IsASignInFailure()
    {
        var error = await Assert.ThrowsAsync<DriveSignInException>(() =>
            new LoopbackSignIn(new Browser(_ => Task.FromResult("")), TimeSpan.FromMilliseconds(100)).SignInAsync(NewRequest, Token));

        Assert.Contains("did not finish", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NoBrowser_IsASignInFailure()
    {
        var error = await Assert.ThrowsAsync<DriveSignInException>(() =>
            new LoopbackSignIn(new Browser(null), TimeSpan.FromSeconds(30)).SignInAsync(NewRequest, Token));

        Assert.Contains("browser", error.Message, StringComparison.Ordinal);
    }

    /// <summary>Plays the user's browser; null means no browser could be opened.</summary>
    private sealed class Browser(Func<Uri, Task<string>>? visit) : IWebBrowser
    {
        public Task<string>? Page { get; private set; }

        public bool Open(Uri url)
        {
            Page = visit is null ? null : Task.Run(() => visit(url));
            return visit is not null;
        }
    }
}
