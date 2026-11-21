using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using ReaderCompanion.Core.Drive;

namespace ReaderCompanion.Data;

/// <summary>A file or folder on Drive. <see cref="Sha256"/> is Drive's checksum, when it has one.</summary>
internal sealed record DriveItem(string Id, string Name, bool IsFolder, long Size, DateTime ModifiedUtc, string? Sha256);

/// <summary>The few Drive API v3 calls the library needs. Every failure other than cancelling, or the link being lost,
/// is a <see cref="DriveUnavailableException"/>.</summary>
internal sealed class GoogleDriveFiles(IDriveAccount account, HttpClient http)
{
    private const string Api = "https://www.googleapis.com/drive/v3/files";
    private const string FolderType = "application/vnd.google-apps.folder";
    private const string Fields = "id,name,mimeType,size,modifiedTime,sha256Checksum";
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
            $"{Api}/{Uri.EscapeDataString(id)}?alt=media&supportsAllDrives=true", HttpCompletionOption.ResponseHeadersRead, cancellationToken);
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

    /// <summary>The answer's JSON, or null for 404.</summary>
    private async Task<JsonDocument?> GetJsonAsync(string url, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ListTimeout);
        try
        {
            using var response = await SendAsync(url, HttpCompletionOption.ResponseContentRead, timeout.Token);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            return response.IsSuccessStatusCode
                ? JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token))
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

    private async Task<HttpResponseMessage> SendAsync(string url, HttpCompletionOption completion, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await account.GetAccessTokenAsync(cancellationToken));
        try
        {
            return await http.SendAsync(request, completion, cancellationToken);
        }
        catch (HttpRequestException e)
        {
            throw new DriveUnavailableException("Google Drive could not be reached.", e);
        }
    }

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
            Text("sha256Checksum"));
    }
}
