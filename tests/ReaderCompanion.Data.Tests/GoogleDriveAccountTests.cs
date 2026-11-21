using System.Net;
using System.Text;
using ReaderCompanion.Core.Drive;

namespace ReaderCompanion.Data.Tests;

public sealed class GoogleDriveAccountTests : IDisposable
{
    private static readonly GoogleClient Client = new("client-1", "secret-1");
    private readonly string _folder = Directory.CreateTempSubdirectory("rc-drive-").FullName;
    private readonly List<(string Url, string? Auth, string Body)> _requests = [];
    private DateTime _now = new(2026, 11, 21, 10, 0, 0, DateTimeKind.Utc);
    private Func<HttpRequestMessage, HttpResponseMessage> _answer;
    private SignInAnswer _signIn = new("code-1", null, []);

    public GoogleDriveAccountTests() => _answer = Google();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private string TokenPath => Path.Combine(_folder, "drive-token.json");

    private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    /// <summary>A Google that issues at1 for the code, at2, at3… on refresh, and knows the user as Ana.</summary>
    private static Func<HttpRequestMessage, HttpResponseMessage> Google()
    {
        var refreshes = 1;
        return request => request.RequestUri!.ToString() switch
        {
            GoogleOAuth.TokenEndpoint when Body(request).Contains("grant_type=authorization_code", StringComparison.Ordinal) =>
                Json("""{"access_token":"at1","expires_in":3600,"refresh_token":"rt1","token_type":"Bearer"}"""),
            GoogleOAuth.TokenEndpoint => Json($$"""{"access_token":"at{{++refreshes}}","expires_in":3600}"""),
            var url when url.StartsWith("https://www.googleapis.com/drive/v3/about", StringComparison.Ordinal) =>
                Json("""{"user":{"displayName":"Ana","emailAddress":"ana@example.com"}}"""),
            _ => new HttpResponseMessage(HttpStatusCode.OK),
        };
    }

    private static string Body(HttpRequestMessage request) =>
        request.Content is null ? "" : request.Content.ReadAsStringAsync().GetAwaiter().GetResult();

    private GoogleDriveAccount Account(GoogleClient? client = null) => new(
        client ?? Client,
        new DriveTokenFile(TokenPath),
        new HttpClient(new FakeHandler(request =>
        {
            _requests.Add((request.RequestUri!.ToString(), request.Headers.Authorization?.ToString(), Body(request)));
            return _answer(request);
        })),
        new FakeSignIn(this),
        () => _now);

    [Fact]
    public async Task Link_ExchangesTheCode_AndRemembersWhoIsLinked()
    {
        var account = Account();
        var changed = 0;
        account.Changed += () => changed++;

        var link = await account.LinkAsync(DriveAccess.WholeDrive, Token);

        Assert.Equal(new DriveLink(DriveAccess.WholeDrive, "Ana (ana@example.com)", _now), link);
        Assert.Equal(link, account.Link);
        Assert.Equal(1, changed);
        var exchange = _requests.Single(r => r.Url == GoogleOAuth.TokenEndpoint).Body;
        Assert.Contains("code=code-1", exchange, StringComparison.Ordinal);
        Assert.Contains("client_id=client-1", exchange, StringComparison.Ordinal);
        Assert.Contains("client_secret=secret-1", exchange, StringComparison.Ordinal);
        Assert.Contains("code_verifier=", exchange, StringComparison.Ordinal);
        Assert.Contains("redirect_uri=http%3A%2F%2F127.0.0.1%3A4000%2F", exchange, StringComparison.Ordinal);
        Assert.Equal("Bearer at1", _requests.Single(r => r.Url.Contains("/about", StringComparison.Ordinal)).Auth);
        Assert.Equal(link, Account().Link); // kept for the next start
    }

    [Fact]
    public async Task Link_AsksGoogleForTheChosenLevel()
    {
        await Account().LinkAsync(DriveAccess.WholeDriveReadOnly, Token);

        Assert.Equal(DriveAccess.WholeDriveReadOnly, LastRequest!.Access);
    }

    [Fact]
    public async Task PickFiles_OpensThePicker_ReturnsThePicks_AndKeepsTheNewLink()
    {
        var account = Account();
        await account.LinkAsync(DriveAccess.PickedFiles, Token);
        _signIn = new SignInAnswer("code-2", null, ["f1", "f2"]);

        var picked = await account.PickFilesAsync(Token);

        Assert.Equal(["f1", "f2"], picked);
        Assert.Contains("trigger_onepick=true", LastRequest!.AuthorizationUrl.Query, StringComparison.Ordinal);
        Assert.Equal(DriveAccess.PickedFiles, account.Link!.Access);
    }

