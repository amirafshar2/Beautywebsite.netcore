using BeautyByNegin.Business.Admin;
using BeautyByNegin.DataAccess.Entities;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Areas.Admin.Controllers;

public class PagesController(IAdminCatalogService catalog, IAdminData data) : AdminController
{
    [HttpGet]
    public async Task<IActionResult> Index() => View(await catalog.GetPagesAsync(HttpContext.RequestAborted));

    [HttpGet]
    public IActionResult Create() => View("Edit", new PageInput());

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var p = await catalog.GetPageAsync(id, HttpContext.RequestAborted);
        if (p is null) return NotFound();
        ViewData["SystemKey"] = p.SystemKey;
        return View(new PageInput
        {
            Id = p.Id, IsVisible = p.IsVisible, ShowInFooter = p.ShowInFooter, ShowInMenu = p.ShowInMenu,
            Tr = p.Translations.ToDictionary(t => t.LanguageCode, t => new PageTrInput
            {
                Title = t.Title, Slug = t.Slug, Content = t.Content, MetaTitle = t.MetaTitle, MetaDescription = t.MetaDescription
            })
        });
    }

    [HttpPost]
    public async Task<IActionResult> Save(PageInput input)
    {
        var result = await catalog.SavePageAsync(input, P.DefaultLanguage.Code, HttpContext.RequestAborted);
        if (!result.Ok)
        {
            ViewData["Errors"] = Errors(result.Errors);
            if (input.Id != 0) ViewData["SystemKey"] = (await catalog.GetPageAsync(input.Id))?.SystemKey;
            return View("Edit", input);
        }
        Saved();
        return Back($"pages/edit/{result.Id}");
    }

    [HttpPost] public async Task<IActionResult> Toggle(int id) => Ok(await data.ToggleVisibilityAsync<Page>(id, HttpContext.RequestAborted));

    [HttpPost]
    public async Task<IActionResult> Sort(string ids)
    {
        await data.ReorderAsync<Page>(ServicesController.ParseIds(ids), HttpContext.RequestAborted);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var page = await catalog.GetPageAsync(id, HttpContext.RequestAborted);
        if (page?.SystemKey is not null) { Problem("page.systemNoDelete"); return Back($"pages/edit/{id}"); }
        await data.MoveToTrashAsync<Page>(id, HttpContext.RequestAborted);
        Saved("toast.movedToTrash");
        return Back("pages");
    }
}
