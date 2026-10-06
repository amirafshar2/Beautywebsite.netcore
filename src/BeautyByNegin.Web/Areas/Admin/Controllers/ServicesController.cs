using BeautyByNegin.Business.Admin;
using BeautyByNegin.Business.Media;
using BeautyByNegin.DataAccess.Entities;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Areas.Admin.Controllers;

public class ServicesController(IAdminCatalogService catalog, IAdminData data) : AdminController
{
    [HttpGet]
    public async Task<IActionResult> Index() => View(await catalog.GetServicesAsync(HttpContext.RequestAborted));

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["Entity"] = null;
        return View("Edit", new ServiceInput { IsVisible = true });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var s = await catalog.GetServiceAsync(id, HttpContext.RequestAborted);
        if (s is null) return NotFound();
        ViewData["Entity"] = s;
        var input = new ServiceInput
        {
            Id = s.Id, IsVisible = s.IsVisible, ShowOnHome = s.ShowOnHome, Price = s.Price, CoverImageId = s.CoverImageId,
            ImageIds = string.Join(',', s.Images.OrderBy(i => i.SortOrder).Select(i => i.MediaImageId)),
            Tr = s.Translations.ToDictionary(t => t.LanguageCode, t => new ServiceTrInput
            {
                Name = t.Name, Subtitle = t.Subtitle, ShortDescription = t.ShortDescription, Description = t.Description,
                SuitableFor = t.SuitableFor, ExpectedResult = t.ExpectedResult, Duration = t.Duration, Slug = t.Slug,
                MetaTitle = t.MetaTitle, MetaDescription = t.MetaDescription
            })
        };
        return View(input);
    }

    [HttpPost]
    public async Task<IActionResult> Save(ServiceInput input)
    {
        var result = await catalog.SaveServiceAsync(input, P.DefaultLanguage.Code, HttpContext.RequestAborted);
        if (!result.Ok)
        {
            ViewData["Errors"] = Errors(result.Errors);
            ViewData["Entity"] = input.Id == 0 ? null : await catalog.GetServiceAsync(input.Id, HttpContext.RequestAborted);
            return View("Edit", input);
        }
        Saved();
        return Back($"services/edit/{result.Id}");
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(int id) => Ok(await data.ToggleVisibilityAsync<Service>(id, HttpContext.RequestAborted));

    [HttpPost]
    public async Task<IActionResult> Home(int id) => Ok(await catalog.ToggleServiceHomeAsync(id, HttpContext.RequestAborted));

    [HttpPost]
    public async Task<IActionResult> Sort(string ids)
    {
        await data.ReorderAsync<Service>(ParseIds(ids), HttpContext.RequestAborted);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> Duplicate(int id)
    {
        var copy = await catalog.DuplicateServiceAsync(id, HttpContext.RequestAborted);
        if (copy is null) return NotFound();
        Saved("toast.duplicated");
        return Back($"services/edit/{copy}");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await data.MoveToTrashAsync<Service>(id, HttpContext.RequestAborted);
        Saved("toast.movedToTrash");
        return Back("services");
    }

    public static List<int> ParseIds(string? ids) => (ids ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(s => int.TryParse(s, out var i) ? i : 0).Where(i => i > 0).ToList();
}