    [Fact]
    public async Task PickFiles_NeedsPickedFilesAccess()
    {
        var account = Account();
        await account.LinkAsync(DriveAccess.WholeDrive, Token);

        await Assert.ThrowsAsync<InvalidOperationException>(() => account.PickFilesAsync(Token));
    }

    [Fact]
    public async Task Refused_LeavesDriveUnlinked()
    {
        _signIn = new SignInAnswer(null, "access_denied", []);
        var account = Account();

        var error = await Assert.ThrowsAsync<DriveSignInException>(() => account.LinkAsync(DriveAccess.PickedFiles, Token));

        Assert.Equal("Access to Google Drive was not given.", error.Message);
        Assert.Null(account.Link);
        Assert.False(File.Exists(TokenPath));
    }

    [Fact]
    public async Task ScopeNotGranted_LeavesDriveUnlinked()
    {
        _answer = request => request.RequestUri!.ToString() == GoogleOAuth.TokenEndpoint
            ? Json("""{"access_token":"at1","expires_in":3600,"refresh_token":"rt1","scope":"https://www.googleapis.com/auth/drive.file"}""")
            : Google()(request);
        var account = Account();

        var error = await Assert.ThrowsAsync<DriveSignInException>(() => account.LinkAsync(DriveAccess.WholeDrive, Token));

        Assert.Contains("not given", error.Message, StringComparison.Ordinal);
        Assert.Null(account.Link);
    }

    [Fact]
    public async Task LinkThatCannotBeSaved_SaysSo()
    {
        Directory.CreateDirectory(TokenPath); // a folder in the way of the file
        var account = Account();

        var error = await Assert.ThrowsAsync<DriveSignInException>(() => account.LinkAsync(DriveAccess.WholeDrive, Token));

        Assert.Contains("could not be saved", error.Message, StringComparison.Ordinal);
        Assert.Null(account.Link);
    }

    [Fact]
    public async Task ExchangeFailing_LeavesDriveUnlinked()
    {
        _answer = _ => Json("""{"error":"invalid_client"}""", HttpStatusCode.Unauthorized);
        var account = Account();

        var error = await Assert.ThrowsAsync<DriveSignInException>(() => account.LinkAsync(DriveAccess.PickedFiles, Token));

        Assert.Contains("invalid_client", error.Message, StringComparison.Ordinal);
        Assert.Null(account.Link);
    }

    [Fact]
    public async Task AccountNameUnavailable_StillLinks()
    {
        var google = Google();
        _answer = request => request.RequestUri!.AbsolutePath.Contains("/about", StringComparison.Ordinal)
            ? new HttpResponseMessage(HttpStatusCode.Forbidden)
            : google(request);

        var link = await Account().LinkAsync(DriveAccess.PickedFiles, Token);

        Assert.Null(link.AccountName);
    }

    [Fact]
    public async Task AccessToken_IsReused_UntilNearlyExpired_ThenRefreshed()
    {
        var account = Account();
        await account.LinkAsync(DriveAccess.WholeDrive, Token);

        Assert.Equal("at1", await account.GetAccessTokenAsync(Token));
        _now = _now.AddMinutes(58);
        Assert.Equal("at1", await account.GetAccessTokenAsync(Token));
        _now = _now.AddMinutes(1.5);
        Assert.Equal("at2", await account.GetAccessTokenAsync(Token));
        var refresh = _requests.Last(r => r.Url == GoogleOAuth.TokenEndpoint).Body;
        Assert.Contains("grant_type=refresh_token", refresh, StringComparison.Ordinal);
        Assert.Contains("refresh_token=rt1", refresh, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AfterARestart_TheStoredLinkGivesAccessTokens()
    {
        await Account().LinkAsync(DriveAccess.WholeDrive, Token);

        Assert.Equal("at2", await Account().GetAccessTokenAsync(Token));
    }

    [Fact]
    public async Task GoogleRejectingTheLink_UnlinksAndSaysSo()
    {
        await Account().LinkAsync(DriveAccess.WholeDrive, Token);
        _answer = _ => Json("""{"error":"invalid_grant"}""", HttpStatusCode.BadRequest);
        var account = Account();
        var changed = 0;
        account.Changed += () => changed++;

        await Assert.ThrowsAsync<DriveLinkLostException>(() => account.GetAccessTokenAsync(Token));

        Assert.Null(account.Link);
        Assert.False(File.Exists(TokenPath));
        Assert.Equal(1, changed);
    }

    [Fact]
    public async Task RefreshFailingOtherwise_KeepsTheLink()
    {
        await Account().LinkAsync(DriveAccess.WholeDrive, Token);
        _answer = _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        var account = Account();

        await Assert.ThrowsAsync<HttpRequestException>(() => account.GetAccessTokenAsync(Token));

        Assert.NotNull(account.Link);
    }

    [Fact]
    public async Task NotLinked_HasNoAccessToken()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => Account().GetAccessTokenAsync(Token));
    }

