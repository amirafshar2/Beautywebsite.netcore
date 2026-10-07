using BeautyByNegin.Business.Admin;
using BeautyByNegin.Business.Customers;
using BeautyByNegin.DataAccess.Entities;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Areas.Admin.Controllers;

/// <summary>Customer accounts (people who registered on the website with their e-mail).</summary>
public class CustomersController(IAdminInboxService inbox, ICustomerAccountService accounts, IAdminData data) : AdminController
{
    [HttpGet]
    public async Task<IActionResult> Index(string? q)
    {
        ViewData["Q"] = q;
        return View(await inbox.GetCustomersAsync(q, Ct));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var c = await inbox.GetCustomerAsync(id, Ct);
        return c is null ? NotFound() : View(c);
    }

    [HttpPost]
    public async Task<IActionResult> Notes(int id, string? notes)
    {
        await inbox.UpdateCustomerNotesAsync(id, notes, Ct);
        Saved();
        return Back($"customers/details/{id}");
    }

    /// <summary>Blocking logs the customer out on every device; they cannot log in or chat until unblocked.</summary>
    [HttpPost]
    public async Task<IActionResult> Block(int id, bool blocked)
    {
        await inbox.SetChatBlockedAsync(id, blocked, Ct);
        if (blocked) await accounts.EndAllSessionsAsync(id, Ct);
        Saved(blocked ? "cust.blockedToast" : "cust.unblockedToast");
        return Back($"customers/details/{id}");
    }

    [HttpPost]
    public async Task<IActionResult> Logout(int id)
    {
        await accounts.EndAllSessionsAsync(id, Ct);
        Saved("cust.loggedOutToast");
        return Back($"customers/details/{id}");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await accounts.EndAllSessionsAsync(id, Ct);
        await data.MoveToTrashAsync<ChatVisitor>(id, Ct);
        Saved("toast.movedToTrash");
        return Back("customers");
    }
}
