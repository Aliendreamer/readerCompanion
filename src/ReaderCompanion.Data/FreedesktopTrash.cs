using System.Globalization;
using ReaderCompanion.Core.Library;

namespace ReaderCompanion.Data;

/// <summary>The home trash of the freedesktop.org Trash specification (Linux desktops): the file goes into
/// <c>files/</c> and a <c>.trashinfo</c> in <c>info/</c> records where it came from, so the desktop's trash can restore
/// it. Moving across file systems falls back to copy-and-delete (done by <see cref="File.Move(string, string)"/>).</summary>
public sealed class FreedesktopTrash(string trashFolder, Func<DateTime> nowLocal) : ITrash
{
    /// <summary>The user's home trash: <c>$XDG_DATA_HOME/Trash</c>, or <c>~/.local/share/Trash</c>.</summary>
    public static FreedesktopTrash ForCurrentUser()
    {
        var dataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME") is { Length: > 0 } xdg
            ? xdg
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
        return new FreedesktopTrash(Path.Combine(dataHome, "Trash"), () => DateTime.Now);
    }

    public bool MoveToTrash(string path)
    {
        if (!File.Exists(path))
        {
            return false;
        }

        var files = Path.Combine(trashFolder, "files");
        var info = Path.Combine(trashFolder, "info");
        Directory.CreateDirectory(files);
        Directory.CreateDirectory(info);
        var name = FreeName(files, info, Path.GetFileName(path));
        var infoPath = Path.Combine(info, name + ".trashinfo");
        var trashed = Path.Combine(files, name);
        var createdInfo = false;
        try
        {
            // The info file comes first (created exclusively, so the name is ours), as the specification asks.
            using (var stream = new FileStream(infoPath, FileMode.CreateNew, FileAccess.Write))
            using (var writer = new StreamWriter(stream))
            {
                createdInfo = true;
                writer.Write($"[Trash Info]\nPath={Uri.EscapeDataString(Path.GetFullPath(path)).Replace("%2F", "/", StringComparison.Ordinal)}\n" +
                    $"DeletionDate={nowLocal().ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)}\n");
            }

            File.Move(path, trashed, overwrite: false);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            if (createdInfo && File.Exists(path)) // ours, and the file never made it: leave no half copy or orphan info
            {
                try
                {
                    File.Delete(infoPath);
                    File.Delete(trashed);
                }
                catch (Exception cleanup) when (cleanup is IOException or UnauthorizedAccessException)
                {
                    // Best effort; the original file is untouched either way.
                }
            }

            return false;
        }
    }

    /// <summary>"a.epub", else "a.2.epub", "a.3.epub"… — free in both <c>files/</c> and <c>info/</c>.</summary>
    private static string FreeName(string files, string info, string original)
    {
        var name = original;
        for (var n = 2; File.Exists(Path.Combine(files, name)) || File.Exists(Path.Combine(info, name + ".trashinfo")); n++)
        {
            name = $"{Path.GetFileNameWithoutExtension(original)}.{n}{Path.GetExtension(original)}";
        }

        return name;
    }
}
