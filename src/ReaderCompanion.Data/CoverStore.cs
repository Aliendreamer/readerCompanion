namespace ReaderCompanion.Data;

/// <summary>Finds cover images saved by the scan, and nothing else: only plain image file names directly in
/// the covers folder are resolved.</summary>
public sealed class CoverStore
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp",
    };

    private readonly string _coversFolder;

    internal CoverStore(LibraryDataPaths paths) => _coversFolder = paths.CoversFolder;

    /// <summary>The cover's full path, or null when the name is not a cover or the file is gone.</summary>
    public string? PathFor(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)
            || fileName.IndexOfAny(['/', '\\']) >= 0
            || fileName != Path.GetFileName(fileName)
            || !ImageExtensions.Contains(Path.GetExtension(fileName)))
        {
            return null;
        }

        var path = Path.Combine(_coversFolder, fileName);
        return File.Exists(path) ? path : null;
    }
}
