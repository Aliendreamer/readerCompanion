using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ReaderCompanion.Core.Drive;

namespace ReaderCompanion.Data;

/// <summary>A file or folder on Drive. <see cref="Sha256"/> is Drive's checksum, when it has one.</summary>
internal sealed record DriveItem(
    string Id, string Name, bool IsFolder, long Size, DateTime ModifiedUtc, string? Sha256, IReadOnlyList<string> Parents);

/// <summary>The few Drive API v3 calls the library needs. Every failure other than cancelling, or the link being lost,
/// is a <see cref="DriveUnavailableException"/>.</summary>
internal sealed class GoogleDriveFiles(IDriveAccount account, HttpClient http)
{
    private const string Api = "https://www.googleapis.com/drive/v3/files";
    private const string FolderType = "application/vnd.google-apps.folder";
    private const string Fields = "id,name,mimeType,size,modifiedTime,sha256Checksum,parents";
    private static readonly TimeSpan ListTimeout = TimeSpan.FromSeconds(60);

    public async Task<IReadOnlyList<DriveItem>> ListChildrenAsync(string parentId, bool foldersOnly, CancellationToken cancellationToken)
    {
        var escaped = parentId.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("'", "\\'", StringComparison.Ordinal);
        var q = $"'{escaped}' in parents and trashed=false"
            + (foldersOnly ? $" and mimeType='{FolderType}'" : "");
        var items = new List<DriveItem>();
        string? page = null;
        do
        {
            var url = $"{Api}?q={Uri.EscapeDataString(q)}&fields={Uri.EscapeDataString($"nextPageToken,files({Fields})")}"
                + "&pageSize=1000&orderBy=name&supportsAllDrives=true&includeItemsFromAllDrives=true"
                + (page is null ? "" : $"&pageToken={Uri.EscapeDataString(page)}");
            using var json = await GetJsonAsync(url, cancellationToken)
                ?? throw new DriveUnavailableException("A Drive folder could not be found.");
            var root = json.RootElement;
            if (root.TryGetProperty("files", out var files))
            {
                items.AddRange(files.EnumerateArray().Select(Item));
            }

            page = root.TryGetProperty("nextPageToken", out var next) ? next.GetString() : null;
        }
        while (page is not null);

        return items;
    }

    /// <summary>The file, or null when it is gone or in Drive's trash.</summary>
    public async Task<DriveItem?> GetAsync(string id, CancellationToken cancellationToken)
    {
        using var json = await GetJsonAsync(
            $"{Api}/{Uri.EscapeDataString(id)}?fields={Uri.EscapeDataString(Fields + ",trashed")}&supportsAllDrives=true", cancellationToken);
        return json is null || (json.RootElement.TryGetProperty("trashed", out var trashed) && trashed.GetBoolean())
            ? null
            : Item(json.RootElement);
    }

    public async Task DownloadAsync(string id, Stream into, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            HttpMethod.Get, $"{Api}/{Uri.EscapeDataString(id)}?alt=media&supportsAllDrives=true", null,
            HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw Unavailable(response.StatusCode);
        }

