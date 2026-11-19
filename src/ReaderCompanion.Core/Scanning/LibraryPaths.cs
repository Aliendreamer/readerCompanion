namespace ReaderCompanion.Core.Scanning;

/// <summary>Path comparison for library folders: case-insensitive on Windows, case-sensitive elsewhere.</summary>
public static class LibraryPaths
{
    public static StringComparison Comparison =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public static StringComparer Comparer => StringComparer.FromComparison(Comparison);

    /// <summary>Full path without a trailing separator (a filesystem root keeps its separator).</summary>
    public static string Normalize(string path) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

    public static bool IsSameOrUnder(string path, string root)
    {
        var p = Normalize(path);
        var r = Normalize(root);
        if (p.Equals(r, Comparison))
        {
            return true;
        }

        var prefix = Path.EndsInDirectorySeparator(r) ? r : r + Path.DirectorySeparatorChar;
        return p.StartsWith(prefix, Comparison);
    }
}
