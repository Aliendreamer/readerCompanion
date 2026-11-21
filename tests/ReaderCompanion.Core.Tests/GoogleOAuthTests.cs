using System.Security.Cryptography;
using System.Text;
using ReaderCompanion.Core.Drive;

namespace ReaderCompanion.Core.Tests;

public class GoogleOAuthTests
{
    private static Dictionary<string, string> Query(Uri uri) => uri.Query.TrimStart('?').Split('&')
        .Select(p => p.Split('=', 2))
        .ToDictionary(p => p[0], p => Uri.UnescapeDataString(p[1]));

    [Theory]
    [InlineData(DriveAccess.PickedFiles, "https://www.googleapis.com/auth/drive.file")]
    [InlineData(DriveAccess.WholeDriveReadOnly, "https://www.googleapis.com/auth/drive.readonly")]
    [InlineData(DriveAccess.WholeDrive, "https://www.googleapis.com/auth/drive")]
    public void EachAccessLevel_HasOneScope(DriveAccess access, string scope)
    {
        Assert.Equal(scope, GoogleOAuth.Scope(access));
    }

    [Fact]
    public void AuthorizationUrl_AsksForAnOfflineCode_WithPkce()
    {
        var request = GoogleOAuth.NewRequest(DriveAccess.WholeDrive, "client-1", "http://127.0.0.1:5000/");

        var query = Query(request.AuthorizationUrl);
        Assert.Equal("https://accounts.google.com/o/oauth2/v2/auth", request.AuthorizationUrl.GetLeftPart(UriPartial.Path));
        Assert.Equal("client-1", query["client_id"]);
        Assert.Equal("http://127.0.0.1:5000/", query["redirect_uri"]);
        Assert.Equal("code", query["response_type"]);
        Assert.Equal("offline", query["access_type"]);
        Assert.Equal("consent", query["prompt"]);
        Assert.Equal("https://www.googleapis.com/auth/drive", query["scope"]);
        Assert.Equal(request.State, query["state"]);
        Assert.Equal("S256", query["code_challenge_method"]);
        var challenge = Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(request.CodeVerifier)))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        Assert.Equal(challenge, query["code_challenge"]);
        Assert.False(query.ContainsKey("trigger_onepick"));
    }

    [Fact]
    public void EachRequest_HasItsOwnStateAndVerifier()
    {
        var a = GoogleOAuth.NewRequest(DriveAccess.PickedFiles, "c", "http://127.0.0.1:1/");
        var b = GoogleOAuth.NewRequest(DriveAccess.PickedFiles, "c", "http://127.0.0.1:1/");

        Assert.NotEqual(a.State, b.State);
        Assert.NotEqual(a.CodeVerifier, b.CodeVerifier);
        Assert.InRange(a.CodeVerifier.Length, 43, 128);
    }

    [Fact]
    public void Picker_AddsThePickerParameters()
    {
        var request = GoogleOAuth.NewRequest(DriveAccess.PickedFiles, "c", "http://127.0.0.1:1/", pickFiles: true);

        var query = Query(request.AuthorizationUrl);
        Assert.Equal("true", query["trigger_onepick"]);
        Assert.Equal("true", query["allow_multiple"]);
    }

    [Fact]
    public void Picker_IsOnlyForPickedFiles()
    {
        Assert.Throws<ArgumentException>(() => GoogleOAuth.NewRequest(DriveAccess.WholeDrive, "c", "http://127.0.0.1:1/", pickFiles: true));
    }

    [Fact]
    public void Redirect_WithCodeAndMatchingState_IsAccepted_WithPickedFiles()
    {
        var request = GoogleOAuth.NewRequest(DriveAccess.PickedFiles, "c", "http://127.0.0.1:1/");

        var answer = GoogleOAuth.ReadRedirect(request, $"?code=abc%2F1&state={request.State}&picked_file_ids=f1,f2");

        Assert.Equal("abc/1", answer.Code);
        Assert.Null(answer.Error);
        Assert.Equal(["f1", "f2"], answer.PickedFileIds);
    }

    [Fact]
    public void Redirect_WithAnError_IsRefused()
    {
        var request = GoogleOAuth.NewRequest(DriveAccess.PickedFiles, "c", "http://127.0.0.1:1/");

        var answer = GoogleOAuth.ReadRedirect(request, $"?error=access_denied&state={request.State}");

        Assert.Null(answer.Code);
        Assert.Equal("access_denied", answer.Error);
    }

    [Fact]
    public void Redirect_WithAnotherState_IsRefused()
    {
        var request = GoogleOAuth.NewRequest(DriveAccess.PickedFiles, "c", "http://127.0.0.1:1/");

        var answer = GoogleOAuth.ReadRedirect(request, "?code=abc&state=forged");

        Assert.Null(answer.Code);
        Assert.Equal("state_mismatch", answer.Error);
    }
}
