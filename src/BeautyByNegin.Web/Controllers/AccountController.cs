using BeautyByNegin.Business.Chat;
using BeautyByNegin.Business.Customers;
using BeautyByNegin.Web.Infrastructure.Customers;
using BeautyByNegin.Web.Infrastructure.Routing;
using BeautyByNegin.Web.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BeautyByNegin.Web.Controllers;

public sealed record AccountPage(
    CurrentCustomer Current,
    string? NeedNameEmail,
    string? Error,
    string? Notice,
    IReadOnlyList<CustomerBooking> Bookings,
    IReadOnlyList<ChatMessageView> Messages,
    string? ReturnUrl);

/// <summary>
/// Customer account ("My account", login button in the header): /fa/account, /de/konto …
/// Login without password: e-mail → 6-digit code by e-mail → logged in on this device.
/// Plain HTML forms (works without JavaScript); actions post to /{lang}/api/account/{action}.
/// </summary>
public class AccountController(ICustomerAccountService accounts, IChatService chat, CustomerContext customer) : PublicController
{
    private string? Token => CustomerCookie.Read(Request);
    private string AccountUrl => SiteUrls.Page(SiteRoutes.Account, Ctx.Code);

    private IActionResult BackToAccount(string? error = null, string? notice = null, string? returnUrl = null)
    {
        if (error is not null) TempData["AccountError"] = Ctx.T[error];
        if (notice is not null) TempData["AccountNotice"] = Ctx.T[notice];
        var url = AccountUrl;
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) url += "?returnUrl=" + Uri.EscapeDataString(returnUrl);
        return Redirect(url);
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? returnUrl)
    {
        Page(SiteRoutes.Account);
        Seo(Ctx.T["account.title"]);
        ViewData["NoIndex"] = true;
        if (!Ctx.Settings.AccountsAvailable) return View("Unavailable");

        var current = Ctx.Customer;
        if (current.IsLoggedIn && !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && TempData["JustLoggedIn"] is true)
            return LocalRedirect(returnUrl);

        IReadOnlyList<CustomerBooking> bookings = [];
        IReadOnlyList<ChatMessageView> messages = [];
        if (current.IsLoggedIn)
        {
            if (Ctx.Settings.AccountsShowBookings) bookings = await accounts.GetBookingsAsync(current.Customer!.Id, HttpContext.RequestAborted);
            if (Ctx.Settings.ChatAvailable)
            {
                messages = (await chat.GetStateAsync(Token, 0, HttpContext.RequestAborted)).Messages;
                await chat.MarkReadByVisitorAsync(Token, HttpContext.RequestAborted);
            }
        }

        return View(new AccountPage(current, TempData["NeedName"] as string, TempData["AccountError"] as string,
            TempData["AccountNotice"] as string, bookings, messages, Url.IsLocalUrl(returnUrl) ? returnUrl : null));
    }

    /// <summary>Step 1: e-mail (and name for new customers) → code by e-mail.</summary>
    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(SpamGuard.ChatAuthPolicy)]
    public async Task<IActionResult> Start(string? email, string? name, string? website, string? returnUrl)
    {
        if (!string.IsNullOrEmpty(website)) return BackToAccount(); // honeypot
        var r = await accounts.StartLoginAsync(Token, email, name, Ctx.Lang, HttpContext.RequestAborted);
        if (r.NeedName)
        {
            TempData["NeedName"] = email?.Trim();
            return BackToAccount(returnUrl: returnUrl);
        }
        if (!r.Ok)
        {
            if (!string.IsNullOrWhiteSpace(name)) TempData["NeedName"] = email?.Trim();
            return BackToAccount(r.ErrorKey, returnUrl: returnUrl);
        }
        CustomerCookie.Write(HttpContext, r.NewSessionToken!);
        return BackToAccount(notice: "chat.codeSent", returnUrl: returnUrl);
    }

    /// <summary>Step 2: the 6-digit code.</summary>
    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(SpamGuard.ChatAuthPolicy)]
    public async Task<IActionResult> Verify(string? code, string? returnUrl)
    {
        var r = await accounts.VerifyAsync(Token, code, HttpContext.RequestAborted);
        if (!r.Ok) return BackToAccount(r.ErrorKey, returnUrl: returnUrl);
        TempData["JustLoggedIn"] = true;
        return BackToAccount(notice: "account.welcomeBack", returnUrl: returnUrl);
    }

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(SpamGuard.ChatAuthPolicy)]
    public async Task<IActionResult> Resend(string? returnUrl)
    {
        var r = await accounts.ResendCodeAsync(Token, Ctx.Lang, HttpContext.RequestAborted);
        return BackToAccount(r.Ok ? null : r.ErrorKey, r.Ok ? "chat.codeSent" : null, returnUrl);
    }

    /// <summary>"Use another e-mail" while waiting for the code, and "Log out".</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await accounts.LogoutAsync(Token, HttpContext.RequestAborted);
        CustomerCookie.Delete(HttpContext);
        customer.Reset();
        return BackToAccount(notice: "account.loggedOut");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(string? name, string? phone)
    {
        var r = await accounts.UpdateProfileAsync(Token, name, phone, HttpContext.RequestAborted);
        return BackToAccount(r.Ok ? null : r.ErrorKey, r.Ok ? "account.saved" : null);
    }

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(SpamGuard.ChatPolicy)]
    public async Task<IActionResult> Message(string? text)
    {
        var r = await chat.SendAsync(Token, text, AccountUrl, PanelBaseUrl, HttpContext.RequestAborted);
        if (!r.Ok) return BackToAccount(r.ErrorKey);
        return Redirect(AccountUrl + "#messages");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(bool confirm)
    {
        if (!confirm) return BackToAccount("account.deleteConfirmMissing");
        var r = await accounts.DeleteAccountAsync(Token, HttpContext.RequestAborted);
        if (!r.Ok) return BackToAccount(r.ErrorKey);
        CustomerCookie.Delete(HttpContext);
        return BackToAccount(notice: "account.deleted");
    }
}
