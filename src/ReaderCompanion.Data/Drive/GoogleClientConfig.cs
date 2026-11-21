using System.Text.Json;

namespace ReaderCompanion.Data;

/// <summary>The app's OAuth client, as Google's "Desktop app" credentials (not secret for installed apps).</summary>
internal sealed record GoogleClient(string ClientId, string ClientSecret);

internal static class GoogleClientConfig
{
    /// <summary>Google's download, renamed; committed encrypted with git-crypt (unusable until unlocked). The build
    /// compiles it into the program scrambled; a copy in the data folder overrides that.</summary>
    public const string FileName = "google-oauth-client.json";

    /// <summary>The client from a usable file in <paramref name="dataFolder"/> (someone's own Google client), else the one
    /// built into the program (<see cref="ReaderCompanion.Client.BuiltInClient"/>), else null.</summary>
    public static GoogleClient? Resolve(string dataFolder, Func<GoogleClient?> builtIn) => TryLoad(dataFolder) ?? builtIn();

    /// <summary>The client built into the program, if the build had one.</summary>
    public static GoogleClient? BuiltIn() =>
        ReaderCompanion.Client.BuiltInClient.TryGet() is var (id, secret) ? new GoogleClient(id, secret) : null;

    /// <summary>The client from the first of <paramref name="folders"/> holding a usable file, or null.</summary>
    public static GoogleClient? TryLoad(params string[] folders)
    {
        foreach (var folder in folders)
        {
            var path = Path.Combine(folder, FileName);
            if (File.Exists(path) && Read(path) is { } client)
            {
                return client;
            }
        }

        return null;
    }

    private static GoogleClient? Read(string path)
    {
        try
        {
            using var json = JsonDocument.Parse(File.ReadAllText(path));
            return json.RootElement.TryGetProperty("installed", out var installed)
                && installed.TryGetProperty("client_id", out var id) && id.GetString() is { Length: > 0 } clientId
                && installed.TryGetProperty("client_secret", out var secret) && secret.GetString() is { Length: > 0 } clientSecret
                ? new GoogleClient(clientId, clientSecret)
                : null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
        {
            return null;
        }
    }
}
