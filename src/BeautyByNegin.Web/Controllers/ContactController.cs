using BeautyByNegin.Web.Infrastructure.Routing;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Controllers;

public class ContactController : PublicController
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        Page(SiteRoutes.Contact);
        Seo(Ctx.T["contact.title"], Ctx.T["contact.intro"]);
        return View(await Content.GetContactAsync(Ctx.Lang, HttpContext.RequestAborted));
    }
}
