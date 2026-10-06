using BeautyByNegin.Business.Notifications;
using BeautyByNegin.DataAccess.Entities;
using BeautyByNegin.Web.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BeautyByNegin.Web.Areas.Admin.Controllers;

public class AccountController(
    SignInManager<AppUser> signIn,
    UserManager<AppUser> users,
    IEmailSender email) : AdminController
{
    // ------------------------------------------------------------ login

    [AllowAnonymous, HttpGet]
    public async Task<IActionResult> Login(string? returnUrl)
    {
        if (!users.Users.Any()) return Redirect(P.Url("setup"));
        if (User.Identity?.IsAuthenticated == true) return Redirect(P.BasePath);
        ViewData["ReturnUrl"] = returnUrl;
        await Task.CompletedTask;
        return View();
    }

    [AllowAnonymous, HttpPost, EnableRateLimiting(SpamGuard.FormsPolicy)]
    public async Task<IActionResult> Login(string? userName, string? password, bool rememberMe, string? returnUrl)
    {
        var panel = P;
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["UserName"] = userName;

        var user = string.IsNullOrWhiteSpace(userName) ? null
            : await users.FindByNameAsync(userName.Trim()) ?? await users.FindByEmailAsync(userName.Trim());
        if (user is null || string.IsNullOrEmpty(password))
        {
            ViewData["Error"] = panel["login.wrong"];
            return View();
        }

        var result = await signIn.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            ViewData["Error"] = panel["login.locked"];
            return View();
        }
        if (!result.Succeeded)
        {
            ViewData["Error"] = panel["login.wrong"];
            return View();
        }

        Response.Cookies.Append("bbn.panel-lang", user.PanelLanguage, new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax, Expires = DateTimeOffset.UtcNow.AddYears(1) });
        return LocalRedirect(Url.IsLocalUrl(returnUrl) && returnUrl!.StartsWith(panel.BasePath) ? returnUrl : panel.BasePath);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await signIn.SignOutAsync();
        return Redirect(P.Url("login"));
    }

    // ------------------------------------------------------------ my account

    [HttpGet]
    public IActionResult Password() => View();

    [HttpPost]
    public async Task<IActionResult> Password(string? currentPassword, string? newPassword, string? confirmPassword)
    {
        var errors = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 8 || !newPassword.Any(char.IsDigit)) errors["newPassword"] = P["account.passwordRules"];
        else if (newPassword != confirmPassword) errors["confirmPassword"] = P["account.passwordMismatch"];
        if (errors.Count == 0)
        {
            var result = await users.ChangePasswordAsync(P.User, currentPassword ?? "", newPassword!);
            if (!result.Succeeded) errors["currentPassword"] = P["account.currentWrong"];
            else
            {
                P.User.MustChangePassword = false;
                await users.UpdateAsync(P.User);
                await signIn.RefreshSignInAsync(P.User);
                Saved("account.passwordChanged");
                return Back("");
            }
        }
        ViewData["Errors"] = errors;
        return View();
    }

    /// <summary>Panel language (FA / TR / DE / EN) for the logged-in user.</summary>
    [HttpPost]
    public async Task<IActionResult> Language(string lang, string? returnUrl)
    {
        if (PanelContext.PanelLanguages.Contains(lang))
        {
            P.User.PanelLanguage = lang;
            await users.UpdateAsync(P.User);
            Response.Cookies.Append("bbn.panel-lang", lang, new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax, Expires = DateTimeOffset.UtcNow.AddYears(1) });
        }
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : P.BasePath);
    }

    // ------------------------------------------------------------ forgotten password (needs e-mail settings)

    [AllowAnonymous, HttpGet]
    public IActionResult Forgot()
    {
        return View();
    }

    [AllowAnonymous, HttpPost, EnableRateLimiting(SpamGuard.FormsPolicy)]
    public async Task<IActionResult> Forgot(string? userName)
    {
        var panel = P;
        var user = string.IsNullOrWhiteSpace(userName) ? null
            : await users.FindByNameAsync(userName.Trim()) ?? await users.FindByEmailAsync(userName.Trim());
        if (user?.Email is not null && panel.Settings.SmtpConfigured)
        {
            var token = await users.GeneratePasswordResetTokenAsync(user);
            var link = $"{Request.Scheme}://{Request.Host}{panel.Url("account/reset")}?user={Uri.EscapeDataString(user.UserName!)}&token={Uri.EscapeDataString(token)}";
            await email.SendAsync(user.Email, panel["forgot.mailSubject"], panel.F("forgot.mailBody", user.DisplayName, link));
        }
        // Same answer whether the user exists or not.
        ViewData["Sent"] = true;
        return View();
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Reset(string? user, string? token)
    {
        ViewData["User"] = user; ViewData["Token"] = token;
        return View();
    }

    [AllowAnonymous, HttpPost, EnableRateLimiting(SpamGuard.FormsPolicy)]
    public async Task<IActionResult> Reset(string? user, string? token, string? newPassword)
    {
        var panel = P;
        ViewData["User"] = user; ViewData["Token"] = token;
        var u = user is null ? null : await users.FindByNameAsync(user);
        if (u is null || token is null || string.IsNullOrEmpty(newPassword) || newPassword.Length < 8 || !newPassword.Any(char.IsDigit))
        {
            ViewData["Error"] = panel["account.passwordRules"];
            return View();
        }
        var result = await users.ResetPasswordAsync(u, token, newPassword);
        if (!result.Succeeded)
        {
            ViewData["Error"] = panel["forgot.linkInvalid"];
            return View();
        }
        await users.SetLockoutEndDateAsync(u, null);
        TempData["Toast"] = panel["account.passwordChanged"];
        return Redirect(panel.Url("login"));
    }
}
