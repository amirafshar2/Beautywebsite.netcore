namespace BeautyByNegin.Web.Infrastructure.Localization;

/// <summary>
/// Matches the first URL segment against the known language codes (fa, tr, de, en).
/// Whether the language is currently switched on is checked later by <see cref="SiteCultureMiddleware"/>,
/// so a disabled language redirects to the default one instead of returning 404.
/// </summary>
public sealed class CultureRouteConstraint : IRouteConstraint
{
    public const string Name = "culture";

    public bool Match(HttpContext? httpContext, IRouter? route, string routeKey,
        RouteValueDictionary values, RouteDirection routeDirection)
    {
        if (!values.TryGetValue(routeKey, out var raw) || raw is null) return false;
        var value = Convert.ToString(raw, System.Globalization.CultureInfo.InvariantCulture);
        return LanguageService.KnownCodes.Contains(value, StringComparer.OrdinalIgnoreCase);
    }
}
