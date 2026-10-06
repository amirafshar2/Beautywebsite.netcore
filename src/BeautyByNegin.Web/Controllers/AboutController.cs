using BeautyByNegin.Web.Infrastructure.Routing;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Controllers;

public class AboutController : PublicController
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        Page(SiteRoutes.About);
        Seo(Ctx.T["nav.about"], Ctx.T["about.intro"]);
        return View(await Content.GetAboutAsync(Ctx.Lang, HttpContext.RequestAborted));
    }
}
