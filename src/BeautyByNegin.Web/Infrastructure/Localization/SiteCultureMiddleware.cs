using System.Globalization;

namespace BeautyByNegin.Web.Infrastructure.Localization;

/// <summary>
/// Runs after routing. Reads the {culture} route value, redirects disabled languages to the default
/// language (same page), and sets the request culture + <see cref="SiteLanguage"/> for views.
/// </summary>
public sealed class SiteCultureMiddleware(RequestDelegate next)
{
    public const string ItemKey = "SiteLanguage";

    public async Task InvokeAsync(HttpContext context, ILanguageService languages)
    {
        var code = context.GetRouteValue("culture") as string;
        if (code is not null)
        {
            var lang = await languages.FindAsync(code, context.RequestAborted);
            if (lang is null || !lang.IsEnabled)
            {
                var fallback = await languages.GetDefaultAsync(context.RequestAborted);
                context.Response.Redirect(CultureUrl.ReplaceCulture(context.Request, fallback.Code), permanent: false);
                return;
            }

            // Canonical lower-case code in URLs: /FA/... -> /fa/...
            if (!string.Equals(code, lang.Code, StringComparison.Ordinal))
            {
                context.Response.Redirect(CultureUrl.ReplaceCulture(context.Request, lang.Code), permanent: true);
                return;
            }

            Apply(context, lang);
        }

        await next(context);
    }

    public static void Apply(HttpContext context, SiteLanguage lang)
    {
        var culture = lang.CreateCulture();
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        context.Items[ItemKey] = lang;
    }
}

public static class CultureUrl
{
    /// <summary>Replaces (or adds) the language segment of the current URL, keeping path and query.</summary>
    public static string ReplaceCulture(HttpRequest request, string newCode)
    {
        var path = request.Path.Value ?? "/";
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();

        if (segments.Count > 0 && LanguageService.KnownCodes.Contains(segments[0], StringComparer.OrdinalIgnoreCase))
            segments[0] = newCode;
        else
            segments.Insert(0, newCode);

        return request.PathBase + "/" + string.Join('/', segments) + request.QueryString;
    }
}
