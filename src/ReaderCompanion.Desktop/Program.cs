using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;
using ReaderCompanion.Data;
using ReaderCompanion.UI;

namespace ReaderCompanion.Desktop;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var builder = PhotinoBlazorAppBuilder.CreateDefault(args);
        builder.Services.AddLibraryData(AppPaths.DefaultDatabasePath());
        builder.Services.AddSingleton<IFolderPicker, PhotinoFolderPicker>();
        builder.RootComponents.Add<Routes>("#app");

        var app = builder.Build();
        app.Services.MigrateLibraryDatabase();

        app.MainWindow
            .SetTitle("readerCompanion")
            .SetSize(1200, 800)
            .SetLogVerbosity(0);

        app.Run();
    }
}
