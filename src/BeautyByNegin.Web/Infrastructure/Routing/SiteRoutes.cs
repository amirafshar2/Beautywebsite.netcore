namespace BeautyByNegin.Web.Infrastructure.Routing;

/// <summary>
/// Public pages and their URL segment in each language, e.g. services → /de/behandlungen, /en/treatments.
/// One conventional route is registered per page and language, so URLs stay clean and translated.
/// </summary>
public static class SiteRoutes
{
    public sealed record PageRoute(string Key, string Controller, string Action, bool HasSlug, IReadOnlyDictionary<string, string> Segments);

    public const string About = "about";
    public const string Services = "services";
    public const string ServiceDetail = "service";
    public const string Gallery = "gallery";
    public const string Reviews = "reviews";
    public const string Contact = "contact";
    public const string Booking = "booking";
    public const string Account = "account";

    /// <summary>Persian and Arabic use the English segments (Latin URLs are easier to share and type).</summary>
    private static Dictionary<string, string> Seg(string fa, string tr, string de, string en)
        => new() { ["fa"] = fa, ["tr"] = tr, ["de"] = de, ["en"] = en, ["ar"] = en };

    private static readonly Dictionary<string, string> ServiceSegments = Seg("services", "hizmetler", "behandlungen", "treatments");

    public static readonly PageRoute[] All =
    [
        new(About, "About", "Index", false, Seg("about", "hakkimizda", "ueber-uns", "about")),
        new(Services, "Services", "Index", false, ServiceSegments),
        new(ServiceDetail, "Services", "Detail", true, ServiceSegments),
        new(Gallery, "Gallery", "Index", false, Seg("gallery", "galeri", "galerie", "gallery")),
        new(Reviews, "Reviews", "Index", false, Seg("reviews", "yorumlar", "bewertungen", "reviews")),
        new(Contact, "Contact", "Index", false, Seg("contact", "iletisim", "kontakt", "contact")),
        new(Booking, "Booking", "Index", false, Seg("booking", "randevu", "termin", "booking")),
        new(Account, "Account", "Index", false, Seg("account", "hesabim", "konto", "account")),
    ];

    /// <summary>Registers all localized routes. Must be called before the generic "{culture}/{slug}" page route.</summary>
    public static void MapSiteRoutes(this IEndpointRouteBuilder app)
    {
        foreach (var page in All)
        {
            foreach (var (lang, segment) in page.Segments)
            {
                var pattern = page.HasSlug ? $"{lang}/{segment}/{{slug}}" : $"{lang}/{segment}";
                app.MapControllerRoute(
                    name: $"{page.Key}-{lang}",
                    pattern: pattern,
                    defaults: new { controller = page.Controller, action = page.Action, culture = lang });
            }
        }

        // Home: /fa, /de …
        app.MapControllerRoute("home", "{culture:culture}", new { controller = "Home", action = "Index" });

        // Small public endpoints used by forms/widgets: /fa/api/newsletter …
        app.MapControllerRoute("site-api", "{culture:culture}/api/{controller}/{action}");

        // Custom pages (Impressum, privacy, "aftercare tips" …): /de/impressum
        app.MapControllerRoute("page", "{culture:culture}/{slug}", new { controller = "Pages", action = "Show" });
    }

    public static PageRoute Get(string key) => All.First(p => p.Key == key);
}

/// <summary>Builds public URLs. All links in views go through here so translated segments stay consistent.</summary>
public static class SiteUrls
{
    public static string Home(string lang) => $"/{lang}";

    public static string Page(string key, string lang)
    {
        var route = SiteRoutes.Get(key);
        return $"/{lang}/{route.Segments[lang]}";
    }

    public static string Service(string lang, string slug) => $"/{lang}/{SiteRoutes.Get(SiteRoutes.ServiceDetail).Segments[lang]}/{slug}";

    public static string Booking(string lang, int? serviceId = null)
        => serviceId is null ? Page(SiteRoutes.Booking, lang) : $"{Page(SiteRoutes.Booking, lang)}?service={serviceId}";

    public static string CustomPage(string lang, string slug) => $"/{lang}/{slug}";

    /// <summary>Same page in all languages (for the language switcher and hreflang tags).</summary>
    public static Dictionary<string, string> Alternates(string key, IEnumerable<string> languages)
        => languages.ToDictionary(l => l, l => Page(key, l));
}
