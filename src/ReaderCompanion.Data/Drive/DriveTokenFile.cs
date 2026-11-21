using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ReaderCompanion.Core.Drive;

namespace ReaderCompanion.Data;

/// <summary>What is kept of a Drive link: the user's refresh token and how it was made.</summary>
internal sealed record DriveToken(string RefreshToken, DriveAccess Access, string? AccountName, DateTime LinkedUtc);

/// <summary>The Drive link on disk, readable only by the user: encrypted for the current user on Windows, a file
/// with mode 0600 elsewhere.</summary>
internal sealed class DriveTokenFile(string path)
{
    private static readonly JsonSerializerOptions Options = new() { Converters = { new JsonStringEnumConverter() } };

    public DriveToken? Load()
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var bytes = File.ReadAllBytes(path);
            if (OperatingSystem.IsWindows())
            {
                bytes = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
            }

            return JsonSerializer.Deserialize<DriveToken>(bytes, Options) is { RefreshToken.Length: > 0 } token ? token : null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or CryptographicException)
        {
            return null;
        }
    }

    public void Save(DriveToken token)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(token, Options);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var partial = path + ".partial";
        File.Delete(partial);
        if (OperatingSystem.IsWindows())
        {
            File.WriteAllBytes(partial, ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser));
        }
        else
        {
            // Created with the restricted mode, so the token is never readable by others, not even briefly.
            using var stream = new FileStream(partial, new FileStreamOptions
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite,
            });
            stream.Write(bytes);
        }

        File.Move(partial, path, overwrite: true);
    }

    public void Delete() => File.Delete(path);
}
