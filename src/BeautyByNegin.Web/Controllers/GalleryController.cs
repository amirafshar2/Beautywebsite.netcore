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

    /// <summary>Test page: the same photos as a 3D fly-through (WebGL). Not linked in the menu, not indexed.</summary>
    [HttpGet]
    public async Task<IActionResult> Immersive()
    {
        Page(SiteRoutes.Gallery3d);
        Ctx.ActiveNav = SiteRoutes.Gallery; // "Gallery" stays highlighted in the menu
        Seo(Ctx.T["gallery.title"], Ctx.T["gallery.intro"]);
        ViewData["NoIndex"] = true;
        return View(await Content.GetGalleryAsync(Ctx.Lang, HttpContext.RequestAborted));
    }
}