    [Fact]
    public async Task Unlink_RevokesAndForgets()
    {
        var account = Account();
        await account.LinkAsync(DriveAccess.WholeDrive, Token);

        await account.UnlinkAsync(Token);

        Assert.Null(account.Link);
        Assert.False(File.Exists(TokenPath));
        var revoke = _requests.Single(r => r.Url == GoogleOAuth.RevokeEndpoint);
        Assert.Equal("token=rt1", revoke.Body);
    }

    [Fact]
    public async Task Unlink_ForgetsEvenWhenGoogleCannotBeReached()
    {
        var account = Account();
        await account.LinkAsync(DriveAccess.WholeDrive, Token);
        _answer = _ => throw new HttpRequestException("offline");

        await account.UnlinkAsync(Token);

        Assert.Null(account.Link);
        Assert.False(File.Exists(TokenPath));
    }

    [Fact]
    public async Task WithoutAClient_DriveCannotBeLinked()
    {
        var account = new GoogleDriveAccount(null, new DriveTokenFile(TokenPath), new HttpClient(), new FakeSignIn(this), () => _now);

        Assert.False(account.IsConfigured);
        await Assert.ThrowsAsync<InvalidOperationException>(() => account.LinkAsync(DriveAccess.PickedFiles, Token));
    }

    private SignInRequest? LastRequest { get; set; }

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private sealed class FakeSignIn(GoogleDriveAccountTests test) : IBrowserSignIn
    {
        public Task<(SignInRequest Request, SignInAnswer Answer)> SignInAsync(
            Func<string, SignInRequest> newRequest, CancellationToken cancellationToken)
        {
            var request = newRequest("http://127.0.0.1:4000/");
            test.LastRequest = request;
            return Task.FromResult((request, test._signIn));
        }
    }
}

public sealed class DriveTokenFileTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("rc-token-").FullName;

    private string FilePath => Path.Combine(_folder, "drive-token.json");

    [Fact]
    public void Saved_IsReadBack()
    {
        var token = new DriveToken("rt", DriveAccess.WholeDrive, "Ana", new DateTime(2026, 11, 21, 0, 0, 0, DateTimeKind.Utc));

        new DriveTokenFile(FilePath).Save(token);

        Assert.Equal(token, new DriveTokenFile(FilePath).Load());
        if (OperatingSystem.IsWindows())
        {
            Assert.DoesNotContain("\"rt\"", File.ReadAllText(FilePath), StringComparison.Ordinal); // encrypted there
        }
    }

    [Fact]
    public void OnlyTheUserCanReadIt()
    {
        if (OperatingSystem.IsWindows())
        {
            return; // protected by DPAPI there instead
        }

        new DriveTokenFile(FilePath).Save(new DriveToken("rt", DriveAccess.PickedFiles, null, DateTime.UtcNow));

        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(FilePath));
    }

    [Fact]
    public void MissingOrDamaged_ReadsAsNotLinked()
    {
        Assert.Null(new DriveTokenFile(FilePath).Load());
        File.WriteAllText(FilePath, "{ not json");
        Assert.Null(new DriveTokenFile(FilePath).Load());
    }

    [Fact]
    public void Delete_RemovesIt()
    {
        var file = new DriveTokenFile(FilePath);
        file.Save(new DriveToken("rt", DriveAccess.PickedFiles, null, DateTime.UtcNow));

        file.Delete();
        file.Delete(); // nothing left: fine

        Assert.False(File.Exists(FilePath));
    }

    [Fact]
    public void WithAKeyring_TheLinkIsKeptThere_AndNotInTheFile()
    {
        var keyring = new FakeKeyring();
        var token = new DriveToken("rt", DriveAccess.WholeDrive, "Ana", new DateTime(2026, 11, 21, 0, 0, 0, DateTimeKind.Utc));

        new DriveTokenFile(FilePath, keyring).Save(token);

        Assert.False(File.Exists(FilePath));
        Assert.Contains("\"rt\"", keyring.Secrets[FilePath], StringComparison.Ordinal);
        Assert.Equal(token, new DriveTokenFile(FilePath, keyring).Load());
    }

    [Fact]
    public void ALinkInTheFile_MovesIntoTheKeyring()
    {
        var token = new DriveToken("rt", DriveAccess.PickedFiles, null, new DateTime(2026, 11, 21, 0, 0, 0, DateTimeKind.Utc));
        new DriveTokenFile(FilePath).Save(token); // an older version's file
        var keyring = new FakeKeyring();

        Assert.Equal(token, new DriveTokenFile(FilePath, keyring).Load());

        Assert.False(File.Exists(FilePath));
        Assert.True(keyring.Secrets.ContainsKey(FilePath));
        Assert.Equal(token, new DriveTokenFile(FilePath, keyring).Load());
    }

    [Fact]
    public void KeyringUnavailable_UsesTheFile()
    {
        var keyring = new FakeKeyring { Available = false };
        var token = new DriveToken("rt", DriveAccess.PickedFiles, null, new DateTime(2026, 11, 21, 0, 0, 0, DateTimeKind.Utc));

        new DriveTokenFile(FilePath, keyring).Save(token);

        Assert.True(File.Exists(FilePath));
        Assert.Equal(token, new DriveTokenFile(FilePath, keyring).Load());
    }

    [Fact]
    public void Delete_ClearsTheKeyringAndTheFile_AndADamagedSecretReadsAsNotLinked()
    {
        var keyring = new FakeKeyring();
        var file = new DriveTokenFile(FilePath, keyring);
        file.Save(new DriveToken("rt", DriveAccess.PickedFiles, null, DateTime.UtcNow));

        file.Delete();

        Assert.Empty(keyring.Secrets);
        keyring.Secrets[FilePath] = "{ not json";
        Assert.Null(file.Load());
    }

    [Fact]
    public void Libsecret_WithoutASessionBus_IsUnavailable_AndDoesNotThrow()
    {
        var keyring = new LibsecretKeyring(hasSessionBus: () => false);

        Assert.False(keyring.TryLoad("rc-test", out _));
        Assert.False(keyring.TrySave("rc-test", "test", "secret"));
        keyring.Delete("rc-test");
    }

    /// <summary>Uses the real keyring, so it runs only when asked: RC_KEYRING_TEST=1.</summary>
    [Fact]
    public void Libsecret_RoundTrip_OnARealKeyring()
    {
        if (!OperatingSystem.IsLinux() || Environment.GetEnvironmentVariable("RC_KEYRING_TEST") != "1")
        {
            return;
        }

        var keyring = new LibsecretKeyring();
        var key = $"rc-test-{Guid.NewGuid():N}";
        Assert.True(keyring.TrySave(key, "readerCompanion test", "secret-1"));
        Assert.True(keyring.TryLoad(key, out var secret));
        Assert.Equal("secret-1", secret);

        keyring.Delete(key);

        Assert.True(keyring.TryLoad(key, out secret));
        Assert.Null(secret);
    }

    public void Dispose() => Directory.Delete(_folder, recursive: true);
}

