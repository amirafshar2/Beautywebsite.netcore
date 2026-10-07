using BeautyByNegin.Business.Admin;
using BeautyByNegin.DataAccess.Entities;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Areas.Admin.Controllers;

public class MessagesController(IAdminInboxService inbox, IAdminData data) : AdminController
{
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1) => View(await inbox.GetMessagesAsync(page, Ct));

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var m = await inbox.GetMessageAsync(id, markRead: true, Ct);
        return m is null ? NotFound() : View(m);
    }

    [HttpPost]
    public async Task<IActionResult> Details(int id, string? adminNotes)
    {
        if (!await inbox.UpdateMessageNotesAsync(id, adminNotes, Ct)) return NotFound();
        Saved();
        return Back($"messages/details/{id}");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await data.MoveToTrashAsync<ContactMessage>(id, Ct);
        Saved("toast.movedToTrash");
        return Back("messages");
    }
}
