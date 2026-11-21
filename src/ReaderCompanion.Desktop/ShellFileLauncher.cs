using System.ComponentModel;
using System.Diagnostics;
using ReaderCompanion.Core.Drive;
using ReaderCompanion.UI;

namespace ReaderCompanion.Desktop;

/// <summary>Opens files and folders with the system's default handler (the shell on Windows, xdg-open on
/// Linux).</summary>
internal sealed class ShellFileLauncher : IFileLauncher
{
    public bool Open(string path) => File.Exists(path) && Launch(path);

    public bool ShowInFolder(string path) =>
        Path.GetDirectoryName(path) is { } folder && Directory.Exists(folder) && Launch(folder);

    /// <summary>Hands a file, folder or web address to the system's default handler.</summary>
    internal static bool Launch(string target)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
            return true;
        }
        catch (Exception e) when (e is Win32Exception or InvalidOperationException or FileNotFoundException)
        {
            return false;
        }
    }
}

/// <summary>Opens web pages (Google's sign-in) in the user's default browser; only https addresses.</summary>
internal sealed class ShellWebBrowser : IWebBrowser
{
    public bool Open(Uri url) => url.Scheme == Uri.UriSchemeHttps && ShellFileLauncher.Launch(url.AbsoluteUri);
}