        try
        {
            await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
            await content.CopyToAsync(into, cancellationToken);
        }
        catch (HttpRequestException e)
        {
            throw new DriveUnavailableException("The download from Google Drive was interrupted.", e);
        }
    }

    /// <summary>Bytes <paramref name="from"/> to <paramref name="to"/> (inclusive) of the file; fewer at its end.</summary>
    public async Task<byte[]> DownloadRangeAsync(string id, long from, long to, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ListTimeout);
        try
        {
            using var response = await SendAsync(
                HttpMethod.Get, $"{Api}/{Uri.EscapeDataString(id)}?alt=media&supportsAllDrives=true", null,
                HttpCompletionOption.ResponseContentRead, timeout.Token, new RangeHeaderValue(from, to));
            if (response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
            {
                return []; // past the end
            }

            if (!response.IsSuccessStatusCode)
            {
                throw Unavailable(response.StatusCode);
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(timeout.Token);
            if (response.StatusCode == HttpStatusCode.PartialContent)
            {
                return bytes;
            }

            // The whole file came back (no range support): keep the part asked for.
            return from >= bytes.Length ? [] : bytes[(int)from..(int)Math.Min(bytes.Length, to + 1)];
        }
        catch (OperationCanceledException e) when (!cancellationToken.IsCancellationRequested)
        {
            throw new DriveUnavailableException("Google Drive did not answer in time.", e);
        }
        catch (HttpRequestException e)
        {
            throw new DriveUnavailableException("The download from Google Drive was interrupted.", e);
        }
    }

    /// <summary>Puts the file in Drive's trash, from where the user can restore it.</summary>
    public async Task TrashAsync(string id, CancellationToken cancellationToken)
    {
        using var _ = await RequestJsonAsync(
            HttpMethod.Patch, $"{Api}/{Uri.EscapeDataString(id)}?supportsAllDrives=true", new { trashed = true }, cancellationToken);
    }

    /// <summary>Renames the file and moves it from <paramref name="fromParents"/> into <paramref name="toParent"/>.</summary>
    public async Task MoveAsync(string id, string name, string toParent, IReadOnlyList<string> fromParents, CancellationToken cancellationToken)
    {
        using var answer = await RequestJsonAsync(
            HttpMethod.Patch,
            $"{Api}/{Uri.EscapeDataString(id)}?addParents={Uri.EscapeDataString(toParent)}"
                + (fromParents.Count > 0 ? $"&removeParents={Uri.EscapeDataString(string.Join(',', fromParents))}" : "")
                + "&supportsAllDrives=true",
            new { name },
            cancellationToken) ?? throw new DriveUnavailableException("The file is no longer on Google Drive.");
    }

    /// <summary>Creates a folder and returns its id.</summary>
    public async Task<string> CreateFolderAsync(string name, string parentId, CancellationToken cancellationToken)
    {
        using var json = await RequestJsonAsync(
            HttpMethod.Post, $"{Api}?supportsAllDrives=true&fields=id", new { name, mimeType = FolderType, parents = new[] { parentId } }, cancellationToken);
        return json?.RootElement.TryGetProperty("id", out var id) == true && id.GetString() is { } folderId
            ? folderId
            : throw new DriveUnavailableException("Google Drive did not create the folder.");
    }

    private Task<JsonDocument?> GetJsonAsync(string url, CancellationToken cancellationToken) =>
        RequestJsonAsync(HttpMethod.Get, url, null, cancellationToken);

    /// <summary>The answer's JSON (empty for an empty answer), or null for 404.</summary>
    private async Task<JsonDocument?> RequestJsonAsync(HttpMethod method, string url, object? body, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ListTimeout);
        try
        {
            using var content = body is null ? null : JsonContent.Create(body);
            using var response = await SendAsync(method, url, content, HttpCompletionOption.ResponseContentRead, timeout.Token);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            return response.IsSuccessStatusCode
                ? JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token) is { Length: > 0 } text ? text : "{}")
                : throw Unavailable(response.StatusCode);
        }
        catch (OperationCanceledException e) when (!cancellationToken.IsCancellationRequested)
        {
            throw new DriveUnavailableException("Google Drive did not answer in time.", e);
        }
        catch (JsonException e)
        {
            throw new DriveUnavailableException("Google Drive gave an answer the app could not read.", e);
        }
    }

    /// <summary>Sends a request. A read Google answers with "too many requests" or a passing server error is sent
    /// again after a short wait (twice at most), as Google asks clients to.</summary>
    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string url, HttpContent? content, HttpCompletionOption completion, CancellationToken cancellationToken,
        RangeHeaderValue? range = null)
    {
        for (var attempt = 0; ; attempt++)
        {
            using var request = new HttpRequestMessage(method, url) { Content = content };
            request.Headers.Range = range;
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await account.GetAccessTokenAsync(cancellationToken));
            HttpResponseMessage response;
            try
            {
                response = await http.SendAsync(request, completion, cancellationToken);
            }
            catch (HttpRequestException e)
            {
                throw new DriveUnavailableException("Google Drive could not be reached.", e);
            }

            var passing = response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500;
            if (!passing || method != HttpMethod.Get || attempt == Retries)
            {
                return response;
            }

            var wait = response.Headers.RetryAfter?.Delta
                ?? TimeSpan.FromMilliseconds((250 << attempt) + Random.Shared.Next(250)); // 0.25-0.5 s, then 0.5-0.75 s
            response.Dispose();
            await Task.Delay(wait < MaxRetryWait ? wait : MaxRetryWait, cancellationToken);
        }
    }

    /// <summary>Two retries (waits of ~0.25-0.5 s, then ~0.5-0.75 s) ride out a moment of throttling; a longer
    /// outage then fails the scan clearly instead of stalling it.</summary>
    private const int Retries = 2;

    /// <summary>A Retry-After longer than this is not waited out: the scan fails and can be run again later.</summary>
    private static readonly TimeSpan MaxRetryWait = TimeSpan.FromSeconds(10);

    private static DriveUnavailableException Unavailable(HttpStatusCode status) => new(status switch
    {
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "Google Drive refused access. Check the access level and link Drive again.",
        _ => $"Google Drive answered {(int)status}.",
    });

    private static DriveItem Item(JsonElement e)
    {
        string? Text(string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        return new DriveItem(
            Text("id") ?? "",
            Text("name") ?? "",
            Text("mimeType") == FolderType,
            long.TryParse(Text("size"), NumberStyles.None, CultureInfo.InvariantCulture, out var size) ? size : 0,
            DateTime.TryParse(Text("modifiedTime"), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var modified)
                ? modified
                : default,
            Text("sha256Checksum"),
            e.TryGetProperty("parents", out var parents) && parents.ValueKind == JsonValueKind.Array
                ? [.. parents.EnumerateArray().Select(p => p.GetString() ?? "")]
                : []);
    }
}
