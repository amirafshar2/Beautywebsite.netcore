using BeautyByNegin.Web.Infrastructure.Localization;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Controllers;

/// <summary>Friendly error pages; never exposes technical details (those go to the log file).</summary>
[ApiExplorerSettings(IgnoreApi = true)]
public class ErrorController : Controller
{
    [HttpGet, HttpPost]
    public async Task<IActionResult> Index(int? code)
    {
        await HttpContext.GetSiteLanguageOrDefaultAsync();
        var status = code ?? 500;
        Response.StatusCode = status;
        return View(status == 404 ? "NotFound" : "Error");
    }
}
