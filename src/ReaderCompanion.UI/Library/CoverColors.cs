using ReaderCompanion.Core.Metadata;

namespace ReaderCompanion.UI.Library;

/// <summary>Placeholder colours that stay the same for a title across restarts.</summary>
public static class CoverColors
{
    /// <summary>A hue (0–359) from a stable FNV-1a hash of the normalized title.</summary>
    public static int HueFor(string title)
    {
        var hash = 2166136261u;
        foreach (var c in NameNormalizer.Normalize(title))
        {
            hash = (hash ^ c) * 16777619u;
        }

        return (int)(hash % 360);
    }
}
