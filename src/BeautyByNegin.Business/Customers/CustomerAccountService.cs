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

namespace BeautyByNegin.Business.Customers;

public enum LoginStep { LoggedOut, Verify, LoggedIn }

public sealed record CustomerInfo(int Id, string Name, string Email, string? Phone, string LanguageCode, DateTime CreatedAtUtc);

/// <summary>Who is visiting: nobody, someone waiting for the e-mail code, or a logged-in customer.</summary>
public sealed record CurrentCustomer(LoginStep Step, CustomerInfo? Customer, string? PendingEmail)
{
    public static readonly CurrentCustomer Anonymous = new(LoginStep.LoggedOut, null, null);
    public bool IsLoggedIn => Step == LoginStep.LoggedIn && Customer is not null;
}

/// <summary>Result of an account action. ErrorKey = site text key for the visitor. NeedName = new e-mail, ask for the name.</summary>
public sealed record AccountResult(bool Ok, string? ErrorKey = null, string? NewSessionToken = null, bool NeedName = false);

public sealed record CustomerBooking(int Id, string? ServiceName, bool WantsConsultation, DateOnly? PreferredDate, string? TimeSlot,
    AppointmentStatus Status, DateTime CreatedAtUtc);

/// <summary>
/// Customer accounts without passwords: the customer types the e-mail address, receives a 6-digit code
/// and is logged in on that device. What a logged-in customer may do:
/// chat with the salon, see their own booking requests and their status (if enabled), write a review
/// (if enabled), edit name/phone and delete the account. Customers never see anything of other customers
/// and have no access to the admin panel.
/// </summary>
public interface ICustomerAccountService
{
    Task<CurrentCustomer> GetCurrentAsync(string? token, CancellationToken ct = default);
    /// <summary>The logged-in (verified, not blocked) customer of this cookie token, tracked by EF for updates.</summary>
    Task<ChatVisitor?> FindLoggedInAsync(string? token, CancellationToken ct = default);
    Task<AccountResult> StartLoginAsync(string? token, string? email, string? name, SiteLanguage lang, CancellationToken ct = default);
    Task<AccountResult> ResendCodeAsync(string? token, SiteLanguage lang, CancellationToken ct = default);
    Task<AccountResult> VerifyAsync(string? token, string? code, CancellationToken ct = default);
    Task LogoutAsync(string? token, CancellationToken ct = default);
    Task<AccountResult> UpdateProfileAsync(string? token, string? name, string? phone, CancellationToken ct = default);
    Task<AccountResult> DeleteAccountAsync(string? token, CancellationToken ct = default);
    Task<IReadOnlyList<CustomerBooking>> GetBookingsAsync(int customerId, CancellationToken ct = default);
    /// <summary>Panel: log the customer out on all devices (e.g. after blocking).</summary>
    Task EndAllSessionsAsync(int customerId, CancellationToken ct = default);
}

