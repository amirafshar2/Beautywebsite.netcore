using BeautyByNegin.Business.Content;
using BeautyByNegin.Business.Settings;
using BeautyByNegin.Web.Infrastructure.Startup;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Areas.Admin.Controllers;

/// <summary>Site colors: 8 groups (browns, creams, dark backgrounds, texts, buttons, gold), each with ~20 suggestions.</summary>
[Authorize(Policy = Policies.AdminOnly)]
public class ColorsController(ISettingsService settings) : AdminController
{
    [HttpGet]
    public IActionResult Index() => View();

    [HttpPost]
    public async Task<IActionResult> Index(IFormCollection form)
    {
        var values = new Dictionary<string, string?>();
        foreach (var g in Theme.Groups)
        {
            var v = form[Theme.SettingKey(g.Key)].ToString();
            values[Theme.SettingKey(g.Key)] = Theme.TryParse(v, out _) && !string.Equals(Theme.Normalize(v), g.Default, StringComparison.OrdinalIgnoreCase)
                ? Theme.Normalize(v) : null; // default = nothing stored
        }
        await settings.SaveAsync(values);
        Cache.InvalidateAll();
        Saved("col.saved");
        return Back("colors");
    }

    [HttpPost]
    public async Task<IActionResult> Reset()
    {
        await settings.SaveAsync(Theme.Groups.ToDictionary(g => Theme.SettingKey(g.Key), _ => (string?)null));
        Cache.InvalidateAll();
        Saved("col.resetDone");
        return Back("colors");
    }
}
