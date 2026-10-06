using BeautyByNegin.Business.Admin;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Areas.Admin.Controllers;

public class TrashController(ITrashService trash) : AdminController
{
    [HttpGet]
    public async Task<IActionResult> Index() => View(await trash.ListAsync(HttpContext.RequestAborted));

    [HttpPost]
    public async Task<IActionResult> Restore(string kind, int id)
        => await trash.RestoreAsync(kind, id, HttpContext.RequestAborted) ? Json(new { ok = true, message = P["trash.restored"] }) : Fail("err.notFound");

    [HttpPost]
    public async Task<IActionResult> Delete(string kind, int id)
        => await trash.DeleteForeverAsync(kind, id, HttpContext.RequestAborted) ? Json(new { ok = true, message = P["toast.deleted"] }) : Fail("err.notFound");
}
