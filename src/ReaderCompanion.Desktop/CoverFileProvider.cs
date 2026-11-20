using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Primitives;
using ReaderCompanion.Data;

namespace ReaderCompanion.Desktop;

/// <summary>Photino.Blazor's file provider with the library's covers added under <c>_covers/</c>. Covers then
/// load from the app's own origin, which works with both WebView2 and WebKitGTK.</summary>
internal sealed class CoverFileProvider(IFileProvider app, CoverStore covers) : IFileProvider
{
    public IFileInfo GetFileInfo(string subpath)
    {
        var path = subpath.TrimStart('/');
        if (!path.StartsWith(CoverUrls.Prefix, StringComparison.Ordinal))
        {
            return app.GetFileInfo(subpath);
        }

        return covers.PathFor(Uri.UnescapeDataString(path[CoverUrls.Prefix.Length..])) is { } file
            ? new PhysicalFileInfo(new FileInfo(file))
            : new NotFoundFileInfo(subpath);
    }

    public IDirectoryContents GetDirectoryContents(string subpath) => app.GetDirectoryContents(subpath);

    public IChangeToken Watch(string filter) => app.Watch(filter);
}
