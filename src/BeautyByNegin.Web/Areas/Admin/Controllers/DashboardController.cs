using BeautyByNegin.Business.Admin;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Areas.Admin.Controllers;

public class DashboardController(IAdminInboxService inbox) : AdminController
{
    [HttpGet]
    public async Task<IActionResult> Index() => View(await inbox.GetDashboardAsync(HttpContext.RequestAborted));
}
