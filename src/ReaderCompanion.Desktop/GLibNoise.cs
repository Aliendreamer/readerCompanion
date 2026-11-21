using System.Runtime.InteropServices;

namespace ReaderCompanion.Desktop;

/// <summary>Silences GLib's known-harmless "GFileInfo created without standard::size" criticals, which GTK's file
/// chooser triggers on some file systems (WSL's among them); every other GLib-GIO message is printed as before.</summary>
internal static class GLibNoise
{
    private const string Domain = "GLib-GIO";
    private const int CriticalLevel = 1 << 3; // G_LOG_LEVEL_CRITICAL

    private delegate void LogFunc(nint domain, int level, nint message, nint userData);

    // Kept for the life of the app: GLib holds a pointer to it.
    private static readonly LogFunc Handler = OnLog;

    public static void Silence()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        try
        {
            _ = g_log_set_handler(Domain, CriticalLevel, Marshal.GetFunctionPointerForDelegate(Handler), 0);
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
            // No GLib: no GTK either, and nothing to silence.
        }
    }

    /// <summary>The two lines GLib prints for a file info missing its size.</summary>
    internal static bool IsNoise(string? message) => message is not null
        && (message.Contains("GFileInfo created without", StringComparison.Ordinal)
            || message.Contains("(g_file_info_get_", StringComparison.Ordinal));

    private static void OnLog(nint domain, int level, nint message, nint userData)
    {
        if (!IsNoise(Marshal.PtrToStringUTF8(message)))
        {
            g_log_default_handler(domain, level, message, userData);
        }
    }

    [DllImport("libglib-2.0.so.0")]
    private static extern uint g_log_set_handler([MarshalAs(UnmanagedType.LPUTF8Str)] string domain, int levels, nint handler, nint userData);

    [DllImport("libglib-2.0.so.0")]
    private static extern void g_log_default_handler(nint domain, int level, nint message, nint userData);
}
