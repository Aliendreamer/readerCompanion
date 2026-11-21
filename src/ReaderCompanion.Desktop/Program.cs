using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Photino.Blazor;
using ReaderCompanion.Core.Drive;
using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Preferences;
using ReaderCompanion.Data;
using ReaderCompanion.UI;

namespace ReaderCompanion.Desktop;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        GLibNoise.Silence();
        try
        {
            Start(args);
        }
        catch (Exception e)
        {
            ReportStartupFailure(e);
            Environment.ExitCode = 1;
        }
    }

    private static void Start(string[] args)
    {
        var builder = PhotinoBlazorAppBuilder.CreateDefault(args);
        builder.Services.AddLibraryData(AppPaths.DefaultDatabasePath());
        builder.Services.AddReaderCompanionUI();
        builder.Services.AddSingleton<IFolderPicker, PhotinoFolderPicker>();
        builder.Services.AddSingleton<IFilePicker, PhotinoFilePicker>();
        builder.Services.AddSingleton<ICoverUrls, CoverUrls>();
        builder.Services.AddSingleton<IFileLauncher, ShellFileLauncher>();
        builder.Services.AddSingleton<IWebBrowser, ShellWebBrowser>();
        var window = new PhotinoAppWindow();
        builder.Services.AddSingleton<IAppWindow>(window); // after AddReaderCompanionUI, so it wins
        AddCoversToAppFiles(builder.Services);
        builder.RootComponents.Add<Routes>("#app");

        var app = builder.Build();
        app.Services.MigrateLibraryDatabase();

        // The whole window follows the chosen text size, at start and whenever it changes.
        var preferences = app.Services.GetRequiredService<IAppPreferences>();
        preferences.Changed += () => app.MainWindow.SetZoom(preferences.Current.TextSize);

        window.Attach(app.MainWindow, ownFrame: !preferences.Current.SystemTitleBar, remembered: preferences.Current.Window);
        var icon = Path.Combine(AppContext.BaseDirectory, OperatingSystem.IsWindows() ? "readercompanion.ico" : "readercompanion.png");
        if (File.Exists(icon))
        {
            app.MainWindow.SetIconFile(icon);
        }

        app.MainWindow
            .SetZoom(preferences.Current.TextSize)
            .SetLogVerbosity(0)
            .RegisterWindowClosingHandler((_, _) =>
            {
                if (window.Placement is { } placement)
                {
                    preferences.Save(preferences.Current with { Window = placement }); // reopened as it was left
                }

                BackUpOnExit(app.Services);
                return false; // never keep the window open
            });

        app.Run();
    }

    /// <summary>Writes the failure to the data folder's log and tells the user, instead of exiting silently.</summary>
    private static void ReportStartupFailure(Exception error)
    {
        var (_, message) = StartupErrorLog.Write(error, AppPaths.DefaultDataFolder(), DateTime.Now);
        const string title = "readerCompanion could not start";
        try
        {
            if (OperatingSystem.IsWindows())
            {
                _ = MessageBoxW(IntPtr.Zero, message, title, 0x10); // MB_ICONERROR; needs no web view, unlike Photino
            }
            else
            {
                new Photino.NET.PhotinoWindow().ShowMessage(title, message,
                    Photino.NET.PhotinoDialogButtons.Ok, Photino.NET.PhotinoDialogIcon.Error);
            }
        }
        catch (Exception)
        {
            Console.Error.WriteLine(message); // no window system either: the terminal is the last resort
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int MessageBoxW(IntPtr owner, string text, string caption, uint type);

    /// <summary>Backs up and prunes when the user asked for it; a failure must not stop the app from closing.</summary>
    private static void BackUpOnExit(IServiceProvider services)
    {
        var preferences = services.GetRequiredService<IAppPreferences>().Current;
        if (!preferences.BackupOnExit || preferences.BackupFolder is not { } folder)
        {
            return;
        }

        try
        {
            var backups = services.GetRequiredService<IBackups>();
            backups.Backup(folder, DateTime.Now);
            backups.Prune(folder, preferences.KeepBackups);
        }
        catch (Exception)
        {
            // Closing always wins, whatever went wrong; the next backup will try again.
        }
    }

    /// <summary>Wraps Photino.Blazor's own file provider so it also serves covers.</summary>
    private static void AddCoversToAppFiles(IServiceCollection services)
    {
        var original = services.Last(d => d.ServiceType == typeof(IFileProvider));
        services.Remove(original);
        services.AddSingleton<IFileProvider>(sp => new CoverFileProvider(
            original.ImplementationInstance as IFileProvider ?? (IFileProvider)original.ImplementationFactory!(sp),
            sp.GetRequiredService<CoverStore>()));
    }
}
