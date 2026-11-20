using Microsoft.Extensions.DependencyInjection;

namespace ReaderCompanion.UI;

public static class UIServiceCollectionExtensions
{
    /// <summary>Services the UI provides itself; the host adds <see cref="IFolderPicker"/> and
    /// <see cref="ICoverUrls"/>.</summary>
    public static IServiceCollection AddReaderCompanionUI(this IServiceCollection services) =>
        services.AddScoped<ISystemTheme, JsSystemTheme>();
}
