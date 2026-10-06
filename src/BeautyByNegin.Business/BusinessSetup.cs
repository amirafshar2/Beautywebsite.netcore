using BeautyByNegin.Business.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace BeautyByNegin.Business;

public static class BusinessSetup
{
    /// <summary>Registers all business-layer services.</summary>
    public static IServiceCollection AddBusiness(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<ILanguageService, LanguageService>();
        return services;
    }
}
