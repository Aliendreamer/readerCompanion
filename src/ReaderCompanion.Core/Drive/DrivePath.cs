namespace ReaderCompanion.Core.Drive;

/// <summary>The catalog path of a Drive file: <c>gdrive://&lt;file id&gt;/&lt;display path&gt;</c>. The id keeps it
/// unique (Drive allows two files with one name); the display path follows renames and moves.</summary>
public static class DrivePath
{
    private const string Scheme = "gdrive://";

    public static string Make(string fileId, string displayPath) => $"{Scheme}{fileId}/{displayPath}";

    public static bool IsDrive(string path) => path.StartsWith(Scheme, StringComparison.Ordinal);

    /// <summary>The Drive id in a Drive path.</summary>
    public static string Id(string path) =>
        IsDrive(path) && path.IndexOf('/', Scheme.Length) is var slash and >= 0
            ? path[Scheme.Length..slash]
            : throw new ArgumentException("Not a Drive path.", nameof(path));

    /// <summary>The path as the user knows it on Drive; a local path is returned as it is.</summary>
    public static string Display(string path) =>
        IsDrive(path) && path.IndexOf('/', Scheme.Length) is var slash and >= 0 ? path[(slash + 1)..] : path;

    /// <summary>True for a Drive file in <paramref name="folder"/> (a display path) or below it.</summary>
    public static bool IsUnder(string path, string folder) => IsDrive(path) && IsSameOrUnder(Display(path), folder);

    /// <summary>A display path's folder and name ("Books/a.epub" → "Books", "a.epub").</summary>
    public static (string Folder, string Name) Split(string display) =>
        display.LastIndexOf('/') is var slash and >= 0 ? (display[..slash], display[(slash + 1)..]) : ("", display);

    /// <summary>The Drive file's display path as seen from <paramref name="folder"/>, or the whole display path when it
    /// is not inside.</summary>
    public static string Relative(string path, string folder) =>
        IsUnder(path, folder) ? Display(path)[(folder.Length + 1)..] : Display(path);

    /// <summary>True when the display path <paramref name="path"/> is <paramref name="folder"/> or inside it.</summary>
    public static bool IsSameOrUnder(string path, string folder) =>
        path == folder || path.StartsWith(folder + "/", StringComparison.Ordinal);
}
