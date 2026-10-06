using BeautyByNegin.Business.Chat;
using BeautyByNegin.Business.Customers;
using BeautyByNegin.Web.Infrastructure.Customers;
using BeautyByNegin.Web.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BeautyByNegin.Web.Controllers;

/// <summary>
/// JSON API of the floating chat (bottom-left): /{lang}/api/chat/{action}.
/// The visitor's identity is a random token in an HttpOnly cookie; every POST needs the antiforgery header.
/// </summary>
public class ChatController(IChatService chat) : PublicController
{
    private string? Token => CustomerCookie.Read(Request);

    private void SetToken(string token) => CustomerCookie.Write(HttpContext, token);

    private IActionResult Disabled() => Json(new { ok = false, errorKey = "form.error.generic", message = Ctx.T["form.error.generic"] });

    private IActionResult Result(ChatResult r)
    {
        if (r.NewSessionToken is not null) SetToken(r.NewSessionToken);
        return Json(new { ok = r.Ok, message = r.ErrorKey is null ? null : Ctx.T[r.ErrorKey] });
    }

    [HttpGet, EnableRateLimiting(SpamGuard.ChatPolicy)]
    public async Task<IActionResult> State(int after = 0)
    {
        if (!Ctx.Settings.ChatAvailable) return Disabled();
        var s = await chat.GetStateAsync(Token, after, HttpContext.RequestAborted);
        return Json(new
        {
            ok = true,
            step = s.Step.ToString().ToLowerInvariant(),
            name = s.Name,
            email = s.Email,
            unread = s.UnreadForVisitor,
            messages = s.Messages.Select(m => new
            {
                id = m.Id,
                fromAdmin = m.FromAdmin,
                text = m.Text,
                time = DateDisplay.ToLocal(m.CreatedAtUtc, Ctx.Settings.TimeZone).ToString("HH:mm")
            })
        });
    }

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(SpamGuard.ChatAuthPolicy)]
    public async Task<IActionResult> Register(string? name, string? email, string? website)
    {
        if (!Ctx.Settings.ChatAvailable) return Disabled();
        if (!string.IsNullOrEmpty(website)) return Json(new { ok = true }); // honeypot
        return Result(await chat.RegisterAsync(Token, name, email, Ctx.Lang, HttpContext.RequestAborted));
    }

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(SpamGuard.ChatAuthPolicy)]
    public async Task<IActionResult> Resend()
        => Result(await chat.ResendCodeAsync(Token, Ctx.Lang, HttpContext.RequestAborted));

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(SpamGuard.ChatAuthPolicy)]
    public async Task<IActionResult> Verify(string? code)
        => Result(await chat.VerifyAsync(Token, code, HttpContext.RequestAborted));

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(SpamGuard.ChatPolicy)]
    public async Task<IActionResult> Send(string? text, string? page)
        => Result(await chat.SendAsync(Token, text, page, PanelBaseUrl, HttpContext.RequestAborted));

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(SpamGuard.ChatPolicy)]
    public async Task<IActionResult> Read()
    {
        await chat.MarkReadByVisitorAsync(Token, HttpContext.RequestAborted);
        return Json(new { ok = true });
    }

    /// <summary>"Change e-mail" while waiting for the code: end this (not yet confirmed) login.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reset([FromServices] ICustomerAccountService accounts)
    {
        var current = await accounts.GetCurrentAsync(Token, HttpContext.RequestAborted);
        if (current.Step == LoginStep.Verify) await accounts.LogoutAsync(Token, HttpContext.RequestAborted);
        if (current.Step != LoginStep.LoggedIn) CustomerCookie.Delete(HttpContext);
        return Json(new { ok = true });
    }
}
