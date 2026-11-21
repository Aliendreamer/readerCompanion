using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ReaderCompanion.Core.Drive;

namespace ReaderCompanion.Data.Tests;

/// <summary>A small Google Drive in memory, answering the Drive API v3 requests the app makes.</summary>
internal sealed class FakeDrive
{
    private const string FolderType = "application/vnd.google-apps.folder";
    private int _nextId;

    public Dictionary<string, Item> Items { get; } = new() { ["root"] = new Item("root", "My Drive", null, null) };

    public List<string> Downloads { get; } = [];

    /// <summary>When false, Drive's checksum is left out of answers, as for some files.</summary>
    public bool GivesChecksums { get; set; } = true;

    public sealed record Item(string Id, string Name, string? Parent, byte[]? Content, bool Trashed = false)
    {
        public bool IsFolder => Content is null;
    }

    public string Folder(string name, string parent = "root") => Add(name, parent, null);

    public string File(string name, string parent, byte[] content) => Add(name, parent, content);

    public void Rename(string id, string name, string? parent = null) =>
        Items[id] = Items[id] with { Name = name, Parent = parent ?? Items[id].Parent };

    public void Delete(string id) => Items.Remove(id);

    public static string Sha256(byte[] content) => Convert.ToHexStringLower(SHA256.HashData(content));

    public HttpClient Client() => new(new FakeHandler(Answer));

    private string Add(string name, string parent, byte[]? content)
    {
        var id = $"id{++_nextId}";
        Items[id] = new Item(id, name, parent, content);
        return id;
    }

    /// <summary>Folders created through the API, by name.</summary>
    public List<string> CreatedFolders { get; } = [];

    /// <summary>When false, every change is refused, as with read-only access.</summary>
    public bool AllowsWrites { get; set; } = true;

    /// <summary>The path of an item from My Drive, as "Books/Fantasy/x.epub".</summary>
    public string PathOf(string id)
    {
        var item = Items[id];
        return item.Parent is null or "root" ? item.Name : $"{PathOf(item.Parent)}/{item.Name}";
    }

    private HttpResponseMessage Answer(HttpRequestMessage request)
    {
        var uri = request.RequestUri!;
        var query = Uri.UnescapeDataString(uri.Query);
        var path = uri.AbsolutePath;
        if (request.Method != HttpMethod.Get)
        {
            return Write(request, path, query);
        }

        if (path == "/drive/v3/files")
        {
            var parent = query.Split("q='")[1].Split('\'')[0];
            if (!Items.ContainsKey(parent))
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            var children = Items.Values
                .Where(i => i.Parent == parent && !i.Trashed && (!query.Contains(FolderType, StringComparison.Ordinal) || i.IsFolder))
                .OrderBy(i => i.Name, StringComparer.Ordinal)
                .Select(Json);
            return Ok(new { files = children });
        }

        var id = path["/drive/v3/files/".Length..];
        if (!Items.TryGetValue(id, out var item))
        {
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        if (query.Contains("alt=media", StringComparison.Ordinal))
        {
            Downloads.Add(id);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(item.Content ?? []) };
        }

        return Ok(Json(item));
    }

    private HttpResponseMessage Write(HttpRequestMessage request, string path, string query)
    {
        if (!AllowsWrites)
        {
            return new HttpResponseMessage(HttpStatusCode.Forbidden);
        }

        using var body = JsonDocument.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());
        var json = body.RootElement;
        if (request.Method == HttpMethod.Post)
        {
            var name = json.GetProperty("name").GetString()!;
            CreatedFolders.Add(name);
            return Ok(new { id = Folder(name, json.GetProperty("parents")[0].GetString()!) });
        }

        var id = path["/drive/v3/files/".Length..];
        if (!Items.TryGetValue(id, out var item))
        {
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        if (json.TryGetProperty("trashed", out var trashed))
        {
            item = item with { Trashed = trashed.GetBoolean() };
        }

        if (json.TryGetProperty("name", out var newName))
        {
            item = item with { Name = newName.GetString()! };
        }

        var parameters = query.TrimStart('?').Split('&').Select(p => p.Split('=', 2)).ToDictionary(p => p[0], p => p.ElementAtOrDefault(1));
        if (parameters.TryGetValue("addParents", out var parent) && parent is { Length: > 0 })
        {
            item = item with { Parent = parent };
        }

        Items[id] = item;
        return Ok(new { id });
    }

    private Dictionary<string, object> Json(Item item)
    {
        var json = new Dictionary<string, object>
        {
            ["id"] = item.Id,
            ["name"] = item.Name,
            ["mimeType"] = item.IsFolder ? FolderType : "application/octet-stream",
            ["modifiedTime"] = "2026-01-01T00:00:00Z",
            ["trashed"] = item.Trashed,
            ["parents"] = item.Parent is null ? Array.Empty<string>() : new[] { item.Parent },
        };
        if (item.Content is { } content)
        {
            json["size"] = content.Length.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (GivesChecksums)
            {
                json["sha256Checksum"] = Sha256(content);
            }
        }

        return json;
    }

    private static HttpResponseMessage Ok(object body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") };
}

/// <summary>A linked Drive account that hands out a token and plays Google's picker.</summary>
internal sealed class TestDriveAccount(DriveAccess access = DriveAccess.WholeDrive) : IDriveAccount
{
    public bool IsConfigured => true;

    public DriveLink? Link { get; set; } = new(access, null, DateTime.UtcNow);

    /// <summary>What the next picker returns.</summary>
    public List<string> Picks { get; } = [];

    public event Action? Changed { add { } remove { } }

    public Task<DriveLink> LinkAsync(DriveAccess access, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<IReadOnlyList<string>> PickFilesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>([.. Picks]);

    public Task UnlinkAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
        Link is null ? throw new InvalidOperationException("Google Drive is not linked.") : Task.FromResult("token-1");
}
