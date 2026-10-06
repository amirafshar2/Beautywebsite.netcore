using BeautyByNegin.Business.Admin;
using BeautyByNegin.Web.Infrastructure.Startup;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Areas.Admin.Controllers;

[Authorize(Policy = Policies.AdminOnly)]
public class LanguagesController(IAdminTextService texts) : AdminController
{
    [HttpGet]
    public async Task<IActionResult> Index() => View(await texts.GetLanguagesAsync(HttpContext.RequestAborted));

    [HttpPost]
    public async Task<IActionResult> Index(List<string> order, List<string>? enabled, List<string>? nativeDigits, string defaultCode)
    {
        var list = order.Select(c => (c, enabled?.Contains(c) == true, nativeDigits?.Contains(c) == true)).ToList();
        var error = await texts.SaveLanguagesAsync(list, defaultCode, HttpContext.RequestAborted);
        if (error is not null) Problem(error); else Saved();
        return Back("languages");
    }
}
