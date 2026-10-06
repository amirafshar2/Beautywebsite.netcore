using BeautyByNegin.Business.Admin;
using BeautyByNegin.DataAccess.Entities;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Areas.Admin.Controllers;

public class AppointmentsController(IAdminInboxService inbox, IAdminData data) : AdminController
{
    [HttpGet]
    public async Task<IActionResult> Index(AppointmentStatus? status, DateOnly? from, DateOnly? to, string? q, int page = 1)
    {
        var filter = new AppointmentFilter(status, from, to, q, page);
        ViewData["Filter"] = filter;
        return View(await inbox.GetAppointmentsAsync(filter, HttpContext.RequestAborted));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var a = await inbox.GetAppointmentAsync(id, HttpContext.RequestAborted);
        return a is null ? NotFound() : View(a);
    }

    [HttpPost]
    public async Task<IActionResult> Details(int id, AppointmentStatus status, string? adminNotes)
    {
        if (!await inbox.UpdateAppointmentAsync(id, status, adminNotes, HttpContext.RequestAborted)) return NotFound();
        Saved();
        return Back($"appointments/details/{id}");
    }

    /// <summary>One-tap status change from the list (fetch).</summary>
    [HttpPost]
    public async Task<IActionResult> Status(int id, AppointmentStatus status)
    {
        var a = await inbox.GetAppointmentAsync(id, HttpContext.RequestAborted);
        if (a is null) return Fail("err.notFound");
        await inbox.UpdateAppointmentAsync(id, status, a.AdminNotes, HttpContext.RequestAborted);
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> Export(AppointmentStatus? status, DateOnly? from, DateOnly? to, string? q)
    {
        var bytes = await inbox.ExportAppointmentsCsvAsync(new AppointmentFilter(status, from, to, q, 1, int.MaxValue), HttpContext.RequestAborted);
        return File(bytes, "text/csv; charset=utf-8", $"appointments-{DateTime.UtcNow:yyyy-MM-dd}.csv");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await data.MoveToTrashAsync<AppointmentRequest>(id, HttpContext.RequestAborted);
        Saved("toast.movedToTrash");
        return Back("appointments");
    }
}
