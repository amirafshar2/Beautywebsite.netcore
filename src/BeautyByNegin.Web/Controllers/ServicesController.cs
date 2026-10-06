using BeautyByNegin.Web.Infrastructure.Routing;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Controllers;

public class ServicesController : PublicController
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        Page(SiteRoutes.Services);
        Seo(Ctx.T["services.title"], Ctx.T["services.intro"]);
        return View(await Content.GetServicesAsync(Ctx.Lang, HttpContext.RequestAborted));
    }

    [HttpGet]
    public async Task<IActionResult> Detail(string slug)
    {
        var lookup = await Content.GetServiceAsync(Ctx.Lang, slug, HttpContext.RequestAborted);
        if (lookup.RedirectSlug is not null)
            return RedirectPermanent(SiteUrls.Service(Ctx.Code, lookup.RedirectSlug));
        if (lookup.Service is not { } service)
            return NotFound();

        Ctx.ActiveNav = SiteRoutes.Services;
        Ctx.Alternates = Ctx.Languages.ToDictionary(l => l.Code,
            l => service.Slugs.TryGetValue(l.Code, out var s) ? SiteUrls.Service(l.Code, s) : SiteUrls.Service(l.Code, service.Card.Slug));
        Seo(string.IsNullOrWhiteSpace(service.MetaTitle) ? service.Card.Name : service.MetaTitle,
            service.MetaDescription ?? service.Card.ShortDescription ?? service.DescriptionHtml);
        var baseUrl = Infrastructure.Seo.StructuredData.BaseUrl(Ctx, Request);
        ViewData["JsonLd"] = Infrastructure.Seo.StructuredData.Service(Ctx, baseUrl, service, baseUrl + SiteUrls.Service(Ctx.Code, service.Card.Slug));
        if (!service.Card.Image.IsPlaceholder) ViewData["OgImage"] = service.Card.Image.Src;
        return View(service);
    }
}
