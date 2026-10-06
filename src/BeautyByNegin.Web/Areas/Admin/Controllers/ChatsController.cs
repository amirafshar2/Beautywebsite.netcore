using BeautyByNegin.Business.Admin;
using BeautyByNegin.Business.Chat;
using BeautyByNegin.DataAccess.Entities;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Areas.Admin.Controllers;

/// <summary>Conversations from the floating chat; replies appear in the visitor's chat window.</summary>
public class ChatsController(IAdminInboxService inbox, IChatService chat, IAdminData data) : AdminController
{
    [HttpGet]
    public async Task<IActionResult> Index() => View(await inbox.GetChatsAsync(HttpContext.RequestAborted));

    [HttpGet]
    public async Task<IActionResult> Conversation(int id)
    {
        var (visitor, messages) = await inbox.GetConversationAsync(id, HttpContext.RequestAborted);
        if (visitor is null) return NotFound();
        ViewData["Messages"] = messages;
        return View(visitor);
    }

    [HttpPost]
    public async Task<IActionResult> Reply(int id, string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) { Problem("err.textRequired"); return Back($"chats/conversation/{id}"); }
        if (!await chat.ReplyAsAdminAsync(id, text, HttpContext.RequestAborted)) return NotFound();
        Saved("chat.sent");
        return Back($"chats/conversation/{id}#bottom");
    }

    [HttpPost]
    public async Task<IActionResult> Block(int id, bool blocked)
    {
        await inbox.SetChatBlockedAsync(id, blocked, HttpContext.RequestAborted);
        Saved();
        return Back($"chats/conversation/{id}");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await data.MoveToTrashAsync<ChatVisitor>(id, HttpContext.RequestAborted);
        Saved("toast.movedToTrash");
        return Back("chats");
    }
}
