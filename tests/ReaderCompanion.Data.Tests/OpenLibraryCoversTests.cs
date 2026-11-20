using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReaderCompanion.Core.Library;

namespace ReaderCompanion.Data.Tests;

public sealed class OpenLibraryCoversTests : IDisposable
{
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3];
    private readonly TempLibrary _library = new();
    private readonly List<string> _asked = [];

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private OpenLibraryCovers Fetcher(Func<string, HttpResponseMessage> answer) => new(
        _library.Services.GetRequiredService<IDbContextFactory<LibraryDbContext>>(),
        _library.Services.GetRequiredService<LibraryDataPaths>(),
        new HttpClient(new FakeHandler(request =>
        {
            _asked.Add(request.RequestUri!.ToString());
            return answer(request.RequestUri.ToString());
        })),
        (_, _) => Task.CompletedTask);

    private int Add(string title, string? isbn, string? cover = null)
    {
        using var db = _library.NewContext();
        var book = new Book { Title = title, Isbn = isbn, CoverFileName = cover };
        db.Books.Add(book);
        db.SaveChanges();
        return book.Id;
    }

    private Book Load(int id)
    {
        using var db = _library.NewContext();
        return db.Books.AsNoTracking().Single(b => b.Id == id);
    }

    [Fact]
    public async Task FoundCovers_AreSavedAndUsed_MissingOnesLeftAlone_OnlyBooksThatNeedOneAreAsked()
    {
        var found = Add("Dune", "978-0-441-17271-9");
        var missing = Add("Mort", "9780552131063");
        Add("No ISBN", null);
        Add("Has cover", "9780000000002", "own.jpg");

        var result = await Fetcher(url => url.Contains("9780441172719", StringComparison.Ordinal)
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Jpeg) }
            : new HttpResponseMessage(HttpStatusCode.NotFound)).FetchMissingAsync(null, Token);

        Assert.Equal((2, 1, 0), (result.Looked, result.Found, result.Failed));
        Assert.Equal(2, _asked.Count);
        Assert.All(_asked, url => Assert.StartsWith("https://covers.openlibrary.org/b/isbn/", url, StringComparison.Ordinal));
        Assert.Equal("ol-9780441172719.jpg", Load(found).CoverFileName);
        Assert.Equal(Jpeg, File.ReadAllBytes(Path.Combine(_library.Services.GetRequiredService<LibraryDataPaths>().CoversFolder, "ol-9780441172719.jpg")));
        Assert.Null(Load(missing).CoverFileName);
    }

    [Fact]
    public async Task NotAnImage_OrAnError_IsCountedAsFailed()
    {
        var book = Add("Dune", "9780441172719");
        Add("Mort", "9780552131063");

        var result = await Fetcher(url => url.Contains("9780441172719", StringComparison.Ordinal)
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<html>oops</html>") }
            : new HttpResponseMessage(HttpStatusCode.InternalServerError)).FetchMissingAsync(null, Token);

        Assert.Equal((2, 0, 2), (result.Looked, result.Found, result.Failed));
        Assert.Null(Load(book).CoverFileName);
    }

    [Fact]
    public async Task Cancelling_Stops()
    {
        Add("Dune", "9780441172719");
        Add("Mort", "9780552131063");
        using var stop = new CancellationTokenSource();

        var fetch = Fetcher(_ =>
        {
            stop.Cancel();
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }).FetchMissingAsync(null, stop.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fetch);
        Assert.Single(_asked);
    }

    public void Dispose() => _library.Dispose();

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(answer(request));
    }
}
