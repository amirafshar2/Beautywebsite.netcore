using BeautyByNegin.Web.Infrastructure.Routing;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Controllers;

public class ReviewsController : PublicController
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        Page(SiteRoutes.Reviews);
        Seo(Ctx.T["reviews.title"]);
        ViewBag.Services = await Content.GetServicesAsync(Ctx.Lang, HttpContext.RequestAborted);
        return View(await Content.GetReviewsAsync(Ctx.Lang, null, HttpContext.RequestAborted));
    }
}
