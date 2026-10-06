using BeautyByNegin.Business.Content;
using BeautyByNegin.Business.Localization;
using BeautyByNegin.Business.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace BeautyByNegin.Business;

public static class BusinessSetup
{
    /// <summary>Registers all business-layer services.</summary>
    public static IServiceCollection AddBusiness(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<ILanguageService, LanguageService>();
        services.AddSingleton<ITextService, TextService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ILayoutService, LayoutService>();
        return services;
    }
}