public sealed class CustomerAccountService(
    AppDbContext db,
    ISettingsService settingsService,
    ITextService texts,
    IEmailSender email,
    ILogger<CustomerAccountService> logger) : ICustomerAccountService
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ResendDelay = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(180);
    private const int MaxCodeAttempts = 5;

    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static string NewToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static string NewCode() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("000000");

    private static CustomerInfo Info(ChatVisitor v) => new(v.Id, v.Name, v.Email, v.Phone, v.LanguageCode, v.CreatedAtUtc);

    private async Task<CustomerSession?> SessionAsync(string? token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 100) return null;
        var hash = Hash(token);
        var s = await db.CustomerSessions.Include(x => x.Visitor).FirstOrDefaultAsync(x => x.TokenHash == hash, ct);
        if (s?.Visitor is null || s.ExpiresAtUtc < DateTime.UtcNow) return null;
        return s;
    }

    public async Task<CurrentCustomer> GetCurrentAsync(string? token, CancellationToken ct = default)
    {
        var s = await SessionAsync(token, ct);
        if (s is null || s.Visitor!.IsBlocked) return CurrentCustomer.Anonymous;
        if (!s.IsVerified) return new CurrentCustomer(LoginStep.Verify, null, s.Visitor.Email);
        if (DateTime.UtcNow - s.LastSeenAtUtc > TimeSpan.FromHours(1))
        {
            s.LastSeenAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        return new CurrentCustomer(LoginStep.LoggedIn, Info(s.Visitor), s.Visitor.Email);
    }

    public async Task<ChatVisitor?> FindLoggedInAsync(string? token, CancellationToken ct = default)
    {
        var s = await SessionAsync(token, ct);
        return s is { IsVerified: true } && !s.Visitor!.IsBlocked ? s.Visitor : null;
    }

    public async Task<AccountResult> StartLoginAsync(string? token, string? emailAddress, string? name, SiteLanguage lang, CancellationToken ct = default)
    {
        var settings = await settingsService.GetAsync(ct);
        if (!settings.AccountsAvailable) return new AccountResult(false, "form.error.generic");
        if (string.IsNullOrWhiteSpace(emailAddress)) return new AccountResult(false, "form.error.required");
        if (!InboxService.IsValidEmail(emailAddress)) return new AccountResult(false, "form.error.email");

        var normalized = emailAddress.Trim().ToLowerInvariant();
        var visitor = await db.ChatVisitors.FirstOrDefaultAsync(v => v.Email == normalized, ct);
        var cleanName = (name ?? "").Trim();
        if (visitor is null && cleanName.Length == 0) return new AccountResult(false, NeedName: true);
        if (visitor?.IsBlocked == true) return new AccountResult(false, "account.blocked");
        if (visitor?.LastCodeSentAtUtc is DateTime last && DateTime.UtcNow - last < ResendDelay)
            return new AccountResult(false, "account.waitMinute");

        if (visitor is null)
        {
            visitor = new ChatVisitor { Email = normalized, Name = cleanName.Length > 150 ? cleanName[..150] : cleanName, LanguageCode = lang.Code };
            db.ChatVisitors.Add(visitor);
        }

        // This browser starts a new (not yet confirmed) session; an older pending one is dropped.
        var old = await SessionAsync(token, ct);
        if (old is not null && !old.IsVerified) db.CustomerSessions.Remove(old);

        var newToken = NewToken();
        visitor.Sessions.Add(new CustomerSession { TokenHash = Hash(newToken), IsVerified = false, ExpiresAtUtc = DateTime.UtcNow.Add(SessionLifetime) });
        var sent = await SendCodeAsync(visitor, lang, ct);
        await db.SaveChangesAsync(ct);
        return sent ? new AccountResult(true, null, newToken) : new AccountResult(false, "form.error.generic");
    }

    public async Task<AccountResult> ResendCodeAsync(string? token, SiteLanguage lang, CancellationToken ct = default)
    {
        var s = await SessionAsync(token, ct);
        if (s is null || s.IsVerified) return new AccountResult(false, "chat.codeExpired");
        if (s.Visitor!.LastCodeSentAtUtc is DateTime last && DateTime.UtcNow - last < ResendDelay)
            return new AccountResult(false, "account.waitMinute");
        var sent = await SendCodeAsync(s.Visitor, lang, ct);
        await db.SaveChangesAsync(ct);
        return sent ? new AccountResult(true) : new AccountResult(false, "form.error.generic");
    }

    private async Task<bool> SendCodeAsync(ChatVisitor v, SiteLanguage lang, CancellationToken ct)
    {
        var code = NewCode();
        v.VerificationCodeHash = Hash(v.Email + ":" + code);
        v.VerificationCodeExpiresAtUtc = DateTime.UtcNow.Add(CodeLifetime);
        v.VerificationAttempts = 0;
        v.LastCodeSentAtUtc = DateTime.UtcNow;

        var t = await texts.GetAsync(lang.Code, ct);
        var result = await email.SendAsync(v.Email, t["account.email.subject"],
            t.Format("account.email.body", ("name", v.Name), ("code", code)), null, ct);
        if (!result.Ok) logger.LogWarning("Login code e-mail to {Email} failed: {Error}", v.Email, result.Error);
        return result.Ok;
    }

    public async Task<AccountResult> VerifyAsync(string? token, string? code, CancellationToken ct = default)
    {
        var s = await SessionAsync(token, ct);
        if (s is null) return new AccountResult(false, "chat.codeExpired");
        if (s.IsVerified) return new AccountResult(true);
        var v = s.Visitor!;
        if (v.VerificationCodeHash is null || v.VerificationCodeExpiresAtUtc < DateTime.UtcNow || v.VerificationAttempts >= MaxCodeAttempts)
            return new AccountResult(false, "chat.codeExpired");

        var clean = Digits.ToLatin(code ?? "").Trim();
        v.VerificationAttempts++;
        var ok = clean.Length == 6 && CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(Hash(v.Email + ":" + clean)), Encoding.ASCII.GetBytes(v.VerificationCodeHash));
        if (ok)
        {
            s.IsVerified = true;
            s.LastSeenAtUtc = DateTime.UtcNow;
            v.IsEmailVerified = true;
            v.EmailVerifiedAtUtc ??= DateTime.UtcNow;
            v.LastLoginAtUtc = DateTime.UtcNow;
            v.VerificationCodeHash = null;
            // tidy up: expired and abandoned sessions of this customer
            var stale = await db.CustomerSessions.Where(x => x.VisitorId == v.Id && x.Id != s.Id &&
                (x.ExpiresAtUtc < DateTime.UtcNow || (!x.IsVerified && x.CreatedAtUtc < DateTime.UtcNow.AddDays(-1)))).ToListAsync(ct);
            db.CustomerSessions.RemoveRange(stale);
        }
        await db.SaveChangesAsync(ct);
        return ok ? new AccountResult(true) : new AccountResult(false, v.VerificationAttempts >= MaxCodeAttempts ? "chat.codeExpired" : "chat.codeInvalid");
    }

    public async Task LogoutAsync(string? token, CancellationToken ct = default)
    {
        var s = await SessionAsync(token, ct);
        if (s is null) return;
        db.CustomerSessions.Remove(s);
        await db.SaveChangesAsync(ct);
    }

    public async Task<AccountResult> UpdateProfileAsync(string? token, string? name, string? phone, CancellationToken ct = default)
    {
        var v = await FindLoggedInAsync(token, ct);
        if (v is null) return new AccountResult(false, "chat.codeExpired");
        var cleanName = (name ?? "").Trim();
        if (cleanName.Length == 0) return new AccountResult(false, "form.error.required");
        var cleanPhone = (phone ?? "").Trim();
        if (cleanPhone.Length > 0 && !InboxService.IsValidPhone(cleanPhone)) return new AccountResult(false, "form.error.phone");
        v.Name = cleanName.Length > 150 ? cleanName[..150] : cleanName;
        v.Phone = cleanPhone.Length == 0 ? null : cleanPhone;
        await db.SaveChangesAsync(ct);
        return new AccountResult(true);
    }

    /// <summary>"Delete my account": moves the customer (with the conversation) to the panel's Trash; removed for good after 30 days.</summary>
    public async Task<AccountResult> DeleteAccountAsync(string? token, CancellationToken ct = default)
    {
        var v = await FindLoggedInAsync(token, ct);
        if (v is null) return new AccountResult(false, "chat.codeExpired");
        await db.CustomerSessions.Where(x => x.VisitorId == v.Id).ExecuteDeleteAsync(ct);
        v.DeletedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new AccountResult(true);
    }

    public async Task<IReadOnlyList<CustomerBooking>> GetBookingsAsync(int customerId, CancellationToken ct = default)
    {
        var v = await db.ChatVisitors.AsNoTracking().FirstOrDefaultAsync(x => x.Id == customerId, ct);
        if (v is null) return [];
        var mail = v.Email;
        // Own requests: sent while logged in, or earlier with the same (now verified) e-mail address.
        return await db.AppointmentRequests.AsNoTracking()
            .Where(a => a.CustomerId == customerId || (a.Email != null && a.Email.ToLower() == mail))
            .OrderByDescending(a => a.CreatedAtUtc).Take(50)
            .Select(a => new CustomerBooking(a.Id, a.ServiceNameSnapshot, a.WantsConsultation, a.PreferredDate, a.TimeSlotSnapshot, a.Status, a.CreatedAtUtc))
            .ToListAsync(ct);
    }

    public async Task EndAllSessionsAsync(int customerId, CancellationToken ct = default)
        => await db.CustomerSessions.IgnoreQueryFilters().Where(x => x.VisitorId == customerId).ExecuteDeleteAsync(ct);
}
