using System.Globalization;
using System.Text.RegularExpressions;

namespace ReaderCompanion.Core.Metadata;

/// <summary>Cleans series names files carry in forms seen in real libraries. Only known forms are changed; any other
/// name is returned as it is.</summary>
public static partial class SeriesName
{
    /// <summary>The series and its number. A release-tagged name such as
    /// "Serified.Dresden Files.SciFan.(#01 of 14)._.ICA Serified" gives "Dresden Files" and 1: of the dot-separated parts
    /// before the "(#N of M)" part, the one with a space (a name, not a tag), else the longest. The number in the name
    /// wins over the file's own: files tagged this way were seen to all say 1.</summary>
    public static (string Name, decimal? Index) Clean(string name, decimal? index)
    {
        var tagged = ReleaseTagged().Match(name);
        if (!tagged.Success)
        {
            return (name, index);
        }

        var parts = tagged.Groups["before"].Value.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var series = parts.FirstOrDefault(p => p.Contains(' ', StringComparison.Ordinal)) ?? parts.MaxBy(p => p.Length)!;
        return (series, decimal.Parse(tagged.Groups["n"].Value, CultureInfo.InvariantCulture));
    }

    // Two or more dot-separated parts, then ".(#N of M)".
    [GeneratedRegex(@"^(?<before>[^.()]+(?:\.[^.()]+)+)\.\(#(?<n>\d+(?:\.\d+)?) of \d+\)")]
    private static partial Regex ReleaseTagged();
}
