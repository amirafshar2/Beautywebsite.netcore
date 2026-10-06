using BeautyByNegin.Business.Content;
using BeautyByNegin.Business.Settings;
using BeautyByNegin.DataAccess.Entities;
using BeautyByNegin.Web.Infrastructure.Startup;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BeautyByNegin.Web.Areas.Admin.Controllers;

/// <summary>
/// Base of every panel screen: login required (Admin or Editor), panel language, forced password change,
/// helpers for the green "Saved" message and for JSON answers of the small fetch() actions.
/// </summary>
[Area("Admin")]
[Authorize(Policy = Policies.Panel)]
[AutoValidateAntiforgeryToken]
public abstract class AdminController : Controller
{
    protected PanelContext P { get; private set; } = null!;

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var sp = HttpContext.RequestServices;
        var users = sp.GetRequiredService<UserManager<AppUser>>();
        var user = User.Identity?.IsAuthenticated == true ? await users.GetUserAsync(User) : null;
        if (user is null && User.Identity?.IsAuthenticated == true)
        {
            // Login cookie of a user that no longer exists (e.g. after restoring a backup): sign out cleanly.
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            HttpContext.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity());
        }
        var anonymousAllowed = context.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any();
        if (anonymousAllowed && user is null)
        {
            P = await BuildAsync(HttpContext, null);
            ViewData["Panel"] = P;
            await next();
            return;
        }
        if (user is null)
        {
            context.Result = Redirect(IdentitySetup.AdminPath(sp.GetRequiredService<IConfiguration>()) + "/login");
            return;
        }

        P = await BuildAsync(HttpContext, user);
        ViewData["Panel"] = P;

        // First login with a temporary password -> must choose a new one.
        if (user.MustChangePassword && context.ActionDescriptor.RouteValues["controller"] != "Account")
        {
            context.Result = Redirect(P.Url("account/password"));
            return;
        }
        await next();
    }

    public static async Task<PanelContext> BuildAsync(HttpContext http, AppUser? user, string? langOverride = null)
    {
        var sp = http.RequestServices;
        var languages = sp.GetRequiredService<ILanguageService>();
        var all = await languages.GetAllAsync();
        var lang = langOverride ?? user?.PanelLanguage ?? http.Request.Cookies["bbn.panel-lang"] ?? "fa";
        if (!PanelContext.PanelLanguages.Contains(lang)) lang = "fa";
        return new PanelContext
        {
            Lang = lang,
            User = user ?? new AppUser(),
            IsAdmin = user is not null && http.User.IsInRole(AppRoles.Admin),
            Settings = await sp.GetRequiredService<ISettingsService>().GetAsync(),
            ContentLanguages = all.OrderByDescending(l => l.IsEnabled).ThenBy(l => l.SortOrder).ToList(),
            DefaultLanguage = await languages.GetDefaultAsync(),
            BasePath = IdentitySetup.AdminPath(sp.GetRequiredService<IConfiguration>())
        };
    }

    /// <summary>Shows the green confirmation at the top of the next page.</summary>
    protected void Saved(string key = "toast.saved") => TempData["Toast"] = P[key];
    protected void Problem(string key) => TempData["ToastError"] = P[key];

    protected IActionResult Ok(object? extra = null) => Json(new { ok = true, message = P["toast.saved"], data = extra });
    protected IActionResult Fail(string key) => Json(new { ok = false, message = P[key] });

    protected IActionResult Back(string path) => Redirect(P.Url(path));

    /// <summary>Panel error keys -> panel texts, for showing under the fields.</summary>
    protected Dictionary<string, string> Errors(Dictionary<string, string> keys) => keys.ToDictionary(k => k.Key, k => P[k.Value]);

    protected ISiteCache Cache => HttpContext.RequestServices.GetRequiredService<ISiteCache>();
}
