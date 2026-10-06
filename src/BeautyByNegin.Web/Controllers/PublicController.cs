using BeautyByNegin.Business.Content;
using BeautyByNegin.Web.Infrastructure;
using BeautyByNegin.Web.Infrastructure.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BeautyByNegin.Web.Controllers;

/// <summary>
/// Base class of every public page: loads the <see cref="PageContext"/> and shows the "coming soon"
/// page while maintenance mode is on (logged-in panel users still see the real site).
/// </summary>
public abstract class PublicController : Controller
{
    protected PageContext Ctx { get; private set; } = null!;
    protected IContentService Content => HttpContext.RequestServices.GetRequiredService<IContentService>();

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        Ctx = await HttpContext.RequestServices.GetRequiredService<SiteContext>().GetAsync();

        if (Ctx.Settings.MaintenanceMode && User.Identity?.IsAuthenticated != true)
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            Response.Headers.RetryAfter = "3600";
            context.Result = View("~/Views/Shared/Maintenance.cshtml");
            return;
        }

        await next();
    }

    /// <summary>Marks the active menu item and sets the URL of this page in every language.</summary>
    protected void Page(string routeKey)
    {
        Ctx.ActiveNav = routeKey;
        Ctx.Alternates = SiteUrls.Alternates(routeKey, Ctx.Languages.Select(l => l.Code));
    }

    protected void Seo(string? title, string? description = null)
    {
        ViewData["Title"] = title;
        if (!string.IsNullOrWhiteSpace(description)) ViewData["Description"] = Shorten(description, 160);
    }

    protected static string Shorten(string text, int max)
    {
        var plain = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]+>", " ");
        plain = System.Net.WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Replace(plain, @"\s+", " ")).Trim();
        if (plain.Length <= max) return plain;
        var cut = plain[..max];
        var space = cut.LastIndexOf(' ');
        return (space > max / 2 ? cut[..space] : cut).TrimEnd(',', '.', '،') + "…";
    }
}
