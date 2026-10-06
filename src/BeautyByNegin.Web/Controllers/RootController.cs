using BeautyByNegin.Web.Infrastructure.Localization;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Controllers;

/// <summary>Handles "/" — sends the visitor to the right language.</summary>
public class RootController(ILanguageService languages) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var enabled = await languages.GetEnabledAsync(HttpContext.RequestAborted);
        var target = await languages.GetDefaultAsync(HttpContext.RequestAborted);

        // Respect the browser language when the admin has enabled it.
        var accept = Request.GetTypedHeaders().AcceptLanguage;
        if (accept is { Count: > 0 })
        {
            foreach (var item in accept.OrderByDescending(a => a.Quality ?? 1))
            {
                var two = item.Value.Value?.Split('-')[0];
                var match = enabled.FirstOrDefault(l => string.Equals(l.Code, two, StringComparison.OrdinalIgnoreCase));
                if (match is not null) { target = match; break; }
            }
        }

        // 302 (not 301): the default language changes when the business moves country.
        return Redirect($"{Request.PathBase}/{target.Code}");
    }
}
