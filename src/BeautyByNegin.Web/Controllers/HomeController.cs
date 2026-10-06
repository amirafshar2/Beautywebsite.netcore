using BeautyByNegin.Web.Infrastructure;
using BeautyByNegin.Web.Infrastructure.Routing;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Controllers;

public class HomeController(SiteContext site) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var ctx = await site.GetAsync();
        ctx.ActiveNav = "home";
        ctx.Alternates = ctx.Languages.ToDictionary(l => l.Code, l => SiteUrls.Home(l.Code));
        return View();
    }
}
