using BeautyByNegin.DataAccess.Entities;
using BeautyByNegin.Web.Infrastructure.Startup;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Areas.Admin.Controllers;

/// <summary>Panel users: Admin (everything) and Editor (content only, no settings/backup/users).</summary>
[Authorize(Policy = Policies.AdminOnly)]
public class UsersController(UserManager<AppUser> users) : AdminController
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var list = new List<(AppUser User, bool IsAdmin)>();
        foreach (var u in users.Users.OrderBy(u => u.CreatedAtUtc).ToList())
            list.Add((u, await users.IsInRoleAsync(u, AppRoles.Admin)));
        return View(list);
    }

    [HttpPost]
    public async Task<IActionResult> Create(string? userName, string? displayName, string? email, string? password, string role)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrEmpty(password) || password.Length < 8 || !password.Any(char.IsDigit))
        {
            Problem("account.passwordRules");
            return Back("users");
        }
        var user = new AppUser
        {
            UserName = userName.Trim(), DisplayName = string.IsNullOrWhiteSpace(displayName) ? userName.Trim() : displayName.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(), PanelLanguage = P.Lang, MustChangePassword = true
        };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded) { Problem("users.createFailed"); return Back("users"); }
        await users.AddToRoleAsync(user, role == AppRoles.Admin ? AppRoles.Admin : AppRoles.Editor);
        Saved("users.created");
        return Back("users");
    }

    /// <summary>Sets a temporary password (the user must change it at next login) and unlocks the account.</summary>
    [HttpPost]
    public async Task<IActionResult> ResetPassword(string id, string? password)
    {
        var user = await users.FindByIdAsync(id);
        if (user is null) return NotFound();
        if (string.IsNullOrEmpty(password) || password.Length < 8 || !password.Any(char.IsDigit)) { Problem("account.passwordRules"); return Back("users"); }
        var token = await users.GeneratePasswordResetTokenAsync(user);
        await users.ResetPasswordAsync(user, token, password);
        user.MustChangePassword = user.Id != P.User.Id;
        await users.UpdateAsync(user);
        await users.SetLockoutEndDateAsync(user, null);
        Saved("users.passwordReset");
        return Back("users");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        if (id == P.User.Id) { Problem("users.cannotDeleteSelf"); return Back("users"); }
        var user = await users.FindByIdAsync(id);
        if (user is not null)
        {
            var admins = await users.GetUsersInRoleAsync(AppRoles.Admin);
            if (admins.Count <= 1 && admins.Any(a => a.Id == id)) { Problem("users.lastAdmin"); return Back("users"); }
            await users.DeleteAsync(user);
            Saved("toast.deleted");
        }
        return Back("users");
    }
}
