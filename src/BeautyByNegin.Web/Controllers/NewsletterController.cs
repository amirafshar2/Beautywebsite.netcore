using BeautyByNegin.Business.Inbox;
using BeautyByNegin.Web.Infrastructure.Routing;
using BeautyByNegin.Web.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BeautyByNegin.Web.Controllers;

/// <summary>POST /{lang}/api/newsletter/subscribe — newsletter sign-up from the home page (also sent to Telegram).</summary>
public class NewsletterController(IInboxService inbox) : PublicController
{
    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(SpamGuard.FormsPolicy)]
    public async Task<IActionResult> Subscribe(string? email, bool privacyAccepted, string? website, long formStartedTicks)
    {
        string message; bool ok;
        if (SpamGuard.LooksLikeBot(website, formStartedTicks))
        {
            ok = true; message = Ctx.T["newsletter.success"];
        }
        else
        {
            var result = await inbox.SubscribeAsync(email, privacyAccepted, Ctx.Lang, HttpContext.RequestAborted);
            ok = result.Ok;
            message = ok ? Ctx.T["newsletter.success"] : string.Join(" ", Translate(result.Errors).Values.Distinct());
        }

        if (WantsJson) return Json(new { ok, message });
        TempData["NewsletterMessage"] = message;
        return Redirect(SiteUrls.Home(Ctx.Code) + "#newsletter");
    }
}
