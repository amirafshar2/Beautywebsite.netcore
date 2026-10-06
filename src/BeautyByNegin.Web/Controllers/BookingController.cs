using BeautyByNegin.Business.Inbox;
using BeautyByNegin.Web.Infrastructure.Routing;
using BeautyByNegin.Web.Infrastructure.Security;
using BeautyByNegin.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BeautyByNegin.Web.Controllers;

public class BookingController(IInboxService inbox) : PublicController
{
    [HttpGet]
    public async Task<IActionResult> Index(int? service)
    {
        Page(SiteRoutes.Booking);
        Seo(Ctx.T["booking.title"], Ctx.T["booking.intro"]);

        if (TempData["BookingDone"] is true)
            return View("Thanks");

        var model = new BookingForm { ServiceId = service, FormStartedTicks = DateTime.UtcNow.Ticks };
        var page = await BuildPage(model);
        if (Request.Query["rl"] == "1") page = page with { Errors = new() { [""] = Ctx.T["form.error.rateLimit"] } };
        return View(page);
    }

    /// <summary>POST /{lang}/api/booking/submit</summary>
    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(SpamGuard.FormsPolicy)]
    public async Task<IActionResult> Submit(BookingForm form)
    {
        // Bots get the normal "thank you" page, nothing is saved.
        if (SpamGuard.LooksLikeBot(form.Website, form.FormStartedTicks))
        {
            TempData["BookingDone"] = true;
            return Redirect(SiteUrls.Booking(Ctx.Code));
        }

        var (date, dateInvalid) = ParseDate(form);
        var result = await inbox.SubmitBookingAsync(new BookingInput(
            form.FullName, form.Phone, form.Email, form.ServiceId, date, dateInvalid,
            form.TimeSlotId, form.Message, form.PrivacyAccepted), Ctx.Lang, PanelBaseUrl, HttpContext.RequestAborted);

        if (result.Ok)
        {
            TempData["BookingDone"] = true;
            return Redirect(SiteUrls.Booking(Ctx.Code));
        }

        Page(SiteRoutes.Booking);
        Seo(Ctx.T["booking.title"]);
        var page = await BuildPage(form);
        return View("Index", page with { Errors = Translate(result.Errors) });
    }

    /// <summary>Persian form sends Jalali day/month/year; other languages send yyyy-MM-dd.</summary>
    private static (DateOnly? Date, bool Invalid) ParseDate(BookingForm f)
    {
        if (f.JalaliDay is int d && f.JalaliMonth is int m && f.JalaliYear is int y)
        {
            try
            {
                if (m > 6 && d > 30 || m == 12 && d > 30) return (null, true);
                return (DateDisplay.FromJalali(y, m, d), false);
            }
            catch (ArgumentOutOfRangeException) { return (null, true); }
        }
        if (f.JalaliDay is not null || f.JalaliMonth is not null) return (null, true); // incomplete Jalali date
        if (string.IsNullOrWhiteSpace(f.PreferredDate)) return (null, false);
        return DateOnly.TryParseExact(Digits.ToLatin(f.PreferredDate), "yyyy-MM-dd", out var g) ? (g, false) : (null, true);
    }

    private async Task<BookingPage> BuildPage(BookingForm form) => new(
        form,
        await Content.GetServicesAsync(Ctx.Lang, HttpContext.RequestAborted),
        await Content.GetTimeSlotsAsync(Ctx.Lang, HttpContext.RequestAborted));
}