internal sealed class FakeKeyring : IKeyring
{
    public bool Available { get; set; } = true;

    public Dictionary<string, string> Secrets { get; } = [];

    public bool TryLoad(string key, out string? secret)
    {
        secret = Available ? Secrets.GetValueOrDefault(key) : null;
        return Available;
    }

    public bool TrySave(string key, string label, string secret)
    {
        if (Available)
        {
            Secrets[key] = secret;
        }

        return Available;
    }

    public void Delete(string key) => Secrets.Remove(key);
}

public sealed class GoogleClientConfigTests : IDisposable
{
    private const string Download = """{"installed":{"client_id":"id-1.apps.googleusercontent.com","client_secret":"s-1","redirect_uris":["http://localhost"]}}""";
    private readonly string _app = Directory.CreateTempSubdirectory("rc-app-").FullName;
    private readonly string _data = Directory.CreateTempSubdirectory("rc-data-").FullName;

    [Fact]
    public void GooglesDownload_IsRead_AppFolderFirst()
    {
        File.WriteAllText(Path.Combine(_data, GoogleClientConfig.FileName), Download.Replace("id-1", "id-2", StringComparison.Ordinal));
        Assert.Equal(new GoogleClient("id-2.apps.googleusercontent.com", "s-1"), GoogleClientConfig.TryLoad(_app, _data));

        File.WriteAllText(Path.Combine(_app, GoogleClientConfig.FileName), Download);
        Assert.Equal(new GoogleClient("id-1.apps.googleusercontent.com", "s-1"), GoogleClientConfig.TryLoad(_app, _data));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("{ damaged")]
    [InlineData("\0GITCRYPT\0locked")] // a clone without the git-crypt key
    [InlineData("""{"web":{"client_id":"x","client_secret":"y"}}""")]
    [InlineData("""{"installed":{"client_id":"","client_secret":"y"}}""")]
    public void MissingOrUnusable_GivesNone(string? content)
    {
        if (content is not null)
        {
            File.WriteAllText(Path.Combine(_app, GoogleClientConfig.FileName), content);
        }

        Assert.Null(GoogleClientConfig.TryLoad(_app, _data));
    }

    public void Dispose()
    {
        Directory.Delete(_app, recursive: true);
        Directory.Delete(_data, recursive: true);
    }
}
