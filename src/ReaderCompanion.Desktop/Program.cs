using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
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
        builder.Services.AddReaderCompanionUI();
        builder.Services.AddSingleton<IFolderPicker, PhotinoFolderPicker>();
        builder.Services.AddSingleton<ICoverUrls, CoverUrls>();
        AddCoversToAppFiles(builder.Services);
        builder.RootComponents.Add<Routes>("#app");

        var app = builder.Build();
        app.Services.MigrateLibraryDatabase();

        app.MainWindow
            .SetTitle("readerCompanion")
            .SetSize(1200, 800)
            .SetLogVerbosity(0);

        app.Run();
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
