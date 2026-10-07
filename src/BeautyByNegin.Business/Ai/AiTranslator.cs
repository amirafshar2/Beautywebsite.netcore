using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using BeautyByNegin.Business.Admin;
using BeautyByNegin.Business.Settings;
using BeautyByNegin.DataAccess;
using Microsoft.Extensions.Configuration;

namespace BeautyByNegin.Business.Ai;

/// <summary>One text to translate. Key = how the panel finds the matching field again.</summary>
public sealed record TranslateItem(string Key, string Text, bool Html);

/// <summary>Translations per language → per key. ErrorKey = panel text key for a plain-language error.</summary>
public sealed record TranslateResult(bool Ok, IReadOnlyDictionary<string, Dictionary<string, string>>? Translations = null, string? ErrorKey = null, string? Detail = null);

public interface IAiTranslator
{
    Task<TranslateResult> TranslateAsync(string source, IReadOnlyList<string> targets, IReadOnlyList<TranslateItem> items, CancellationToken ct = default);
}

/// <summary>
/// Automatic translation in the admin panel with Google Gemini (key from Google AI Studio, entered under Settings).
/// The admin writes Persian once and presses "Translate"; the results are put into the other language fields
/// (not saved automatically – she checks them and presses Save).
/// Note: Google's API is not reachable from servers inside Iran.
/// </summary>
public sealed class GeminiTranslator(IHttpClientFactory http, ISettingsService settings, IConfiguration config, ILogger<GeminiTranslator> logger) : IAiTranslator
{
    public const string HttpClientName = "gemini";
    public const string DefaultModel = "gemini-3.5-flash";
    private const int MaxCharsPerCall = 12000;

    private static readonly Dictionary<string, string> LanguageNames = new()
    {
        ["fa"] = "Persian (Farsi)", ["tr"] = "Turkish", ["de"] = "German", ["en"] = "English", ["ar"] = "Arabic (Modern Standard)",
    };

    private static string Name(string code) => LanguageNames.TryGetValue(code, out var n) ? n : code;

    public async Task<TranslateResult> TranslateAsync(string source, IReadOnlyList<string> targets, IReadOnlyList<TranslateItem> items, CancellationToken ct = default)
    {
        var key = await settings.GetSecretAsync(SettingKeys.AiGeminiKey, ct);
        if (string.IsNullOrWhiteSpace(key)) return new TranslateResult(false, ErrorKey: "ai.err.noKey");
        targets = targets.Where(t => t != source && LanguageNames.ContainsKey(t)).Distinct().ToList();
        var work = items.Where(i => !string.IsNullOrWhiteSpace(i.Text)).ToList();
        if (targets.Count == 0 || work.Count == 0) return new TranslateResult(false, ErrorKey: "ai.err.nothing");

        var model = (await settings.GetAsync(ct)).Text(SettingKeys.AiModel, DefaultModel);
        var all = targets.ToDictionary(t => t, _ => new Dictionary<string, string>());

        // Long pages (all site texts) are sent in a few parts.
        foreach (var chunk in Chunks(work))
        {
            var r = await CallAsync(key, model, source, targets, chunk, ct);
            if (!r.Ok) return r;
            foreach (var (lang, values) in r.Translations!)
                foreach (var (k, v) in values) all[lang][k] = v;
        }
        return new TranslateResult(true, all);
    }

    private static IEnumerable<List<TranslateItem>> Chunks(List<TranslateItem> items)
    {
        var chunk = new List<TranslateItem>(); var size = 0;
        foreach (var i in items)
        {
            if (chunk.Count > 0 && size + i.Text.Length > MaxCharsPerCall) { yield return chunk; chunk = []; size = 0; }
            chunk.Add(i); size += i.Text.Length;
        }
        if (chunk.Count > 0) yield return chunk;
    }

