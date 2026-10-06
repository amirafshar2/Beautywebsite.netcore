using BeautyByNegin.Web.Infrastructure.Routing;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Controllers;

public class HomeController : PublicController
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        Ctx.ActiveNav = "home";
        Ctx.Alternates = Ctx.Languages.ToDictionary(l => l.Code, l => SiteUrls.Home(l.Code));
        Seo(null, Ctx.T["seo.home.description"]);
        return View(await Content.GetHomeAsync(Ctx.Lang, HttpContext.RequestAborted));
    }
}
