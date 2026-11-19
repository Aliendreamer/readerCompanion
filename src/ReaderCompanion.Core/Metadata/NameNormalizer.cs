using System.Text.RegularExpressions;

namespace ReaderCompanion.Core.Metadata;

/// <summary>Cleans names and builds the key used to match authors, genres and series across books.</summary>
public static partial class NameNormalizer
{
    /// <summary>Trims and collapses whitespace; returns null for null, empty or whitespace-only values.</summary>
    public static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Whitespace().Replace(value.Trim(), " ");
    }

    /// <summary>Matching key: cleaned and upper-cased invariantly.</summary>
    public static string Normalize(string value) => (Clean(value) ?? "").ToUpperInvariant();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
