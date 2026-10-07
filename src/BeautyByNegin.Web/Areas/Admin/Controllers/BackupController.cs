using BeautyByNegin.Business.Admin;
using BeautyByNegin.Web.Infrastructure.Startup;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Areas.Admin.Controllers;

/// <summary>One-click backup (database + photos) and restore — also the way to move the site to another server.</summary>
[Authorize(Policy = Policies.AdminOnly)]
public class BackupController(IBackupService backups) : AdminController
{
    [HttpGet]
    public IActionResult Index() => View(backups.List());

    /// <summary>Creates a fresh backup and downloads it.</summary>
    [HttpPost]
    public async Task<IActionResult> Download()
    {
        var path = await backups.CreateAsync(automatic: false, Ct);
        return PhysicalFile(path, "application/zip", Path.GetFileName(path));
    }

    [HttpGet]
    public IActionResult File(string name)
    {
        var path = backups.GetPath(name);
        return path is null ? NotFound() : PhysicalFile(path, "application/zip", name);
    }

    [HttpPost]
    public IActionResult Delete(string name)
    {
        if (backups.Delete(name)) Saved("toast.deleted");
        return Back("backup");
    }

    [HttpPost, RequestSizeLimit(2L * 1024 * 1024 * 1024), RequestFormLimits(MultipartBodyLengthLimit = 2L * 1024 * 1024 * 1024)]
    public async Task<IActionResult> Restore(IFormFile? file, bool confirm)
    {
        if (file is null || !confirm) { Problem("backup.chooseFile"); return Back("backup"); }
        await using var stream = file.OpenReadStream();
        var result = await backups.RestoreAsync(stream, Ct);
        if (!result.Ok) { Problem(result.ErrorKey!); return Back("backup"); }
        await HttpContext.SignOutAsync(Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme);
        TempData["Toast"] = P["backup.restored"];
        return Redirect(P.Url("login"));
    }
}
