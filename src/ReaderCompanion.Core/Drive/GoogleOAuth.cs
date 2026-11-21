using System.Security.Cryptography;
using System.Text;

namespace ReaderCompanion.Core.Drive;

/// <summary>How much of the user's Google Drive the app may use. Each level is one OAuth scope.</summary>
public enum DriveAccess
{
    /// <summary>Only the files the user picks in Google's picker (<c>drive.file</c>).</summary>
    PickedFiles,

    /// <summary>The whole Drive, without changing anything (<c>drive.readonly</c>).</summary>
    WholeDriveReadOnly,

    /// <summary>The whole Drive, including moving, renaming and trashing (<c>drive</c>).</summary>
    WholeDrive,
}

/// <summary>One sign-in in progress: the URL to open and the secrets to check and finish it with.</summary>
public sealed record SignInRequest(DriveAccess Access, Uri AuthorizationUrl, string RedirectUri, string State, string CodeVerifier);

/// <summary>What Google sent back to the redirect address: a code to exchange, or an error.</summary>
public sealed record SignInAnswer(string? Code, string? Error, IReadOnlyList<string> PickedFileIds);

/// <summary>Google's OAuth for installed apps (loopback redirect with PKCE), without any network access.</summary>
public static class GoogleOAuth
{
    public const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    public const string RevokeEndpoint = "https://oauth2.googleapis.com/revoke";
    private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";

    public static string Scope(DriveAccess access) => "https://www.googleapis.com/auth/" + access switch
    {
        DriveAccess.PickedFiles => "drive.file",
        DriveAccess.WholeDriveReadOnly => "drive.readonly",
        _ => "drive",
    };

    /// <summary>A new sign-in. With <paramref name="pickFiles"/>, Google shows its file picker after the sign-in
    /// (only allowed with <see cref="DriveAccess.PickedFiles"/>).</summary>
    public static SignInRequest NewRequest(DriveAccess access, string clientId, string redirectUri, bool pickFiles = false)
    {
        if (pickFiles && access != DriveAccess.PickedFiles)
        {
            throw new ArgumentException("Google's picker only works with access to picked files.", nameof(pickFiles));
        }

        var state = Base64Url(RandomNumberGenerator.GetBytes(16));
        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        var parameters = new List<(string, string)>
        {
            ("client_id", clientId),
            ("redirect_uri", redirectUri),
            ("response_type", "code"),
            ("scope", Scope(access)),
            ("access_type", "offline"),
            ("prompt", "consent"),
            ("state", state),
            ("code_challenge", Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)))),
            ("code_challenge_method", "S256"),
        };
        if (pickFiles)
        {
            parameters.Add(("trigger_onepick", "true"));
            parameters.Add(("allow_multiple", "true"));
        }

        var query = string.Join('&', parameters.Select(p => $"{p.Item1}={Uri.EscapeDataString(p.Item2)}"));
        return new SignInRequest(access, new Uri($"{AuthorizationEndpoint}?{query}"), redirectUri, state, verifier);
    }

    /// <summary>Reads the redirect's query string. An answer for another sign-in is refused as
    /// <c>state_mismatch</c>.</summary>
    public static SignInAnswer ReadRedirect(SignInRequest request, string query)
    {
        var values = query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2))
            .GroupBy(p => p[0])
            .ToDictionary(g => g.Key, g => g.First() is [_, var v] ? Uri.UnescapeDataString(v.Replace('+', ' ')) : "");
        if (!values.TryGetValue("state", out var state) || state != request.State)
        {
            return new SignInAnswer(null, "state_mismatch", []);
        }

        var picked = values.TryGetValue("picked_file_ids", out var ids)
            ? ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : [];
        if (values.TryGetValue("error", out var error))
        {
            return new SignInAnswer(null, error, []);
        }

        return values.TryGetValue("code", out var code) ? new SignInAnswer(code, null, picked) : new SignInAnswer(null, "no_code", []);
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
