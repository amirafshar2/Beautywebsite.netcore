using BeautyByNegin.Business.Admin;
using BeautyByNegin.Business.Media;
using BeautyByNegin.Business.Notifications;
using BeautyByNegin.Business.Settings;
using BeautyByNegin.DataAccess;
using BeautyByNegin.DataAccess.Entities;
using BeautyByNegin.Web.Infrastructure.Startup;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Areas.Admin.Controllers;

[Authorize(Policy = Policies.AdminOnly)]
public class SettingsController(
    ISettingsService settings,
    IAdminCatalogService catalog,
    IMediaService media,
    IEmailSender email,
    ITelegramNotifier telegram,
    IAdminData data) : AdminController
{
    /// <summary>Countries the business moves through, with their usual time zone.</summary>
    public static readonly (string Code, string TimeZone)[] Countries =
        [("IR", "Asia/Tehran"), ("TR", "Europe/Istanbul"), ("DE", "Europe/Berlin"), ("AT", "Europe/Vienna"), ("CH", "Europe/Zurich"), ("AE", "Asia/Dubai"), ("GB", "Europe/London")];

    public static readonly string[] TimeZones =
        ["Asia/Tehran", "Europe/Istanbul", "Europe/Berlin", "Europe/Vienna", "Europe/Zurich", "Europe/London", "Europe/Paris", "Asia/Dubai", "UTC"];

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["Slots"] = await catalog.GetTimeSlotsAsync(HttpContext.RequestAborted);
        ViewData["Images"] = new Dictionary<string, string?>
        {
            ["logo"] = await Preview(P.Settings.LogoImageId),
            ["logoDark"] = await Preview(P.Settings.LogoDarkImageId),
            ["favicon"] = await Preview(P.Settings.FaviconImageId),
            ["share"] = await Preview(P.Settings.ShareImageId),
        };
        ViewData["HasSmtpPassword"] = await settings.GetSecretAsync(SettingKeys.SmtpPassword) is not null;
        ViewData["HasTelegramToken"] = await settings.GetSecretAsync(SettingKeys.TelegramBotToken) is not null;
        ViewData["HasGeminiKey"] = await settings.GetSecretAsync(SettingKeys.AiGeminiKey) is not null;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Index(IFormCollection form)
    {
        string? V(string name) => form.TryGetValue(name, out var v) ? v.ToString().Trim() : null;
        string B(string name) => form.TryGetValue(name, out var v) && v.Contains("true") ? "true" : "false";

        var values = new Dictionary<string, string?>
        {
            [SettingKeys.BrandName] = string.IsNullOrWhiteSpace(V("brandName")) ? "Beauty by Negin" : V("brandName"),
            [SettingKeys.LogoImageId] = V("logoImageId"),
            [SettingKeys.LogoDarkImageId] = V("logoDarkImageId"),
            [SettingKeys.FaviconImageId] = V("faviconImageId"),
            [SettingKeys.ShareImageId] = V("shareImageId"),
            [SettingKeys.SiteUrl] = V("siteUrl")?.TrimEnd('/'),
            [SettingKeys.City] = V("city"),
            [SettingKeys.AddCityToTitles] = B("addCityToTitles"),
            [SettingKeys.CountryCode] = V("country"),
            [SettingKeys.TimeZoneId] = TimeZones.Contains(V("timeZone")) ? V("timeZone") : "UTC",
            [SettingKeys.ShowPrices] = B("showPrices"),
            [SettingKeys.AllowVisitorReviews] = B("visitorReviews"),
            [SettingKeys.PrivacyConsentRequired] = B("privacyRequired"),
            [SettingKeys.NewsletterEnabled] = B("newsletter"),
            [SettingKeys.ChatEnabled] = B("chat"),
            [SettingKeys.AccountsEnabled] = B("accounts"),
            [SettingKeys.AccountsShowBookings] = B("accountsBookings"),
            [SettingKeys.ReviewsRequireLogin] = B("reviewsRequireLogin"),
            [SettingKeys.MaintenanceMode] = B("maintenance"),
            [SettingKeys.SmtpEnabled] = B("smtpEnabled"),
            [SettingKeys.SmtpHost] = V("smtpHost"),
            [SettingKeys.SmtpPort] = int.TryParse(V("smtpPort"), out var port) && port is > 0 and < 65536 ? port.ToString() : "587",
            [SettingKeys.SmtpUseSsl] = B("smtpSsl"),
            [SettingKeys.SmtpUser] = V("smtpUser"),
            [SettingKeys.SmtpFromAddress] = V("smtpFrom"),
            [SettingKeys.SmtpFromName] = V("smtpFromName"),
            [SettingKeys.NotificationEmail] = V("notifyEmail"),
            [SettingKeys.TelegramEnabled] = B("telegramEnabled"),
            [SettingKeys.GoogleVerification] = GoogleCode(V("googleVerification")),
            [SettingKeys.AiModel] = string.IsNullOrWhiteSpace(V("aiModel")) ? BeautyByNegin.Business.Ai.GeminiTranslator.DefaultModel : V("aiModel")!.Trim(),
            [SettingKeys.TelegramChatId] = V("telegramChatId"),
            [SettingKeys.TelegramNotifyAppointments] = B("tgAppointments"),
            [SettingKeys.TelegramNotifyMessages] = B("tgMessages"),
            [SettingKeys.TelegramNotifyChat] = B("tgChat"),
            [SettingKeys.TelegramNotifyNewsletter] = B("tgNewsletter"),
            [SettingKeys.HttpsRedirect] = B("httpsRedirect"),
            [SettingKeys.Hsts] = B("hsts"),
            [SettingKeys.AutoBackupEnabled] = B("autoBackup"),
        };
        await settings.SaveAsync(values, HttpContext.RequestAborted);

        // Secrets: an empty field keeps the saved value; the "remove" box clears it.
        if (!string.IsNullOrEmpty(V("smtpPassword"))) await settings.SaveSecretAsync(SettingKeys.SmtpPassword, V("smtpPassword"));
        else if (B("smtpPasswordClear") == "true") await settings.SaveSecretAsync(SettingKeys.SmtpPassword, null);
        if (!string.IsNullOrEmpty(V("geminiKey"))) await settings.SaveSecretAsync(SettingKeys.AiGeminiKey, V("geminiKey")!.Trim());
        else if (B("geminiKeyClear") == "true") await settings.SaveSecretAsync(SettingKeys.AiGeminiKey, null);
        if (!string.IsNullOrEmpty(V("telegramToken"))) await settings.SaveSecretAsync(SettingKeys.TelegramBotToken, V("telegramToken"));
        else if (B("telegramTokenClear") == "true") await settings.SaveSecretAsync(SettingKeys.TelegramBotToken, null);

        data.Changed();
        Saved();
        return Back("settings#" + (V("section") ?? ""));
    }

    /// <summary>"Send test e-mail" — uses the SAVED settings.</summary>
    [HttpPost]
    public async Task<IActionResult> TestEmail(string? to)
    {
        to ??= P.Settings.Text(SettingKeys.NotificationEmail);
        if (string.IsNullOrWhiteSpace(to)) return Fail("set.testEmailNoAddress");
        var result = await email.SendAsync(to, P["set.testEmailSubject"], P["set.testEmailBody"], null, HttpContext.RequestAborted);
        return result.Ok ? Json(new { ok = true, message = P["set.testEmailOk"] }) : Json(new { ok = false, message = P["set.testEmailFail"] + " (" + result.Error + ")" });
    }

    /// <summary>"Send test message" to Telegram — uses the SAVED token and chat id.</summary>
    [HttpPost]
    public async Task<IActionResult> TestTelegram()
    {
        var token = await settings.GetSecretAsync(SettingKeys.TelegramBotToken);
        var chat = P.Settings.Text(SettingKeys.TelegramChatId);
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(chat)) return Fail("set.tgMissing");
        var r = await telegram.SendWithAsync(token, chat, "✅ " + P["set.tgTestText"], HttpContext.RequestAborted);
        return r.Ok ? Json(new { ok = true, message = P["set.tgTestOk"] }) : Json(new { ok = false, message = P["set." + r.Error] });
    }

    /// <summary>Finds the Chat ID of people who wrote "hello" to the bot.</summary>
    [HttpPost]
    public async Task<IActionResult> FindChatId()
    {
        var token = await settings.GetSecretAsync(SettingKeys.TelegramBotToken);
        if (string.IsNullOrWhiteSpace(token)) return Fail("set.tgMissing");
        var (result, chats) = await telegram.DiscoverChatsAsync(token, HttpContext.RequestAborted);
        if (!result.Ok) return Json(new { ok = false, message = P["set." + result.Error] });
        if (chats.Count == 0) return Json(new { ok = false, message = P["set.tgNoChats"] });
        return Json(new { ok = true, message = P["set.tgFound"], data = chats.Select(c => new { id = c.ChatId, name = c.Name }) });
    }

    // ---------------- booking time options

    [HttpPost]
    public async Task<IActionResult> SaveSlot(NamedInput input)
    {
        var r = await catalog.SaveTimeSlotAsync(input, P.DefaultLanguage.Code, HttpContext.RequestAborted);
        if (r.Ok) Saved(); else Problem("err.textRequired");
        return Back("settings#slots");
    }

    [HttpPost] public async Task<IActionResult> ToggleSlot(int id) => Ok(await data.ToggleVisibilityAsync<TimeSlot>(id, HttpContext.RequestAborted));
    [HttpPost] public async Task<IActionResult> DeleteSlot(int id) => await data.MoveToTrashAsync<TimeSlot>(id, HttpContext.RequestAborted) ? Ok() : Fail("err.notFound");

    [HttpPost]
    public async Task<IActionResult> SortSlots(string ids)
    {
        await data.ReorderAsync<TimeSlot>(ServicesController.ParseIds(ids), HttpContext.RequestAborted);
        return Ok();
    }

    private async Task<string?> Preview(int? id)
        => id is int i && await media.GetAsync(i, HttpContext.RequestAborted) is { } m ? MediaUrls.Url(m, m.WidthList.Min()) : null;

    /// <summary>Accepts the whole meta tag from Google or only its code; keeps only the code.</summary>
    internal static string? GoogleCode(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var m = System.Text.RegularExpressions.Regex.Match(input, "content\\s*=\\s*[\"']([^\"']+)[\"']");
        var code = (m.Success ? m.Groups[1].Value : input).Trim();
        return System.Text.RegularExpressions.Regex.IsMatch(code, "^[A-Za-z0-9_\\-]{10,100}$") ? code : null;
    }
}
