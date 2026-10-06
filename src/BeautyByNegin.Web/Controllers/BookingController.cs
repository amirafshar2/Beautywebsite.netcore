using BeautyByNegin.Web.Infrastructure.Routing;
using BeautyByNegin.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Controllers;

public class BookingController : PublicController
{
    [HttpGet]
    public async Task<IActionResult> Index(int? service)
    {
        Page(SiteRoutes.Booking);
        Seo(Ctx.T["booking.title"], Ctx.T["booking.intro"]);
        var model = new BookingForm { ServiceId = service, FormStartedTicks = DateTime.UtcNow.Ticks };
        return View(await BuildPage(model));
    }

    private async Task<BookingPage> BuildPage(BookingForm form) => new(
        form,
        await Content.GetServicesAsync(Ctx.Lang, HttpContext.RequestAborted),
        await Content.GetTimeSlotsAsync(Ctx.Lang, HttpContext.RequestAborted));
}
