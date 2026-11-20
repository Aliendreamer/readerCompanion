using System.ComponentModel;
using System.Diagnostics;
using ReaderCompanion.UI;

namespace ReaderCompanion.Desktop;

/// <summary>Opens files and folders with the system's default handler (the shell on Windows, xdg-open on
/// Linux).</summary>
internal sealed class ShellFileLauncher : IFileLauncher
{
    public bool Open(string path) => File.Exists(path) && Launch(path);

    public bool ShowInFolder(string path) =>
        Path.GetDirectoryName(path) is { } folder && Directory.Exists(folder) && Launch(folder);

    private static bool Launch(string target)
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
