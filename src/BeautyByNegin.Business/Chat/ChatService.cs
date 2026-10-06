using System.Security.Cryptography;
using System.Text;
using BeautyByNegin.Business.Content;
using BeautyByNegin.Business.Inbox;
using BeautyByNegin.Business.Localization;
using BeautyByNegin.Business.Notifications;
using BeautyByNegin.Business.Settings;
using BeautyByNegin.DataAccess;
using BeautyByNegin.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using static BeautyByNegin.Business.Notifications.TelegramNotifier;

namespace BeautyByNegin.Business.Chat;

public enum ChatStep { Register, Verify, Conversation }

public sealed record ChatMessageView(int Id, bool FromAdmin, string Text, DateTime CreatedAtUtc);

public sealed record ChatState(ChatStep Step, string? Name, string? Email, IReadOnlyList<ChatMessageView> Messages, int UnreadForVisitor);

/// <summary>Result of a chat action: error = text key for the visitor, or null on success.</summary>
public sealed record ChatResult(bool Ok, string? ErrorKey = null, string? NewSessionToken = null);

public interface IChatService
{
    Task<ChatState> GetStateAsync(string? sessionToken, int afterId, CancellationToken ct = default);
    Task<ChatResult> RegisterAsync(string? sessionToken, string? name, string? email, SiteLanguage lang, CancellationToken ct = default);
    Task<ChatResult> ResendCodeAsync(string? sessionToken, SiteLanguage lang, CancellationToken ct = default);
    Task<ChatResult> VerifyAsync(string? sessionToken, string? code, CancellationToken ct = default);
    Task<ChatResult> SendAsync(string? sessionToken, string? text, string? pageUrl, string panelBaseUrl, CancellationToken ct = default);
    Task MarkReadByVisitorAsync(string? sessionToken, CancellationToken ct = default);

    /// <summary>Reply written by the salon in the admin panel.</summary>
    Task<bool> ReplyAsAdminAsync(int visitorId, string text, CancellationToken ct = default);
}

