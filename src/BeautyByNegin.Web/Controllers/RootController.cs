using BeautyByNegin.Web.Infrastructure.Localization;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Controllers;

/// <summary>Handles "/" — sends the visitor to the main language of the site (set in the panel).</summary>
public class RootController(ILanguageService languages) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // Always the main language chosen in the panel ("Languages"), not the browser language.
        var target = await languages.GetDefaultAsync(HttpContext.RequestAborted);

        // 302 (not 301): the default language changes when the business moves country.
        return Redirect($"{Request.PathBase}/{target.Code}");
    }
}
