using ReaderCompanion.Core.Metadata;

namespace ReaderCompanion.Data;

/// <summary>Writes cover images into the covers folder; shared by scans and cover lookups.</summary>
internal static class CoverFiles
{
    /// <summary>Saves the cover as <paramref name="baseName"/> plus its extension (keeping an existing file) and returns
    /// the file name, or null when it cannot be written — a cover is nice to have, never worth failing over.</summary>
    public static string? Save(string coversFolder, string baseName, CoverImage cover)
    {
        var fileName = baseName + cover.Extension;
        var path = Path.Combine(coversFolder, fileName);
        try
        {
            if (!File.Exists(path))
            {
                // Written aside and moved into place, so a crash never leaves a half-written cover under the real name.
                Directory.CreateDirectory(coversFolder);
                var partial = path + ".partial";
                File.WriteAllBytes(partial, cover.Bytes);
                File.Move(partial, path, overwrite: true);
            }

            return fileName;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
