using BeautyByNegin.Business.Inbox;
using BeautyByNegin.Web.Infrastructure.Routing;
using BeautyByNegin.Web.Infrastructure.Security;
using BeautyByNegin.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BeautyByNegin.Web.Controllers;

public class ReviewsController(IInboxService inbox) : PublicController
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        Page(SiteRoutes.Reviews);
        Seo(Ctx.T["reviews.title"]);
        ViewBag.Services = await Content.GetServicesAsync(Ctx.Lang, HttpContext.RequestAborted);
        if (Request.Query["rl"] == "1") TempData["ReviewError"] = Ctx.T["form.error.rateLimit"];
        var reviews = await Content.GetReviewsAsync(Ctx.Lang, null, HttpContext.RequestAborted);
        if (reviews.Count > 0) ViewData["JsonLd"] = Infrastructure.Seo.StructuredData.Reviews(Infrastructure.Seo.StructuredData.BaseUrl(Ctx, Request), reviews);
        return View(reviews);
    }

    /// <summary>POST /{lang}/api/reviews/submit (only when visitor reviews are switched on in the panel)</summary>
    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(SpamGuard.FormsPolicy)]
    public async Task<IActionResult> Submit(ReviewForm form)
    {
        var back = SiteUrls.Page(SiteRoutes.Reviews, Ctx.Code) + "#write-review";
        if (!SpamGuard.LooksLikeBot(form.Website, form.FormStartedTicks))
        {
            var result = await inbox.SubmitReviewAsync(new ReviewInput(form.Name, form.InitialsOnly, form.Rating, form.Text, form.ServiceId, form.PrivacyAccepted, Ctx.Customer.Customer?.Id),
                Ctx.Lang, PanelBaseUrl, HttpContext.RequestAborted);
            if (!result.Ok)
            {
                TempData["ReviewError"] = string.Join(" ", Translate(result.Errors).Values.Distinct());
                return Redirect(back);
            }
        }
        TempData["ReviewSent"] = true;
        return Redirect(back);
    }
}
