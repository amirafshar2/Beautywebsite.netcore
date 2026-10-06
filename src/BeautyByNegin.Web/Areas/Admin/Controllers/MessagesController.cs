using BeautyByNegin.Business.Admin;
using BeautyByNegin.DataAccess.Entities;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Areas.Admin.Controllers;

public class MessagesController(IAdminInboxService inbox, IAdminData data) : AdminController
{
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1) => View(await inbox.GetMessagesAsync(page, HttpContext.RequestAborted));

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var m = await inbox.GetMessageAsync(id, markRead: true, HttpContext.RequestAborted);
        return m is null ? NotFound() : View(m);
    }

    [HttpPost]
    public async Task<IActionResult> Details(int id, string? adminNotes)
    {
        if (!await inbox.UpdateMessageNotesAsync(id, adminNotes, HttpContext.RequestAborted)) return NotFound();
        Saved();
        return Back($"messages/details/{id}");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await data.MoveToTrashAsync<ContactMessage>(id, HttpContext.RequestAborted);
        Saved("toast.movedToTrash");
        return Back("messages");
    }
}
