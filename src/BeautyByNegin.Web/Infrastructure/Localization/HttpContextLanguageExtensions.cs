namespace BeautyByNegin.Web.Infrastructure.Localization;

public static class HttpContextLanguageExtensions
{
    /// <summary>The language of the current public page (set by <see cref="SiteCultureMiddleware"/>).</summary>
    public static SiteLanguage? GetSiteLanguage(this HttpContext context)
        => context.Items.TryGetValue(SiteCultureMiddleware.ItemKey, out var v) ? v as SiteLanguage : null;

    /// <summary>The current language, or the default language when the URL has none (e.g. 404 page).</summary>
    public static async Task<SiteLanguage> GetSiteLanguageOrDefaultAsync(this HttpContext context)
    {
        var current = context.GetSiteLanguage();
        if (current is not null) return current;

        var languages = context.RequestServices.GetRequiredService<ILanguageService>();
        var lang = await languages.GetDefaultAsync(context.RequestAborted);
        SiteCultureMiddleware.Apply(context, lang);
        return lang;
    }
}
