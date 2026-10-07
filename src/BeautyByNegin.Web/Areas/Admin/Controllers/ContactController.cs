using BeautyByNegin.Business.Admin;
using BeautyByNegin.Business.Media;
using BeautyByNegin.Business.Settings;
using BeautyByNegin.DataAccess;
using BeautyByNegin.DataAccess.Entities;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Areas.Admin.Controllers;

public class ContactController(IAdminCatalogService catalog, IAdminTextService texts, ISettingsService settings, IMediaService media, IAdminData data) : AdminController
{
    private static readonly string[] AddressKeys = ["contact.address", "contact.byAppointment"];
    private static readonly string[] TemplateKeys = ["wa.general", "wa.service", "wa.booking", "wa.reply", "mail.subject.general", "mail.subject.service", "mail.subject.reply"];

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["Address"] = await texts.GetTextsByKeysAsync(AddressKeys, Ct);
        ViewData["Templates"] = await texts.GetTextsByKeysAsync(TemplateKeys, Ct);
        var firstDay = WeekStart.For(P.Settings.Text(BeautyByNegin.DataAccess.SettingKeys.CountryCode));
        ViewData["Hours"] = (await catalog.GetOpeningHoursAsync(Ct))
            .OrderBy(h => WeekStart.Position(h.Day, firstDay)).ToList();
        ViewData["Instagram"] = await catalog.GetInstagramPostsAsync(Ct);
        ViewData["InstagramTitle"] = await texts.GetTextsByKeysAsync(["home.instagram.title"], Ct);
        ViewData["Map"] = P.Settings.MapImageId is int id && await media.GetAsync(id) is { } m ? MediaUrls.Url(m, m.WidthList.First(w => w >= Math.Min(480, m.WidthList.Max()))) : null;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Index(string? phone, string? whatsApp, string? email, string? instagram, string? telegram,
        string? mapUrl, int? mapImageId, List<OpeningHourInput> hours)
    {
        if (!string.IsNullOrWhiteSpace(email) && !Business.Inbox.InboxService.IsValidEmail(email))
        {
            Problem("err.emailInvalid");
            return Back("contact");
        }
        await settings.SaveAsync(new Dictionary<string, string?>
        {
            [SettingKeys.Phone] = phone?.Trim(),
            [SettingKeys.WhatsApp] = whatsApp?.Trim(),
            [SettingKeys.Email] = email?.Trim(),
            [SettingKeys.InstagramUsername] = instagram?.Trim().TrimStart('@'),
            [SettingKeys.TelegramUsername] = telegram?.Trim().TrimStart('@'),
            [SettingKeys.MapUrl] = string.IsNullOrWhiteSpace(mapUrl) ? null : mapUrl.Trim().StartsWith("http") ? mapUrl.Trim() : "https://" + mapUrl.Trim(),
            [SettingKeys.MapImageId] = mapImageId?.ToString()
        }, Ct);
        await texts.SaveTextsAsync(await TextForm.ReadAsync(Request), Ct);
        if (hours.Count > 0) await catalog.SaveOpeningHoursAsync(hours, Ct);
        data.Changed();
        Saved();
        return Back("contact");
    }

    // ---------------- Instagram block photos

    [HttpPost]
    public async Task<IActionResult> AddInstagram(string ids)
    {
        await catalog.AddInstagramPostsAsync(ServicesController.ParseIds(ids), Ct);
        Saved();
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> InstagramLink(int id, string? url)
        => await catalog.SetInstagramLinkAsync(id, url, Ct) ? Ok() : Fail("err.notFound");

    [HttpPost] public async Task<IActionResult> ToggleInstagram(int id) => Ok(await data.ToggleVisibilityAsync<InstagramPost>(id, Ct));

    [HttpPost]
    public async Task<IActionResult> SortInstagram(string ids)
    {
        await data.ReorderAsync<InstagramPost>(ServicesController.ParseIds(ids), Ct);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> DeleteInstagram(int id) => await data.MoveToTrashAsync<InstagramPost>(id, Ct) ? Ok() : Fail("err.notFound");
}
