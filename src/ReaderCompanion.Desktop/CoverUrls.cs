using ReaderCompanion.UI;

namespace ReaderCompanion.Desktop;

/// <summary>Cover URLs under the app's own origin, answered by <see cref="CoverFileProvider"/>.</summary>
internal sealed class CoverUrls : ICoverUrls
{
    public const string Prefix = "_covers/";

    public string UrlFor(string coverFileName) => Prefix + Uri.EscapeDataString(coverFileName);
}
