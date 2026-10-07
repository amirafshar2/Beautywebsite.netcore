using BeautyByNegin.Business.Admin;
using BeautyByNegin.Business.Media;
using BeautyByNegin.Business.Settings;
using BeautyByNegin.DataAccess;
using BeautyByNegin.DataAccess.Entities;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Areas.Admin.Controllers;

public class AboutController(IAdminCatalogService catalog, IAdminTextService texts, ISettingsService settings, IMediaService media, IAdminData data) : AdminController
{
    private static readonly string[] TextKeys =
        ["about.eyebrow", "about.title", "about.intro", "about.expertise.title", "about.philosophy.title", "about.philosophy.text", "about.certificates.title"];

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["Expertise"] = await catalog.GetListItemsAsync(ListKeys.Expertise, Ct);
        ViewData["Certificates"] = await catalog.GetListItemsAsync(ListKeys.Certificates, Ct);
        ViewData["Image"] = P.Settings.AboutImageId is int id && await media.GetAsync(id) is { } m ? MediaUrls.Url(m, m.WidthList.First(w => w >= Math.Min(480, m.WidthList.Max()))) : null;
        return View(await texts.GetTextsByKeysAsync(TextKeys, Ct));
    }

    [HttpPost]
    public async Task<IActionResult> Index(int? aboutImageId)
    {
        await texts.SaveTextsAsync(await TextForm.ReadAsync(Request), Ct);
        await settings.SaveAsync(new Dictionary<string, string?> { [SettingKeys.AboutImageId] = aboutImageId?.ToString() }, Ct);
        data.Changed();
        Saved();
        return Back("about");
    }

    /// <summary>Add or edit an item of the specialties ("expertise") or certificates list.</summary>
    [HttpPost]
    public async Task<IActionResult> SaveItem(string listKey, NamedInput input)
    {
        if (listKey is not (ListKeys.Expertise or ListKeys.Certificates)) return BadRequest();
        var result = await catalog.SaveListItemAsync(listKey, input, P.DefaultLanguage.Code, Ct);
        if (result.Ok) Saved(); else Problem("err.textRequired");
        return Back("about#" + listKey);
    }

    [HttpPost] public async Task<IActionResult> ToggleItem(int id) => Ok(await data.ToggleVisibilityAsync<ListItem>(id, Ct));

    [HttpPost]
    public async Task<IActionResult> SortItems(string ids)
    {
        await data.ReorderAsync<ListItem>(ServicesController.ParseIds(ids), Ct);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> DeleteItem(int id) => await data.MoveToTrashAsync<ListItem>(id, Ct) ? Ok() : Fail("err.notFound");
}
