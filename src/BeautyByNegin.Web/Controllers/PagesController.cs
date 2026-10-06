using BeautyByNegin.Web.Infrastructure.Routing;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Controllers;

/// <summary>Impressum, privacy policy and custom pages created in the panel: /{lang}/{slug}.</summary>
public class PagesController : PublicController
{
    [HttpGet]
    public async Task<IActionResult> Show(string slug)
    {
        var lookup = await Content.GetPageAsync(Ctx.Lang, slug, HttpContext.RequestAborted);
        if (lookup.RedirectSlug is not null)
            return RedirectPermanent(SiteUrls.CustomPage(Ctx.Code, lookup.RedirectSlug));
        if (lookup.Page is not { } page)
            return NotFound();

        Ctx.Alternates = Ctx.Languages.ToDictionary(l => l.Code,
            l => SiteUrls.CustomPage(l.Code, page.Slugs.TryGetValue(l.Code, out var s) ? s : slug));
        Seo(string.IsNullOrWhiteSpace(page.MetaTitle) ? page.Title : page.MetaTitle, page.MetaDescription ?? page.ContentHtml);
        return View(page);
    }
}
