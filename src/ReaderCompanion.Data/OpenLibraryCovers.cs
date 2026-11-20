using System.Net;
using Microsoft.EntityFrameworkCore;
using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Metadata;

namespace ReaderCompanion.Data;

/// <summary>Covers from covers.openlibrary.org by ISBN, one request per second (their limits), saved in the covers
/// folder as <c>ol-{isbn}.jpg</c> or <c>.png</c>.</summary>
internal sealed class OpenLibraryCovers(
    IDbContextFactory<LibraryDbContext> contexts,
    LibraryDataPaths paths,
    HttpClient http,
    Func<TimeSpan, CancellationToken, Task> pause) : ICoverFetcher
{
    private const long MaxBytes = 5 * 1024 * 1024;

    public async Task<CoverFetchResult> FetchMissingAsync(IProgress<(int Done, int Total)>? progress, CancellationToken cancellationToken = default)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        // One lookup per ISBN; every book with it gets the cover.
        var wanting = (await db.Books.Where(b => b.CoverFileName == null && b.Isbn != null).Select(b => new { b.Id, b.Isbn }).ToListAsync(cancellationToken))
            .Select(b => (b.Id, Isbn: MatchKeys.Isbn(b.Isbn)))
            .Where(b => b.Isbn is not null)
            .GroupBy(b => b.Isbn!, b => b.Id)
            .ToList();

        int found = 0, failed = 0;
        for (var i = 0; i < wanting.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested(); // Stop takes effect before the next lookup
            if (i > 0)
            {
                await pause(TimeSpan.FromSeconds(1), cancellationToken);
            }

            var ids = wanting[i].ToList();
            var (outcome, fileName) = await DownloadAsync(wanting[i].Key, cancellationToken);
            if (outcome == Outcome.Found)
            {
                await db.Books.Where(b => ids.Contains(b.Id)).ExecuteUpdateAsync(u => u.SetProperty(b => b.CoverFileName, fileName), cancellationToken);
                found++;
            }
            else if (outcome == Outcome.Failed)
            {
                failed++;
            }

            progress?.Report((i + 1, wanting.Count));
        }

        return new CoverFetchResult(wanting.Count, found, failed);
    }

    /// <summary>The body, or null when it is longer than <paramref name="max"/> bytes (reading stops there).</summary>
    private static async Task<byte[]?> ReadAtMostAsync(HttpContent content, long max, CancellationToken cancellationToken)
    {
        await using var body = await content.ReadAsStreamAsync(cancellationToken);
        using var copy = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = await body.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (copy.Length + read > max)
            {
                return null;
            }

            copy.Write(buffer, 0, read);
        }

        return copy.ToArray();
    }

    private enum Outcome
    {
        Found,
        NoCover,
        Failed,
    }

    private async Task<(Outcome Outcome, string? FileName)> DownloadAsync(string isbn, CancellationToken cancellationToken)
    {
        try
        {
            // One limit for the whole lookup, body included (the client's own timeout stops at the headers).
            using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            limit.CancelAfter(TimeSpan.FromSeconds(20));
            using var response = await http.GetAsync(
                $"https://covers.openlibrary.org/b/isbn/{isbn}-L.jpg?default=false", HttpCompletionOption.ResponseHeadersRead, limit.Token);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return (Outcome.NoCover, null);
            }

            // The size is checked before downloading, so an odd answer cannot fill memory.
            if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength is > MaxBytes)
            {
                return (Outcome.Failed, null);
            }

            var bytes = await ReadAtMostAsync(response.Content, MaxBytes, limit.Token);
            return bytes is not null && CoverImage.TryFrom(bytes) is { } cover
                && CoverFiles.Save(paths.CoversFolder, $"ol-{isbn}", cover) is { } fileName
                ? (Outcome.Found, fileName)
                : (Outcome.Failed, null);
        }
        catch (Exception e) when (e is HttpRequestException or IOException or UnauthorizedAccessException
            || (e is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            return (Outcome.Failed, null); // offline, a timeout, or the covers folder cannot be written
        }
    }
}
