using BeautyByNegin.Business.Admin;
using BeautyByNegin.Business.Inbox;
using BeautyByNegin.Business.Settings;
using BeautyByNegin.DataAccess;
using BeautyByNegin.DataAccess.Entities;
using BeautyByNegin.Web.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BeautyByNegin.Web.Areas.Admin.Controllers;

/// <summary>
/// First-run wizard: creates the first admin and the basic settings (language, country/time zone,
/// phone, WhatsApp, e-mail). Only available while no panel user exists.
/// </summary>
[AllowAnonymous]
public class SetupController(
    UserManager<AppUser> users,
    SignInManager<AppUser> signIn,
    ISettingsService settings,
    IAdminTextService texts,
    IAdminData data) : AdminController
{
    [HttpGet]
    public IActionResult Index() => users.Users.Any() ? Redirect(P.Url("login")) : View();

    [HttpPost, EnableRateLimiting(SpamGuard.FormsPolicy)]
    public async Task<IActionResult> Index(string? userName, string? displayName, string? password, string? confirm,
        string panelLanguage, string defaultLanguage, string country, string timeZone,
        string? phone, string? whatsApp, string? email)
    {
        if (users.Users.Any()) return Redirect(P.Url("login"));

        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(userName)) errors["userName"] = P["err.required"];
        if (string.IsNullOrEmpty(password) || password.Length < 8 || !password.Any(char.IsDigit)) errors["password"] = P["account.passwordRules"];
        else if (password != confirm) errors["confirm"] = P["account.passwordMismatch"];
        if (!string.IsNullOrWhiteSpace(email) && !InboxService.IsValidEmail(email)) errors["email"] = P["err.emailInvalid"];
        if (errors.Count > 0)
        {
            ViewData["Errors"] = errors;
            return View();
        }

        var user = new AppUser
        {
            UserName = userName!.Trim(),
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? userName.Trim() : displayName.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            PanelLanguage = PanelContext.PanelLanguages.Contains(panelLanguage) ? panelLanguage : "fa"
        };
        var result = await users.CreateAsync(user, password!);
        if (!result.Succeeded)
        {
            ViewData["Errors"] = new Dictionary<string, string> { ["userName"] = string.Join(" ", result.Errors.Select(e => e.Description)) };
            return View();
        }
        await users.AddToRoleAsync(user, AppRoles.Admin);

        // Default language: keep all languages, enable the chosen one and make it the default.
        var languages = await texts.GetLanguagesAsync();
        await texts.SaveLanguagesAsync(languages.Select(l => (l.Code, l.IsEnabled || l.Code == defaultLanguage, l.UseNativeDigits)).ToList(), defaultLanguage);

        await settings.SaveAsync(new Dictionary<string, string?>
        {
            [SettingKeys.CountryCode] = country,
            [SettingKeys.TimeZoneId] = SettingsController.TimeZones.Contains(timeZone) ? timeZone : "UTC",
            [SettingKeys.Phone] = phone?.Trim(),
            [SettingKeys.WhatsApp] = whatsApp?.Trim(),
            [SettingKeys.Email] = email?.Trim(),
            [SettingKeys.NotificationEmail] = email?.Trim(),
            [SettingKeys.PrivacyConsentRequired] = country is "IR" ? "false" : "true",
            [SettingKeys.SetupCompleted] = "true"
        });
        data.Changed();

        await signIn.SignInAsync(user, isPersistent: true);
        Response.Cookies.Append("bbn.panel-lang", user.PanelLanguage, new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax, Expires = DateTimeOffset.UtcNow.AddYears(1) });
        TempData["Toast"] = PanelText.Get("setup.done", user.PanelLanguage);
        return Redirect(P.BasePath);
    }
}
