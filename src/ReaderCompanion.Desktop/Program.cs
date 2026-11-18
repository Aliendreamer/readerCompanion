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
        builder.RootComponents.Add<Routes>("#app");

        var app = builder.Build();
        app.Services.MigrateLibraryDatabase();

        app.MainWindow
            .SetTitle("readerCompanion")
            .SetSize(1200, 800);

        app.Run();
    }
}
