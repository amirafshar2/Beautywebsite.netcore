using BeautyByNegin.Business.Inbox;
using BeautyByNegin.Web.Infrastructure.Routing;
using BeautyByNegin.Web.Infrastructure.Security;
using BeautyByNegin.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BeautyByNegin.Web.Controllers;

public class ContactController(IInboxService inbox) : PublicController
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        Page(SiteRoutes.Contact);
        Seo(Ctx.T["contact.title"], Ctx.T["contact.intro"]);
        if (Request.Query["rl"] == "1") ViewData["Errors"] = new Dictionary<string, string> { [""] = Ctx.T["form.error.rateLimit"] };
        return View(await Content.GetContactAsync(Ctx.Lang, HttpContext.RequestAborted));
    }

    /// <summary>POST /{lang}/api/contact/send</summary>
    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(SpamGuard.FormsPolicy)]
    public async Task<IActionResult> Send(ContactForm form)
    {
        var done = SiteUrls.Page(SiteRoutes.Contact, Ctx.Code) + "#contact-form";
        if (SpamGuard.LooksLikeBot(form.Website, form.FormStartedTicks))
        {
            TempData["ContactSent"] = true;
            return Redirect(done);
        }

        var result = await inbox.SubmitContactAsync(new ContactInput(form.Name, form.ContactInfo, form.Message, form.PrivacyAccepted),
            Ctx.Lang, PanelBaseUrl, HttpContext.RequestAborted);
        if (result.Ok)
        {
            TempData["ContactSent"] = true;
            return Redirect(done);
        }

        Page(SiteRoutes.Contact);
        Seo(Ctx.T["contact.title"]);
        ViewData["Errors"] = Translate(result.Errors);
        ViewData["Form"] = form;
        return View("Index", await Content.GetContactAsync(Ctx.Lang, HttpContext.RequestAborted));
    }
}
