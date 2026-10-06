using BeautyByNegin.Business.Admin;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Areas.Admin.Controllers;

/// <summary>"Site texts": every button, menu item, form label and message in all languages.</summary>
public class TextsController(IAdminTextService texts) : AdminController
{
    [HttpGet]
    public async Task<IActionResult> Index(string? group, string? q)
    {
        ViewData["Groups"] = await texts.GetGroupsAsync(P.ContentLanguages.Where(l => l.IsEnabled).Select(l => l.Code), HttpContext.RequestAborted);
        ViewData["Group"] = group;
        ViewData["Q"] = q;
        if (string.IsNullOrWhiteSpace(group) && string.IsNullOrWhiteSpace(q)) return View(null);
        return View(await texts.GetTextsAsync(group, q, HttpContext.RequestAborted));
    }

    [HttpPost]
    public async Task<IActionResult> Index(string? group, string? q, bool _ = false)
    {
        await texts.SaveTextsAsync(await TextForm.ReadAsync(Request), HttpContext.RequestAborted);
        Saved();
        return Back($"texts?group={Uri.EscapeDataString(group ?? "")}&q={Uri.EscapeDataString(q ?? "")}");
    }
}
