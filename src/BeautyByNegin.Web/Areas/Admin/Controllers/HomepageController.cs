using BeautyByNegin.Business.Admin;
using BeautyByNegin.Business.Media;
using BeautyByNegin.Business.Settings;
using BeautyByNegin.DataAccess;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Areas.Admin.Controllers;

/// <summary>Home page: big photo + texts, blocks on/off and their order.</summary>
public class HomepageController(IAdminCatalogService catalog, IAdminTextService texts, ISettingsService settings, IMediaService media, IAdminData data) : AdminController
{
    public static readonly string[] TextKeys =
    [
        "home.hero.eyebrow", "home.hero.title", "home.hero.slogan", "btn.book", "btn.contact",
        "home.intro.eyebrow", "home.intro.title", "home.intro.text",
        "home.services.eyebrow", "home.services.title",
        "consult.eyebrow", "consult.title", "consult.text", "consult.question",
        "home.gallery.eyebrow", "home.gallery.title", "home.reviews.eyebrow", "home.reviews.title",
        "home.instagram.title", "home.newsletter.title", "home.newsletter.text",
        "home.contact.title", "home.contact.text", "seo.home.title", "seo.home.description"
    ];

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["Sections"] = await catalog.GetHomeSectionsAsync(Ct);
        ViewData["Hero"] = await Preview(P.Settings.HeroImageId);
        ViewData["Consult"] = await Preview(P.Settings.ConsultationImageId);
        return View(await texts.GetTextsByKeysAsync(TextKeys, Ct));
    }

    [HttpPost]
    public async Task<IActionResult> Index(int? heroImageId, int? consultationImageId)
    {
        var form = await ReadTexts();
        await texts.SaveTextsAsync(form, Ct);
        await settings.SaveAsync(new Dictionary<string, string?>
        {
            [SettingKeys.HeroImageId] = heroImageId?.ToString(),
            [SettingKeys.ConsultationImageId] = consultationImageId?.ToString()
        }, Ct);
        data.Changed();
        Saved();
        return Back("homepage");
    }

    [HttpPost] public async Task<IActionResult> ToggleSection(int id) => Ok(await catalog.ToggleHomeSectionAsync(id, Ct));

    [HttpPost]
    public async Task<IActionResult> SortSections(string ids)
    {
        await data.ReorderAsync<DataAccess.Entities.HomeSection>(ServicesController.ParseIds(ids), Ct);
        return Ok();
    }

    private async Task<string?> Preview(int? id)
    {
        if (id is not int i || await media.GetAsync(i, Ct) is not { } m) return null;
        return MediaUrls.Url(m, m.WidthList.First(w => w >= Math.Min(480, m.WidthList.Max())));
    }

    /// <summary>Reads "Texts[key][lang]" fields from the posted form.</summary>
    private async Task<Dictionary<string, Dictionary<string, string?>>> ReadTexts() => await TextForm.ReadAsync(Request);
}

public static class TextForm
{
    public static async Task<Dictionary<string, Dictionary<string, string?>>> ReadAsync(HttpRequest request)
    {
        var form = await request.ReadFormAsync();
        var result = new Dictionary<string, Dictionary<string, string?>>();
        foreach (var (name, value) in form)
        {
            if (!name.StartsWith("Texts[", StringComparison.Ordinal)) continue;
            var parts = name[6..].Split("][", 2);
            if (parts.Length != 2) continue;
            var key = parts[0];
            var lang = parts[1].TrimEnd(']');
            if (!result.TryGetValue(key, out var byLang)) result[key] = byLang = [];
            byLang[lang] = value.ToString();
        }
        return result;
    }
}
