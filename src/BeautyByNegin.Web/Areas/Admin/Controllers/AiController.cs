using System.Text.Json;
using BeautyByNegin.Business.Ai;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Areas.Admin.Controllers;

/// <summary>"Translate to all languages" button next to the language tabs (Gemini). Results are only filled in, not saved.</summary>
public class AiController(IAiTranslator translator) : AdminController
{
    public sealed record Payload(string? Source, List<string>? Targets, List<TranslateItem>? Items);

    [HttpPost]
    public async Task<IActionResult> Translate(string? payload)
    {
        Payload? p;
        try { p = string.IsNullOrWhiteSpace(payload) ? null : JsonSerializer.Deserialize<Payload>(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)); }
        catch (JsonException) { p = null; }
        if (p?.Items is null || p.Targets is null || string.IsNullOrWhiteSpace(p.Source)) return Fail("ai.err.nothing");
        if (p.Items.Count > 400 || p.Items.Sum(i => i.Text?.Length ?? 0) > 120_000) return Fail("ai.err.tooLong");

        var r = await translator.TranslateAsync(p.Source, p.Targets, p.Items.Where(i => i.Key is not null && i.Text is not null).ToList(), HttpContext.RequestAborted);
        return r.Ok
            ? Json(new { ok = true, message = P["ai.done"], translations = r.Translations })
            : Json(new { ok = false, message = P[r.ErrorKey ?? "ai.err.generic"] + (r.Detail is null ? "" : $" ({r.Detail})") });
    }

    /// <summary>Settings → "Test": translates one short sentence.</summary>
    [HttpPost]
    public async Task<IActionResult> Test()
    {
        var r = await translator.TranslateAsync("fa", ["en", "de"], [new TranslateItem("t", "پوست شما، زیبایی شما.", false)], HttpContext.RequestAborted);
        return r.Ok
            ? Json(new { ok = true, message = P.F("ai.testOk", r.Translations!["en"].GetValueOrDefault("t") ?? "", r.Translations!["de"].GetValueOrDefault("t") ?? "") })
            : Json(new { ok = false, message = P[r.ErrorKey ?? "ai.err.generic"] + (r.Detail is null ? "" : $" ({r.Detail})") });
    }
}
