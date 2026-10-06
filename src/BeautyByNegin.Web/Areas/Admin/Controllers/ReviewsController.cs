using BeautyByNegin.Business.Admin;
using BeautyByNegin.DataAccess.Entities;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Areas.Admin.Controllers;

public class ReviewsController(IAdminCatalogService catalog, IAdminData data) : AdminController
{
    [HttpGet]
    public async Task<IActionResult> Index(string? tab)
    {
        var pending = tab == "pending";
        ViewData["Pending"] = pending;
        ViewData["PendingCount"] = (await catalog.GetReviewsAsync(ReviewStatus.Pending, HttpContext.RequestAborted)).Count;
        return View(await catalog.GetReviewsAsync(pending ? ReviewStatus.Pending : ReviewStatus.Approved, HttpContext.RequestAborted));
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewData["Services"] = await catalog.GetServicesAsync(HttpContext.RequestAborted);
        return View("Edit", new ReviewInputModel { LanguageCode = P.DefaultLanguage.Code, ReviewDate = DateOnly.FromDateTime(DateTime.UtcNow), Rating = 5 });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var r = await catalog.GetReviewAsync(id, HttpContext.RequestAborted);
        if (r is null) return NotFound();
        ViewData["Services"] = await catalog.GetServicesAsync(HttpContext.RequestAborted);
        ViewData["Pending"] = r.Status == ReviewStatus.Pending;
        return View(new ReviewInputModel
        {
            Id = r.Id, AuthorName = r.AuthorName, ShowInitialsOnly = r.ShowInitialsOnly, Rating = r.Rating, Text = r.Text,
            LanguageCode = r.LanguageCode, ReviewDate = r.ReviewDate, ServiceId = r.ServiceId, Source = r.Source, IsVisible = r.IsVisible
        });
    }

    [HttpPost]
    public async Task<IActionResult> Save(ReviewInputModel input)
    {
        var result = await catalog.SaveReviewAsync(input, HttpContext.RequestAborted);
        if (!result.Ok)
        {
            ViewData["Errors"] = Errors(result.Errors);
            ViewData["Services"] = await catalog.GetServicesAsync(HttpContext.RequestAborted);
            return View("Edit", input);
        }
        Saved();
        return Back("reviews");
    }

    [HttpPost]
    public async Task<IActionResult> Approve(int id)
    {
        await catalog.ApproveReviewAsync(id, HttpContext.RequestAborted);
        if (Request.Headers.XRequestedWith == "fetch") return Ok();
        Saved("rev.approved");
        return Back("reviews?tab=pending");
    }

    [HttpPost] public async Task<IActionResult> Toggle(int id) => Ok(await data.ToggleVisibilityAsync<Review>(id, HttpContext.RequestAborted));

    [HttpPost]
    public async Task<IActionResult> Sort(string ids)
    {
        await data.ReorderAsync<Review>(ServicesController.ParseIds(ids), HttpContext.RequestAborted);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await data.MoveToTrashAsync<Review>(id, HttpContext.RequestAborted);
        if (Request.Headers.XRequestedWith == "fetch") return Ok();
        Saved("toast.movedToTrash");
        return Back("reviews");
    }
}