/// <summary>
/// Floating chat. A visitor registers with name + e-mail, confirms a 6-digit code sent by e-mail,
/// then writes messages that are stored for the panel and forwarded to Telegram.
/// Identity = random token in an HttpOnly cookie (only its SHA-256 hash is stored).
/// </summary>
public sealed class ChatService(
    AppDbContext db,
    ISettingsService settingsService,
    ITextService texts,
    IEmailSender email,
    INotificationQueue notifications,
    ILogger<ChatService> logger) : IChatService
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ResendDelay = TimeSpan.FromSeconds(60);
    private const int MaxCodeAttempts = 5;
    private const int MaxMessagesPerHour = 40;

    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static string NewToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static string NewCode() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("000000");

    private async Task<ChatVisitor?> FindAsync(string? token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 100) return null;
        var hash = Hash(token);
        return await db.ChatVisitors.FirstOrDefaultAsync(v => v.SessionTokenHash == hash, ct);
    }

    public async Task<ChatState> GetStateAsync(string? sessionToken, int afterId, CancellationToken ct = default)
    {
        var v = await FindAsync(sessionToken, ct);
        if (v is null) return new ChatState(ChatStep.Register, null, null, [], 0);
        if (!v.IsEmailVerified) return new ChatState(ChatStep.Verify, v.Name, v.Email, [], 0);

        var messages = await db.ChatMessages.AsNoTracking()
            .Where(m => m.VisitorId == v.Id && m.Id > afterId)
            .OrderBy(m => m.Id).Take(200)
            .Select(m => new ChatMessageView(m.Id, m.FromAdmin, m.Text, m.CreatedAtUtc))
            .ToListAsync(ct);
        var unread = await db.ChatMessages.CountAsync(m => m.VisitorId == v.Id && m.FromAdmin && !m.ReadByVisitor, ct);
        return new ChatState(ChatStep.Conversation, v.Name, v.Email, messages, unread);
    }

    public async Task<ChatResult> RegisterAsync(string? sessionToken, string? name, string? emailAddress, SiteLanguage lang, CancellationToken ct = default)
    {
        var settings = await settingsService.GetAsync(ct);
        if (!settings.ChatAvailable) return new ChatResult(false, "form.error.generic");
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(emailAddress)) return new ChatResult(false, "form.error.required");
        if (!InboxService.IsValidEmail(emailAddress)) return new ChatResult(false, "form.error.email");

        var normalized = emailAddress.Trim().ToLowerInvariant();
        var visitor = await FindAsync(sessionToken, ct);
        if (visitor is not null && visitor.Email != normalized)
            visitor = null; // e-mail changed -> new identity

        // Same e-mail registered before (e.g. new device): reuse the conversation after verification.
        visitor ??= await db.ChatVisitors.FirstOrDefaultAsync(v => v.Email == normalized, ct);
        if (visitor?.IsBlocked == true) return new ChatResult(false, "form.error.generic");

        var token = NewToken();
        if (visitor is null)
        {
            visitor = new ChatVisitor { Email = normalized };
            db.ChatVisitors.Add(visitor);
        }
        else if (visitor.LastCodeSentAtUtc is DateTime last && DateTime.UtcNow - last < ResendDelay)
        {
            return new ChatResult(false, "form.error.rateLimit");
        }

        visitor.Name = name.Trim().Length > 150 ? name.Trim()[..150] : name.Trim();
        visitor.LanguageCode = lang.Code;
        visitor.SessionTokenHash = Hash(token);
        visitor.IsEmailVerified = false; // every new device/session proves ownership of the e-mail again
        var sent = await SendCodeAsync(visitor, lang, ct);
        await db.SaveChangesAsync(ct);
        return sent ? new ChatResult(true, null, token) : new ChatResult(false, "form.error.generic");
    }

    public async Task<ChatResult> ResendCodeAsync(string? sessionToken, SiteLanguage lang, CancellationToken ct = default)
    {
        var v = await FindAsync(sessionToken, ct);
        if (v is null) return new ChatResult(false, "chat.codeExpired");
        if (v.LastCodeSentAtUtc is DateTime last && DateTime.UtcNow - last < ResendDelay)
            return new ChatResult(false, "form.error.rateLimit");
        var sent = await SendCodeAsync(v, lang, ct);
        await db.SaveChangesAsync(ct);
        return sent ? new ChatResult(true) : new ChatResult(false, "form.error.generic");
    }

    private async Task<bool> SendCodeAsync(ChatVisitor v, SiteLanguage lang, CancellationToken ct)
    {
        var code = NewCode();
        v.VerificationCodeHash = Hash(v.Email + ":" + code);
        v.VerificationCodeExpiresAtUtc = DateTime.UtcNow.Add(CodeLifetime);
        v.VerificationAttempts = 0;
        v.LastCodeSentAtUtc = DateTime.UtcNow;

        var t = await texts.GetAsync(lang.Code, ct);
        var result = await email.SendAsync(v.Email, t["chat.email.subject"],
            t.Format("chat.email.body", ("name", v.Name), ("code", code)), null, ct);
        if (!result.Ok) logger.LogWarning("Chat code e-mail to {Email} failed: {Error}", v.Email, result.Error);
        return result.Ok;
    }

    public async Task<ChatResult> VerifyAsync(string? sessionToken, string? code, CancellationToken ct = default)
    {
        var v = await FindAsync(sessionToken, ct);
        if (v is null || v.VerificationCodeHash is null) return new ChatResult(false, "chat.codeExpired");
        if (v.IsEmailVerified) return new ChatResult(true);
        if (v.VerificationCodeExpiresAtUtc < DateTime.UtcNow || v.VerificationAttempts >= MaxCodeAttempts)
            return new ChatResult(false, "chat.codeExpired");

        var clean = Digits.ToLatin(code ?? "").Trim();
        v.VerificationAttempts++;
        var ok = clean.Length == 6 && CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(Hash(v.Email + ":" + clean)), Encoding.ASCII.GetBytes(v.VerificationCodeHash));
        if (ok)
        {
            v.IsEmailVerified = true;
            v.EmailVerifiedAtUtc = DateTime.UtcNow;
            v.VerificationCodeHash = null;
        }
        await db.SaveChangesAsync(ct);
        return ok ? new ChatResult(true) : new ChatResult(false, v.VerificationAttempts >= MaxCodeAttempts ? "chat.codeExpired" : "chat.codeInvalid");
    }

    public async Task<ChatResult> SendAsync(string? sessionToken, string? text, string? pageUrl, string panelBaseUrl, CancellationToken ct = default)
    {
        var v = await FindAsync(sessionToken, ct);
        if (v is null || !v.IsEmailVerified) return new ChatResult(false, "chat.codeExpired");
        if (v.IsBlocked) return new ChatResult(false, "form.error.generic");
        var clean = (text ?? "").Trim();
        if (clean.Length == 0) return new ChatResult(false, "form.error.required");
        if (clean.Length > 2000) clean = clean[..2000];

        var hourAgo = DateTime.UtcNow.AddHours(-1);
        if (await db.ChatMessages.CountAsync(m => m.VisitorId == v.Id && !m.FromAdmin && m.CreatedAtUtc > hourAgo, ct) >= MaxMessagesPerHour)
            return new ChatResult(false, "form.error.rateLimit");

        var message = new ChatMessage
        {
            VisitorId = v.Id,
            Text = clean,
            PageUrl = pageUrl is { Length: <= 500 } ? pageUrl : null,
            ReadByVisitor = true
        };
        db.ChatMessages.Add(message);
        v.LastMessageAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var messageId = message.Id;
        var link = $"{panelBaseUrl}/chats/conversation/{v.Id}";
        notifications.Enqueue(new Notification(NotificationKinds.Chat,
            $"💬 <b>پیام جدید در چت سایت</b>\n\n👤 {Html(v.Name)}\n✉️ {Html(v.Email)}\n\n{Html(clean)}\n\n<a href=\"{Html(link)}\">پاسخ در پنل</a>",
            $"پیام چت از {v.Name}",
            $"{v.Name} <{v.Email}>:\n\n{clean}\n\n{link}",
            v.Email,
            async (sp, delivered) =>
            {
                if (!delivered) return;
                var scopedDb = sp.GetRequiredService<AppDbContext>();
                await scopedDb.ChatMessages.Where(m => m.Id == messageId)
                    .ExecuteUpdateAsync(s => s.SetProperty(m => m.TelegramDelivered, true));
            }));
        return new ChatResult(true);
    }

    public async Task MarkReadByVisitorAsync(string? sessionToken, CancellationToken ct = default)
    {
        var v = await FindAsync(sessionToken, ct);
        if (v is null) return;
        await db.ChatMessages.Where(m => m.VisitorId == v.Id && m.FromAdmin && !m.ReadByVisitor)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.ReadByVisitor, true), ct);
    }

    public async Task<bool> ReplyAsAdminAsync(int visitorId, string text, CancellationToken ct = default)
    {
        var v = await db.ChatVisitors.FirstOrDefaultAsync(x => x.Id == visitorId, ct);
        if (v is null || string.IsNullOrWhiteSpace(text)) return false;
        db.ChatMessages.Add(new ChatMessage { VisitorId = v.Id, FromAdmin = true, Text = text.Trim(), ReadByAdmin = true });
        v.LastMessageAtUtc = DateTime.UtcNow;
        await db.ChatMessages.Where(m => m.VisitorId == v.Id && !m.FromAdmin && !m.ReadByAdmin)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.ReadByAdmin, true), ct);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
