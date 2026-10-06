using BeautyByNegin.Web.Infrastructure.Routing;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Controllers;

public class GalleryController : PublicController
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        Page(SiteRoutes.Gallery);
        Seo(Ctx.T["gallery.title"], Ctx.T["gallery.intro"]);
        return View(await Content.GetGalleryAsync(Ctx.Lang, HttpContext.RequestAborted));
    }
}
