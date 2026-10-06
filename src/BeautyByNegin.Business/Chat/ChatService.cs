using System.Security.Cryptography;
using System.Text;
using BeautyByNegin.Business.Content;
using BeautyByNegin.Business.Customers;
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
    ICustomerAccountService accounts,
    INotificationQueue notifications) : IChatService
{
    private const int MaxMessagesPerHour = 40;

    private static ChatResult Map(AccountResult r) =>
        r.NeedName ? new ChatResult(false, "account.needName") : new ChatResult(r.Ok, r.ErrorKey, r.NewSessionToken);

    private Task<ChatVisitor?> FindAsync(string? token, CancellationToken ct) => accounts.FindLoggedInAsync(token, ct);

    public async Task<ChatState> GetStateAsync(string? sessionToken, int afterId, CancellationToken ct = default)
    {
        var current = await accounts.GetCurrentAsync(sessionToken, ct);
        if (current.Step == LoginStep.LoggedOut) return new ChatState(ChatStep.Register, null, null, [], 0);
        if (current.Step == LoginStep.Verify) return new ChatState(ChatStep.Verify, null, current.PendingEmail, [], 0);
        var v = current.Customer!;

        var messages = await db.ChatMessages.AsNoTracking()
            .Where(m => m.VisitorId == v.Id && m.Id > afterId)
            .OrderBy(m => m.Id).Take(200)
            .Select(m => new ChatMessageView(m.Id, m.FromAdmin, m.Text, m.CreatedAtUtc))
            .ToListAsync(ct);
        var unread = await db.ChatMessages.CountAsync(m => m.VisitorId == v.Id && m.FromAdmin && !m.ReadByVisitor, ct);
        return new ChatState(ChatStep.Conversation, v.Name, v.Email, messages, unread);
    }

    /// <summary>Chat "register" = start the customer login (name + e-mail, code by e-mail).</summary>
    public async Task<ChatResult> RegisterAsync(string? sessionToken, string? name, string? emailAddress, SiteLanguage lang, CancellationToken ct = default)
        => Map(await accounts.StartLoginAsync(sessionToken, emailAddress, string.IsNullOrWhiteSpace(name) ? null : name, lang, ct));

    public async Task<ChatResult> ResendCodeAsync(string? sessionToken, SiteLanguage lang, CancellationToken ct = default)
        => Map(await accounts.ResendCodeAsync(sessionToken, lang, ct));

    public async Task<ChatResult> VerifyAsync(string? sessionToken, string? code, CancellationToken ct = default)
        => Map(await accounts.VerifyAsync(sessionToken, code, ct));

    public async Task<ChatResult> SendAsync(string? sessionToken, string? text, string? pageUrl, string panelBaseUrl, CancellationToken ct = default)
    {
        var v = await FindAsync(sessionToken, ct);
        if (v is null) return new ChatResult(false, "chat.codeExpired");
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
