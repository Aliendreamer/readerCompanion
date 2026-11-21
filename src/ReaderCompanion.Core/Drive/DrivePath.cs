namespace ReaderCompanion.Core.Drive;

/// <summary>The catalog path of a Drive file: <c>gdrive://&lt;file id&gt;/&lt;display path&gt;</c>. The id keeps it
/// unique (Drive allows two files with one name); the display path follows renames and moves.</summary>
public static class DrivePath
{
    private const string Scheme = "gdrive://";

    public static string Make(string fileId, string displayPath) => $"{Scheme}{fileId}/{displayPath}";

    public static bool IsDrive(string path) => path.StartsWith(Scheme, StringComparison.Ordinal);

    /// <summary>The path as the user knows it on Drive; a local path is returned as it is.</summary>
    public static string Display(string path) =>
        IsDrive(path) && path.IndexOf('/', Scheme.Length) is var slash and >= 0 ? path[(slash + 1)..] : path;

    /// <summary>True for a Drive file in <paramref name="folder"/> (a display path) or below it.</summary>
    public static bool IsUnder(string path, string folder) => IsDrive(path) && IsSameOrUnder(Display(path), folder);

    /// <summary>True when the display path <paramref name="path"/> is <paramref name="folder"/> or inside it.</summary>
    public static bool IsSameOrUnder(string path, string folder) =>
        path == folder || path.StartsWith(folder + "/", StringComparison.Ordinal);
}
