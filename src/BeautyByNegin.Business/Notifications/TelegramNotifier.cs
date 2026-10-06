using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BeautyByNegin.Business.Settings;
using BeautyByNegin.DataAccess;

namespace BeautyByNegin.Business.Notifications;

public interface ITelegramNotifier
{
    /// <summary>Sends an HTML-formatted message to the configured chat. Returns the result (used by the "Test" button).</summary>
    Task<SendResult> SendAsync(string html, CancellationToken ct = default);

    /// <summary>Same as <see cref="SendAsync"/> but with explicit token/chat (testing values before saving).</summary>
    Task<SendResult> SendWithAsync(string token, string chatId, string html, CancellationToken ct = default);

    /// <summary>Finds chat ids that recently wrote to the bot (helps the admin find her Chat ID).</summary>
    Task<(SendResult Result, IReadOnlyList<(string ChatId, string Name)> Chats)> DiscoverChatsAsync(string token, CancellationToken ct = default);
}

/// <summary>
/// Telegram Bot API client. api.telegram.org is blocked from Iranian servers, so this is always an
/// optional extra: failures are logged, never shown to visitors, and data is saved before notifying.
/// </summary>
public sealed class TelegramNotifier(IHttpClientFactory httpFactory, ISettingsService settings, ILogger<TelegramNotifier> logger) : ITelegramNotifier
{
    public const string HttpClientName = "telegram";

    public async Task<SendResult> SendAsync(string html, CancellationToken ct = default)
    {
        var s = await settings.GetAsync(ct);
        if (!s.Bool(SettingKeys.TelegramEnabled)) return SendResult.Fail("telegram-disabled");
        var token = await settings.GetSecretAsync(SettingKeys.TelegramBotToken, ct);
        var chatId = s.Text(SettingKeys.TelegramChatId);
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(chatId)) return SendResult.Fail("telegram-not-configured");
        return await SendWithAsync(token, chatId, html, ct);
    }

    public async Task<SendResult> SendWithAsync(string token, string chatId, string html, CancellationToken ct = default)
    {
        try
        {
            var http = httpFactory.CreateClient(HttpClientName);
            using var response = await http.PostAsJsonAsync($"bot{token.Trim()}/sendMessage", new
            {
                chat_id = chatId.Trim(),
                text = html.Length > 4000 ? html[..4000] + "…" : html,
                parse_mode = "HTML",
                disable_web_page_preview = true
            }, ct);
            if (response.IsSuccessStatusCode) return SendResult.Success;

            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("Telegram sendMessage failed: {Status} {Body}", (int)response.StatusCode, body);
            return SendResult.Fail(response.StatusCode switch
            {
                HttpStatusCode.Unauthorized or HttpStatusCode.NotFound => "telegram-bad-token",
                HttpStatusCode.BadRequest => "telegram-bad-chat",
                HttpStatusCode.Forbidden => "telegram-blocked-by-user",
                _ => "telegram-error"
            });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Telegram is unreachable from this server");
            return SendResult.Fail("telegram-unreachable");
        }
    }

    public async Task<(SendResult Result, IReadOnlyList<(string ChatId, string Name)> Chats)> DiscoverChatsAsync(string token, CancellationToken ct = default)
    {
        try
        {
            var http = httpFactory.CreateClient(HttpClientName);
            using var doc = JsonDocument.Parse(await http.GetStringAsync($"bot{token.Trim()}/getUpdates", ct));
            var chats = new List<(string, string)>();
            foreach (var update in doc.RootElement.GetProperty("result").EnumerateArray())
            {
                if (!update.TryGetProperty("message", out var msg) || !msg.TryGetProperty("chat", out var chat)) continue;
                var id = chat.GetProperty("id").GetRawText();
                var name = string.Join(" ", new[] { "first_name", "last_name", "title", "username" }
                    .Select(p => chat.TryGetProperty(p, out var v) ? v.GetString() : null).OfType<string>());
                if (chats.All(c => c.Item1 != id)) chats.Add((id, name));
            }
            return (SendResult.Success, chats);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.NotFound)
        {
            return (SendResult.Fail("telegram-bad-token"), []);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Telegram getUpdates failed");
            return (SendResult.Fail("telegram-unreachable"), []);
        }
    }

    /// <summary>Escapes text for Telegram's HTML parse mode.</summary>
    public static string Html(string? text) => WebUtility.HtmlEncode(text ?? "");
}
