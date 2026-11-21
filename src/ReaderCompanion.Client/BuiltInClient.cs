namespace ReaderCompanion.Client;

/// <summary>The app's Google OAuth client, compiled into this assembly in scrambled form when the build had
/// <c>google-oauth-client.json</c> (see the project file). Unscrambled only in memory, never written anywhere.</summary>
public static class BuiltInClient
{
    /// <summary>The client ID and secret, or null when the build had no client file.</summary>
    public static (string Id, string Secret)? TryGet() =>
        ClientData.Key is { } key && ClientData.Id is { } id && ClientData.Secret is { } secret
            ? (Scrambler.Unscramble(id, key), Scrambler.Unscramble(secret, key))
            : null;
}
