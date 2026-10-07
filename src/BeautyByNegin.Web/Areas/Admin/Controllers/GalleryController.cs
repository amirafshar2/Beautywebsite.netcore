using BeautyByNegin.Business.Admin;
using BeautyByNegin.DataAccess.Entities;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Areas.Admin.Controllers;

public class GalleryController(IAdminCatalogService catalog, IAdminData data) : AdminController
{
    [HttpGet]
    public async Task<IActionResult> Index(int? category)
    {
        ViewData["Categories"] = await catalog.GetGalleryCategoriesAsync(Ct);
        ViewData["Category"] = category;
        return View(await catalog.GetGalleryItemsAsync(category, Ct));
    }

    /// <summary>Several photos uploaded at once are added to the gallery (optionally into a category).</summary>
    [HttpPost]
    public async Task<IActionResult> Add(string ids, int? categoryId)
    {
        var count = await catalog.AddGalleryItemsAsync(ServicesController.ParseIds(ids), categoryId, Ct);
        Saved();
        return Ok(count);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await catalog.GetGalleryItemAsync(id, Ct);
        if (item is null) return NotFound();
        ViewData["Categories"] = await catalog.GetGalleryCategoriesAsync(Ct);
        return View(item);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(GalleryItemInput input)
    {
        var result = await catalog.SaveGalleryItemAsync(input, Ct);
        if (!result.Ok) return NotFound();
        Saved();
        return Back($"gallery/edit/{input.Id}");
    }

    [HttpPost] public async Task<IActionResult> Toggle(int id) => Ok(await data.ToggleVisibilityAsync<GalleryItem>(id, Ct));
    [HttpPost] public async Task<IActionResult> Home(int id) => Ok(await catalog.ToggleGalleryHomeAsync(id, Ct));

    [HttpPost]
    public async Task<IActionResult> Sort(string ids)
    {
        await data.ReorderAsync<GalleryItem>(ServicesController.ParseIds(ids), Ct);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await data.MoveToTrashAsync<GalleryItem>(id, Ct);
        if (Request.Headers.XRequestedWith == "fetch") return Ok();
        Saved("toast.movedToTrash");
        return Back("gallery");
    }

    // ---------------- categories

    [HttpGet]
    public async Task<IActionResult> Categories() => View(await catalog.GetGalleryCategoriesAsync(Ct));

    [HttpPost]
    public async Task<IActionResult> SaveCategory(NamedInput input)
    {
        var result = await catalog.SaveGalleryCategoryAsync(input, P.DefaultLanguage.Code, Ct);
        if (result.Ok) Saved(); else Problem("err.nameRequired");
        return Back("gallery/categories");
    }

    [HttpPost] public async Task<IActionResult> ToggleCategory(int id) => Ok(await data.ToggleVisibilityAsync<GalleryCategory>(id, Ct));

    [HttpPost]
    public async Task<IActionResult> SortCategories(string ids)
    {
        await data.ReorderAsync<GalleryCategory>(ServicesController.ParseIds(ids), Ct);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        await data.MoveToTrashAsync<GalleryCategory>(id, Ct);
        Saved("toast.movedToTrash");
        return Back("gallery/categories");
    }
}
