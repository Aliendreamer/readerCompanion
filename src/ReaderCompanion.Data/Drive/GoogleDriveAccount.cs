using System.Net.Http.Headers;
using System.Text.Json;
using ReaderCompanion.Core.Drive;

namespace ReaderCompanion.Data;

/// <summary>The Google Drive link over plain HTTPS: sign-in, token refresh and revoke. The access token lives only
/// in memory; the refresh token is in <see cref="DriveTokenFile"/>. Link state changes only under one lock.</summary>
internal sealed class GoogleDriveAccount(
    GoogleClient? client, DriveTokenFile file, HttpClient http, IBrowserSignIn signIn, Func<DateTime> utcNow) : IDriveAccount
{
    private const string AboutUrl = "https://www.googleapis.com/drive/v3/about?fields=user(displayName,emailAddress)";
    private static readonly TimeSpan RefreshEarly = TimeSpan.FromMinutes(1);

    // The shared client has no timeout (downloads can be long); these small calls get their own.
    private static readonly TimeSpan CallTimeout = TimeSpan.FromSeconds(30);

    private readonly SemaphoreSlim _lock = new(1, 1);
    private DriveToken? _token = file.Load();
    private (string Value, DateTime ExpiresUtc)? _access;

    public bool IsConfigured => client is not null;

    public DriveLink? Link => _token is { } t ? new DriveLink(t.Access, t.AccountName, t.LinkedUtc) : null;

    public event Action? Changed;

    public async Task<DriveLink> LinkAsync(DriveAccess access, CancellationToken cancellationToken = default) =>
        (await SignInAsync(access, pickFiles: false, cancellationToken)).Link;

    public async Task<IReadOnlyList<string>> PickFilesAsync(CancellationToken cancellationToken = default)
    {
        if (Link?.Access != DriveAccess.PickedFiles)
        {
            throw new InvalidOperationException("Picking books needs the Google Drive access “Only books I pick”.");
        }

        return (await SignInAsync(DriveAccess.PickedFiles, pickFiles: true, cancellationToken)).Picked;
    }

    /// <summary>Signs in through the browser (with Google's picker when <paramref name="pickFiles"/>) and keeps the
    /// new link.</summary>
    private async Task<(DriveLink Link, IReadOnlyList<string> Picked)> SignInAsync(
        DriveAccess access, bool pickFiles, CancellationToken cancellationToken)
    {
        var app = RequireClient();
        var (request, answer) = await signIn.SignInAsync(
            redirectUri => GoogleOAuth.NewRequest(access, app.ClientId, redirectUri, pickFiles), cancellationToken);
        if (answer.Code is not { } code)
        {
            throw new DriveSignInException(answer.Error == "access_denied"
                ? "Access to Google Drive was not given."
                : $"Google did not link Drive ({answer.Error}).");
        }

        var tokens = await PostTokenAsync(
            new()
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["code_verifier"] = request.CodeVerifier,
                ["redirect_uri"] = request.RedirectUri,
                ["client_id"] = app.ClientId,
                ["client_secret"] = app.ClientSecret,
            },
            cancellationToken);
        if (tokens.AccessToken is not { } accessToken || tokens.RefreshToken is not { } refreshToken)
        {
            throw new DriveSignInException($"Google did not link Drive ({tokens.Error ?? "no refresh token"}).");
        }

        // Google lets the user untick a permission on its consent screen.
        if (tokens.Scope is { } granted && !granted.Split(' ').Contains(GoogleOAuth.Scope(access)))
        {
            throw new DriveSignInException("Access to Google Drive was not given.");
        }

        var token = new DriveToken(refreshToken, access, await AccountNameAsync(accessToken, cancellationToken), utcNow());
        await _lock.WaitAsync(cancellationToken);
        try
        {
            file.Save(token);
            _token = token;
            Cache(tokens);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new DriveSignInException($"The link could not be saved in the data folder: {e.Message}");
        }
        finally
        {
            _lock.Release();
        }

        Changed?.Invoke();
        return (Link!, answer.PickedFileIds);
    }

    public async Task UnlinkAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_token is { } token)
            {
                try
                {
                    (await PostFormAsync(GoogleOAuth.RevokeEndpoint, new() { ["token"] = token.RefreshToken }, cancellationToken)).Dispose();
                }
                catch (Exception e) when (e is HttpRequestException || (e is OperationCanceledException && !cancellationToken.IsCancellationRequested))
                {
                    // Offline: the token is forgotten here anyway; the user can also remove access in their Google account.
                }
            }

            Forget();
        }
        finally
        {
            _lock.Release();
        }

        Changed?.Invoke();
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        var lost = false;
        try
        {
            if (_access is { } access && utcNow() < access.ExpiresUtc - RefreshEarly)
            {
                return access.Value;
            }

            var token = _token ?? throw new InvalidOperationException("Google Drive is not linked.");
            var app = RequireClient();
            var tokens = await PostTokenAsync(
                new()
                {
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = token.RefreshToken,
                    ["client_id"] = app.ClientId,
                    ["client_secret"] = app.ClientSecret,
                },
                cancellationToken);
            if (tokens.Error == "invalid_grant")
            {
                Forget();
                lost = true;
                throw new DriveLinkLostException();
            }

            return Cache(tokens) ?? throw new HttpRequestException($"Google did not give an access token ({tokens.Error ?? "no token"}).");
        }
        finally
        {
            _lock.Release();
            if (lost)
            {
                Changed?.Invoke();
            }
        }
    }

    private GoogleClient RequireClient() => client ?? throw new InvalidOperationException("Google Drive is not set up in this build.");

    /// <summary>Keeps the answer's access token for its lifetime; returns it, or null when there was none.</summary>
    private string? Cache(TokenAnswer tokens)
    {
        if (tokens.AccessToken is { } value)
        {
            _access = (value, utcNow().AddSeconds(tokens.ExpiresIn));
        }

        return tokens.AccessToken;
    }

    /// <summary>Call under the lock; the caller raises <see cref="Changed"/> after releasing it.</summary>
    private void Forget()
    {
        _token = null;
        _access = null;
        try
        {
            file.Delete();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Unlinked for this session anyway; a file left behind is rejected by Google again next time.
        }
    }

    private async Task<HttpResponseMessage> PostFormAsync(string url, Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        using var timeout = Timeout(cancellationToken);
        using var content = new FormUrlEncodedContent(form);
        var response = await http.PostAsync(new Uri(url), content, timeout.Token);
        await response.Content.LoadIntoBufferAsync(timeout.Token); // read within the timeout
        return response;
    }

    /// <summary>Google's token answer; an OAuth error comes back as <see cref="TokenAnswer.Error"/>, any other failure
    /// throws <see cref="HttpRequestException"/>.</summary>
    private async Task<TokenAnswer> PostTokenAsync(Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        using var response = await PostFormAsync(GoogleOAuth.TokenEndpoint, form, cancellationToken);
        TokenAnswer? answer = null;
        try
        {
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = json.RootElement;
            string? Text(string name) => root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            answer = new TokenAnswer(
                Text("access_token"), Text("refresh_token"),
                root.TryGetProperty("expires_in", out var e) && e.TryGetInt32(out var seconds) ? seconds : 0,
                Text("error"),
                Text("scope"));
        }
        catch (JsonException)
        {
            // Not Google's JSON: reported below as a failed call.
        }

        return answer is not null && (answer.Error is not null || response.IsSuccessStatusCode)
            ? answer
            : throw new HttpRequestException($"Google answered {(int)response.StatusCode}.");
    }

    /// <summary>"Name (email)" from Drive, or null when Drive will not say.</summary>
    private async Task<string?> AccountNameAsync(string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            using var timeout = Timeout(cancellationToken);
            using var request = new HttpRequestMessage(HttpMethod.Get, AboutUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var response = await http.SendAsync(request, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token));
            if (!json.RootElement.TryGetProperty("user", out var user))
            {
                return null;
            }

            string? Text(string name) => user.TryGetProperty(name, out var v) && v.GetString() is { Length: > 0 } s ? s : null;
            var (displayName, email) = (Text("displayName"), Text("emailAddress"));
            return displayName is not null && email is not null ? $"{displayName} ({email})" : displayName ?? email;
        }
        catch (Exception e) when (e is HttpRequestException or JsonException or InvalidOperationException
            || (e is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            return null;
        }
    }

    private static CancellationTokenSource Timeout(CancellationToken cancellationToken)
    {
        var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(CallTimeout);
        return timeout;
    }

    private sealed record TokenAnswer(string? AccessToken, string? RefreshToken, int ExpiresIn, string? Error, string? Scope);
}
