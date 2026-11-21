using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ReaderCompanion.UI;

public static class UIServiceCollectionExtensions
{
    /// <summary>Services the UI provides itself; the host adds <see cref="IFolderPicker"/> and
    /// <see cref="ICoverUrls"/>, and may add an <see cref="IAppWindow"/> with a frame of its own.</summary>
    public static IServiceCollection AddReaderCompanionUI(this IServiceCollection services)
    {
        services.AddScoped<ISystemTheme, JsSystemTheme>();
        services.AddScoped<IWindowKeys, JsWindowKeys>();
        services.AddScoped<IInView, JsInView>();
        services.TryAddSingleton<IAppWindow, NoAppWindow>();
        services.AddSingleton<ScanStatus>();
        return services;
    }
}
