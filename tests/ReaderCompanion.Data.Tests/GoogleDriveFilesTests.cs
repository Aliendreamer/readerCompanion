using System.Net;
using System.Text;
using ReaderCompanion.Core.Drive;

namespace ReaderCompanion.Data.Tests;

public sealed class GoogleDriveFilesTests
{
    private readonly List<HttpRequestMessage> _requests = [];

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private GoogleDriveFiles Files(Func<HttpRequestMessage, HttpResponseMessage> answer) => new(
        new TestDriveAccount(),
        new HttpClient(new FakeHandler(request =>
        {
            _requests.Add(request);
            return answer(request);
        })));

    private static string Query(HttpRequestMessage request) => Uri.UnescapeDataString(request.RequestUri!.Query);

    [Fact]
    public async Task Children_AreListedPageByPage()
    {
        var files = Files(request => Query(request).Contains("pageToken=p2", StringComparison.Ordinal)
            ? Json("""{"files":[{"id":"b","name":"B.pdf","mimeType":"application/pdf","size":"20","modifiedTime":"2026-01-02T00:00:00Z"}]}""")
            : Json("""
                {"nextPageToken":"p2","files":[
                  {"id":"a","name":"A.epub","mimeType":"application/epub+zip","size":"10","modifiedTime":"2026-01-01T10:00:00.000Z","sha256Checksum":"abc"},
                  {"id":"f","name":"Sub","mimeType":"application/vnd.google-apps.folder"}]}
                """));

        var children = await files.ListChildrenAsync("folder-1", foldersOnly: false, Token);

        Assert.Equal(
            [
                new DriveItem("a", "A.epub", false, 10, new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc), "abc"),
                new DriveItem("f", "Sub", true, 0, default, null),
                new DriveItem("b", "B.pdf", false, 20, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), null),
            ],
            children);
        Assert.Equal(2, _requests.Count);
        Assert.Contains("q='folder-1' in parents and trashed=false", Query(_requests[0]), StringComparison.Ordinal);
        Assert.All(_requests, r => Assert.Equal("Bearer token-1", r.Headers.Authorization?.ToString()));
    }

    [Fact]
    public async Task FoldersOnly_AsksForFolders()
    {
        await Files(_ => Json("""{"files":[]}""")).ListChildrenAsync("root", foldersOnly: true, Token);

        Assert.Contains("mimeType='application/vnd.google-apps.folder'", Query(_requests[0]), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_GivesTheFile_OrNullWhenGoneOrTrashed()
    {
        var files = Files(request => request.RequestUri!.AbsolutePath switch
        {
            "/drive/v3/files/a" => Json("""{"id":"a","name":"A.epub","mimeType":"application/epub+zip","size":"10","modifiedTime":"2026-01-01T00:00:00Z"}"""),
            "/drive/v3/files/t" => Json("""{"id":"t","name":"T.epub","mimeType":"application/epub+zip","size":"1","trashed":true}"""),
            _ => Json("""{"error":{"code":404}}""", HttpStatusCode.NotFound),
        });

        Assert.Equal("A.epub", (await files.GetAsync("a", Token))?.Name);
        Assert.Null(await files.GetAsync("t", Token));
        Assert.Null(await files.GetAsync("gone", Token));
    }

    [Fact]
    public async Task Download_CopiesTheContent()
    {
        var files = Files(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3]) });
        using var into = new MemoryStream();

        await files.DownloadAsync("a", into, Token);

        Assert.Equal([1, 2, 3], into.ToArray());
        Assert.Contains("alt=media", Query(_requests[0]), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Failures_SayDriveCouldNotBeRead()
    {
        var files = Files(_ => Json("""{"error":{"code":500}}""", HttpStatusCode.InternalServerError));

        await Assert.ThrowsAsync<DriveUnavailableException>(() => files.ListChildrenAsync("x", false, Token));
        await Assert.ThrowsAsync<DriveUnavailableException>(() => files.DownloadAsync("x", Stream.Null, Token));
    }
}
