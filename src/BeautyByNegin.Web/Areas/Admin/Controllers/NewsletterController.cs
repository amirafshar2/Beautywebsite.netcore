using BeautyByNegin.Business.Admin;
using BeautyByNegin.DataAccess.Entities;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Areas.Admin.Controllers;

public class NewsletterController(IAdminInboxService inbox, IAdminData data) : AdminController
{
    [HttpGet]
    public async Task<IActionResult> Index() => View(await inbox.GetSubscribersAsync(Ct));

    [HttpGet]
    public async Task<IActionResult> Export()
        => File(await inbox.ExportSubscribersCsvAsync(Ct), "text/csv; charset=utf-8", $"newsletter-{DateTime.UtcNow:yyyy-MM-dd}.csv");

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
        => await data.MoveToTrashAsync<NewsletterSubscriber>(id, Ct) ? Ok() : Fail("err.notFound");
}
