using System.Security.Cryptography;

namespace ReaderCompanion.Data;

/// <summary>How the library identifies a file's content: SHA-256, lower-case hex (the form Google Drive uses too, so
/// local and Drive copies of a book match).</summary>
internal static class ContentHash
{
    public static string Of(Stream content) => Convert.ToHexStringLower(SHA256.HashData(content));
}
