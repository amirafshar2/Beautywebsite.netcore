using BeautyByNegin.Business.Content;
using BeautyByNegin.Business.Settings;
using BeautyByNegin.Web.Infrastructure.Localization;

namespace BeautyByNegin.Web.Infrastructure;

/// <summary>Everything a public page and its layout need: language, texts, settings, contact links, footer data.</summary>
public sealed class PageContext
{
    public required SiteLanguage Lang { get; init; }
    public required SiteLanguage DefaultLang { get; init; }
    public required IReadOnlyList<SiteLanguage> Languages { get; init; }
    public required TextBundle T { get; init; }
    public required SiteSettings Settings { get; init; }
    public required ContactInfo Contact { get; init; }
    public required LayoutData Layout { get; init; }

    /// <summary>URL of the current page in every enabled language (language switcher + hreflang).</summary>
    public Dictionary<string, string> Alternates { get; set; } = [];

    /// <summary>Menu item to highlight, e.g. "services".</summary>
    public string? ActiveNav { get; set; }

    /// <summary>The visiting customer (logged in / waiting for the code / anonymous).</summary>
    public Business.Customers.CurrentCustomer Customer { get; set; } = Business.Customers.CurrentCustomer.Anonymous;

    public string Code => Lang.Code;

    /// <summary>Formats a number with Persian digits when the language uses them.</summary>
    public string Num(int n) => Digits.Format(n, Lang);
}

/// <summary>
/// Scoped provider: builds the <see cref="PageContext"/> once per request (all parts are cached in memory).
/// Controllers set <see cref="PageContext.Alternates"/>; views use <c>@inject SiteContext Site</c>.
/// </summary>
public sealed class SiteContext(
    IHttpContextAccessor http,
    ILanguageService languages,
    ITextService texts,
    ISettingsService settings,
    ILayoutService layout)
{
    private PageContext? _ctx;

    public async Task<PageContext> GetAsync()
    {
        if (_ctx is not null) return _ctx;

        var context = http.HttpContext!;
        var lang = await context.GetSiteLanguageOrDefaultAsync();
        var s = await settings.GetAsync(context.RequestAborted);
        var enabled = await languages.GetEnabledAsync(context.RequestAborted);

        _ctx = new PageContext
        {
            Lang = lang,
            DefaultLang = await languages.GetDefaultAsync(context.RequestAborted),
            Languages = enabled,
            T = await texts.GetAsync(lang.Code, context.RequestAborted),
            Settings = s,
            Contact = ContactLinks.From(s),
            Layout = await layout.GetAsync(lang, context.RequestAborted),
            // Default: same path with another language prefix (controllers override for translated URLs).
            Alternates = enabled.ToDictionary(l => l.Code, l => CultureUrl.ReplaceCulture(context.Request, l.Code))
        };
        return _ctx;
    }
}
