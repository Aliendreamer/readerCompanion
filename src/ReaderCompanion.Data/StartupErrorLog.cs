using System.Globalization;
using System.Runtime.InteropServices;

namespace ReaderCompanion.Data;

/// <summary>Records why the app could not start, so the user sees a reason instead of a silent exit.</summary>
public static class StartupErrorLog
{
    public const string FileName = "startup-error.log";

    /// <summary>Writes the error's details to <c>startup-error.log</c> in <paramref name="preferredFolder"/> (or the system
    /// temp folder if that fails) and returns the log's path (null if nothing could be written) and a short message for a
    /// dialog.</summary>
    public static (string? LogPath, string Message) Write(Exception error, string? preferredFolder, DateTime nowLocal)
    {
        var details =
            $"readerCompanion could not start at {nowLocal.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}\n" +
            $"{RuntimeInformation.FrameworkDescription} on {RuntimeInformation.OSDescription}\n\n{error}\n";
        // In the shared temp folder the name is unique and created fresh, so it can never overwrite someone's file.
        var places = new (string? Folder, string Name, FileMode Mode)[]
        {
            (preferredFolder, FileName, FileMode.Create),
            (Path.GetTempPath(), $"readerCompanion-startup-error-{Guid.NewGuid():N}.log", FileMode.CreateNew),
        };
        string? logPath = null;
        foreach (var (folder, name, mode) in places)
        {
            if (folder is null)
            {
                continue;
            }

            try
            {
                Directory.CreateDirectory(folder);
                var path = Path.Combine(folder, name);
                using (var writer = new StreamWriter(new FileStream(path, mode, FileAccess.Write)))
                {
                    writer.Write(details);
                }

                logPath = path;
                break;
            }
            catch (Exception)
            {
                // Reporting a failure must never fail itself: try the next place.
            }
        }

        var message = $"readerCompanion could not start.\n\n{error.Message}" +
            (logPath is null ? "" : $"\n\nThe details are in {logPath}");
        return (logPath, message);
    }
}
