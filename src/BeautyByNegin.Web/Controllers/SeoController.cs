using System.Text;
using System.Xml;
using BeautyByNegin.Business.Content;
using BeautyByNegin.Business.Localization;
using BeautyByNegin.Business.Settings;
using BeautyByNegin.Web.Infrastructure.Routing;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Controllers;

/// <summary>/sitemap.xml (all enabled languages, with hreflang alternates) and /robots.txt.</summary>
public class SeoController(ILanguageService languages, IContentService content, ISettingsService settings) : Controller
{
    private async Task<string> BaseUrl()
    {
        var s = await settings.GetAsync();
        return string.IsNullOrWhiteSpace(s.SiteUrl) ? $"{Request.Scheme}://{Request.Host}{Request.PathBase}" : s.SiteUrl;
    }

    [HttpGet("/robots.txt")]
    public async Task<IActionResult> Robots()
    {
        var s = await settings.GetAsync();
        var adminPath = Infrastructure.Startup.IdentitySetup.AdminPath(HttpContext.RequestServices.GetRequiredService<IConfiguration>());
        var text = s.MaintenanceMode
            ? "User-agent: *\nDisallow: /\n"
            : $"User-agent: *\nDisallow: {adminPath}/\nDisallow: /*/api/\nAllow: /\n\nSitemap: {await BaseUrl()}/sitemap.xml\n";
        return Content(text, "text/plain", Encoding.UTF8);
    }

    [HttpGet("/sitemap.xml")]
    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> Sitemap()
    {
        var baseUrl = await BaseUrl();
        var langs = await languages.GetEnabledAsync();
        var codes = langs.Select(l => l.Code).ToList();

        // Every URL group = the same page in all languages (each URL lists its alternates).
        var groups = new List<Dictionary<string, string>>
        {
            codes.ToDictionary(c => c, SiteUrls.Home)
        };
        foreach (var key in new[] { SiteRoutes.About, SiteRoutes.Services, SiteRoutes.Gallery, SiteRoutes.Reviews, SiteRoutes.Contact, SiteRoutes.Booking })
            groups.Add(SiteUrls.Alternates(key, codes));

        var first = langs.First();
        foreach (var service in await content.GetServicesAsync(first))
        {
            var lookup = await content.GetServiceAsync(first, service.Slug);
            if (lookup.Service is null) continue;
            groups.Add(codes.ToDictionary(c => c, c => SiteUrls.Service(c, lookup.Service.Slugs.TryGetValue(c, out var s) ? s : service.Slug)));
        }
        foreach (var lang in langs.Take(1))
        {
            var layout = await HttpContext.RequestServices.GetRequiredService<ILayoutService>().GetAsync(lang);
            foreach (var page in layout.FooterPages.Concat(layout.MenuPages).DistinctBy(p => p.Slug))
            {
                var found = await content.GetPageAsync(lang, page.Slug);
                if (found.Page is null) continue;
                groups.Add(codes.ToDictionary(c => c, c => SiteUrls.CustomPage(c, found.Page.Slugs.TryGetValue(c, out var s) ? s : page.Slug)));
            }
        }

        var sb = new StringBuilder();
        using (var xml = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 }))
        {
            xml.WriteStartDocument();
            xml.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");
            xml.WriteAttributeString("xmlns", "xhtml", null, "http://www.w3.org/1999/xhtml");
            foreach (var group in groups)
            {
                foreach (var (code, url) in group)
                {
                    xml.WriteStartElement("url");
                    xml.WriteElementString("loc", baseUrl + url);
                    foreach (var (altCode, altUrl) in group)
                    {
                        xml.WriteStartElement("xhtml", "link", "http://www.w3.org/1999/xhtml");
                        xml.WriteAttributeString("rel", "alternate");
                        xml.WriteAttributeString("hreflang", altCode);
                        xml.WriteAttributeString("href", baseUrl + altUrl);
                        xml.WriteEndElement();
                    }
                    xml.WriteEndElement();
                }
            }
            xml.WriteEndElement();
        }
        return Content(sb.ToString().Replace("encoding=\"utf-16\"", "encoding=\"utf-8\""), "application/xml", Encoding.UTF8);
    }
}