    private async Task<TranslateResult> CallAsync(string apiKey, string model, string source, IReadOnlyList<string> targets, List<TranslateItem> items, CancellationToken ct)
    {
        var input = new JsonObject();
        foreach (var i in items) input[i.Key] = new JsonObject { ["html"] = i.Html, ["text"] = i.Text };

        var prompt = new StringBuilder()
            .AppendLine("You translate website and admin-panel texts for \"Beauty by Negin\", a premium facial and skincare studio.")
            .AppendLine($"Translate every item from {Name(source)} into: {string.Join(", ", targets.Select(t => $"{t} = {Name(t)}"))}.")
            .AppendLine("Rules:")
            .AppendLine("- Natural, warm, elegant tone of a luxury beauty brand. Not word-for-word, but keep the meaning exactly.")
            .AppendLine("- Keep the brand name \"Beauty by Negin\" and treatment names (e.g. Carboxy Facial, Dermaplaning, Microcurrent) in English.")
            .AppendLine("- Keep placeholders in curly braces such as {name}, {code}, {service}, {email} exactly as they are.")
            .AppendLine("- If \"html\" is true, keep all HTML tags and their order exactly; translate only the visible text.")
            .AppendLine("- Keep line breaks. Do not add explanations, quotes or notes.")
            .AppendLine("- German: no healing promises (Heilmittelwerbegesetz); prefer wording like \"kann helfen, das Hautbild zu verbessern\". Use the polite \"Sie\".")
            .AppendLine("- Turkish: polite \"siz\". Arabic: Modern Standard Arabic. English: international English.")
            .AppendLine("Answer with JSON only, in this shape: {\"<language code>\": {\"<item key>\": \"<translated text>\"}} for every target language and every item key.")
            .AppendLine("Items:")
            .Append(input.ToJsonString(new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
            .ToString();

        var body = new JsonObject
        {
            ["contents"] = new JsonArray(new JsonObject { ["role"] = "user", ["parts"] = new JsonArray(new JsonObject { ["text"] = prompt }) }),
            ["generationConfig"] = new JsonObject { ["responseMimeType"] = "application/json", ["temperature"] = 0.2 },
        };

        var client = http.CreateClient(HttpClientName);
        var baseUrl = (config["Site:GeminiBaseUrl"] ?? "https://generativelanguage.googleapis.com").TrimEnd('/');
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1beta/models/{Uri.EscapeDataString(model)}:generateContent")
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("x-goog-api-key", apiKey);
        await request.Content.LoadIntoBufferAsync(ct); // send a Content-Length instead of chunked upload (friendlier to proxies)

        HttpResponseMessage response;
        try { response = await client.SendAsync(request, ct); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Gemini not reachable");
            return new TranslateResult(false, ErrorKey: "ai.err.network", Detail: ex.Message);
        }

        using (response)
        {
            var text = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Gemini error {Status}: {Body}", (int)response.StatusCode, text.Length > 500 ? text[..500] : text);
                var errorKey = response.StatusCode switch
                {
                    HttpStatusCode.TooManyRequests => "ai.err.quota",
                    HttpStatusCode.NotFound => "ai.err.model",
                    HttpStatusCode.Forbidden when text.Contains("location", StringComparison.OrdinalIgnoreCase) => "ai.err.region",
                    HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden when text.Contains("API_KEY", StringComparison.OrdinalIgnoreCase) || text.Contains("API key", StringComparison.OrdinalIgnoreCase) => "ai.err.key",
                    HttpStatusCode.BadRequest when text.Contains("location", StringComparison.OrdinalIgnoreCase) => "ai.err.region",
                    _ => "ai.err.generic",
                };
                return new TranslateResult(false, ErrorKey: errorKey, Detail: ((int)response.StatusCode).ToString());
            }

            try
            {
                var answer = JsonNode.Parse(text)?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>() ?? "";
                answer = answer.Trim();
                if (answer.StartsWith("```")) answer = answer.Trim('`').Replace("json", "", StringComparison.OrdinalIgnoreCase).Trim();
                var json = JsonNode.Parse(answer) as JsonObject ?? throw new JsonException("not an object");
                var result = new Dictionary<string, Dictionary<string, string>>();
                var html = items.ToDictionary(i => i.Key, i => i.Html);
                foreach (var lang in targets)
                {
                    var values = new Dictionary<string, string>();
                    if (json[lang] is JsonObject obj)
                        foreach (var (k, v) in obj)
                        {
                            if (!html.TryGetValue(k, out var isHtml) || v is null) continue;
                            var value = v.GetValueKind() == JsonValueKind.String ? v.GetValue<string>() : v.ToString();
                            values[k] = isHtml ? RichText.Clean(value) ?? "" : value.Trim();
                        }
                    result[lang] = values;
                }
                return new TranslateResult(true, result);
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException)
            {
                logger.LogWarning(ex, "Gemini answer could not be read");
                return new TranslateResult(false, ErrorKey: "ai.err.generic");
            }
        }
    }
}
