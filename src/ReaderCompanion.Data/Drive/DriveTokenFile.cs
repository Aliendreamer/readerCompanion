using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ReaderCompanion.Core.Drive;

namespace ReaderCompanion.Data;

/// <summary>What is kept of a Drive link: the user's refresh token and how it was made.</summary>
internal sealed record DriveToken(string RefreshToken, DriveAccess Access, string? AccountName, DateTime LinkedUtc);

/// <summary>The Drive link, kept from others: in the system keyring when one is given and usable (Linux);
/// otherwise in a file, encrypted for the current user on Windows, with mode 0600 elsewhere. A link found in the
/// file moves into the keyring.</summary>
internal sealed class DriveTokenFile(string path, IKeyring? keyring = null)
{
    private const string KeyringLabel = "readerCompanion Google Drive link";
    private static readonly JsonSerializerOptions Options = new() { Converters = { new JsonStringEnumConverter() } };

    public DriveToken? Load()
    {
        if (keyring is null || !keyring.TryLoad(path, out var secret))
        {
            return LoadFile();
        }

        if (secret is not null)
        {
            return Parse(Encoding.UTF8.GetBytes(secret));
        }

        // Nothing in the keyring yet: a link an older version left in the file moves in.
        var old = LoadFile();
        if (old is not null && keyring.TrySave(path, KeyringLabel, JsonSerializer.Serialize(old, Options)))
        {
            DeleteFile();
        }

        return old;
    }

    public void Save(DriveToken token)
    {
        if (keyring?.TrySave(path, KeyringLabel, JsonSerializer.Serialize(token, Options)) == true)
        {
            DeleteFile(); // an older plain copy must not linger
            return;
        }

        SaveFile(token);
    }

    public void Delete()
    {
        keyring?.Delete(path);
        DeleteFile();
    }

    private static DriveToken? Parse(byte[] json)
    {
        try
        {
            return JsonSerializer.Deserialize<DriveToken>(json, Options) is { RefreshToken.Length: > 0 } token ? token : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private DriveToken? LoadFile()
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

            return Parse(bytes);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or CryptographicException)
        {
            return null;
        }
    }

    private void SaveFile(DriveToken token)
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

    private void DeleteFile() => File.Delete(path);
}
