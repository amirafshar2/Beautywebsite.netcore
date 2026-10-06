using System.Net.Mail;
using BeautyByNegin.Business.Content;
using BeautyByNegin.Business.Localization;
using BeautyByNegin.Business.Notifications;
using BeautyByNegin.Business.Settings;
using BeautyByNegin.DataAccess;
using BeautyByNegin.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using static BeautyByNegin.Business.Notifications.TelegramNotifier;

namespace BeautyByNegin.Business.Inbox;

public sealed record BookingInput(
    string? FullName, string? Phone, string? Email, int? ServiceId, DateOnly? PreferredDate, bool DateInvalid,
    int? TimeSlotId, string? Message, bool PrivacyAccepted);

public sealed record ContactInput(string? Name, string? ContactInfo, string? Message, bool PrivacyAccepted);

public sealed record ReviewInput(string? Name, bool InitialsOnly, int? Rating, string? Text, int? ServiceId, bool PrivacyAccepted);

/// <summary>Validation result: field name -> text key of the error message (translated by the web layer).</summary>
public sealed record SubmitResult(bool Ok, IReadOnlyDictionary<string, string> Errors, int? Id = null)
{
    public static SubmitResult Success(int id) => new(true, new Dictionary<string, string>(), id);
    public static SubmitResult Invalid(Dictionary<string, string> errors) => new(false, errors);
}

public interface IInboxService
{
    Task<SubmitResult> SubmitBookingAsync(BookingInput input, SiteLanguage lang, string panelBaseUrl, CancellationToken ct = default);
    Task<SubmitResult> SubmitContactAsync(ContactInput input, SiteLanguage lang, string panelBaseUrl, CancellationToken ct = default);
    Task<SubmitResult> SubmitReviewAsync(ReviewInput input, SiteLanguage lang, string panelBaseUrl, CancellationToken ct = default);
    Task<SubmitResult> SubscribeAsync(string? email, bool privacyAccepted, SiteLanguage lang, CancellationToken ct = default);
}

/// <summary>
/// Saves visitor submissions to the database FIRST (so nothing is lost when e-mail/Telegram are
/// unreachable), then queues notifications that are delivered in the background.
/// </summary>
public sealed class InboxService(
    AppDbContext db,
    ISettingsService settingsService,
    IContentService content,
    INotificationQueue notifications) : IInboxService
{
    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 200) return false;
        try { var a = new MailAddress(email.Trim()); return a.Address == email.Trim() && a.Host.Contains('.'); }
        catch { return false; }
    }

    public static bool IsValidPhone(string? phone)
    {
        var digits = ContactLinks.NormalizePhone(phone).TrimStart('+');
        return digits.Length is >= 7 and <= 16;
    }

    private static string Clean(string? s, int max) => (s ?? "").Trim() is var t && t.Length > max ? t[..max] : (s ?? "").Trim();

    public async Task<SubmitResult> SubmitBookingAsync(BookingInput input, SiteLanguage lang, string panelBaseUrl, CancellationToken ct = default)
    {
        var settings = await settingsService.GetAsync(ct);
        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(input.FullName)) errors["FullName"] = "form.error.required";
        if (string.IsNullOrWhiteSpace(input.Phone)) errors["Phone"] = "form.error.required";
        else if (!IsValidPhone(input.Phone)) errors["Phone"] = "form.error.phone";
        if (!string.IsNullOrWhiteSpace(input.Email) && !IsValidEmail(input.Email)) errors["Email"] = "form.error.email";

        var today = DateOnly.FromDateTime(DateDisplay.ToLocal(DateTime.UtcNow, settings.TimeZone));
        if (input.DateInvalid || input.PreferredDate is DateOnly d && (d < today || d > today.AddYears(1)))
            errors["PreferredDate"] = "form.error.date";
        if (settings.PrivacyConsentRequired && !input.PrivacyAccepted) errors["PrivacyAccepted"] = "form.error.privacy";
        if (errors.Count > 0) return SubmitResult.Invalid(errors);

        var services = await content.GetServicesAsync(lang, ct);
        var service = input.ServiceId is int sid and > 0 ? services.FirstOrDefault(s => s.Id == sid) : null;
        var slots = await content.GetTimeSlotsAsync(lang, ct);
        var slot = input.TimeSlotId is int tid ? slots.FirstOrDefault(s => s.Id == tid) : null;

        var request = new AppointmentRequest
        {
            FullName = Clean(input.FullName, 150),
            Phone = Clean(Digits.ToLatin(input.Phone), 40),
            Email = string.IsNullOrWhiteSpace(input.Email) ? null : Clean(input.Email, 200),
            ServiceId = service?.Id,
            ServiceNameSnapshot = service?.Name,
            WantsConsultation = input.ServiceId == 0,
            PreferredDate = input.PreferredDate,
            TimeSlotId = slot?.Id,
            TimeSlotSnapshot = slot?.Label,
            Message = string.IsNullOrWhiteSpace(input.Message) ? null : Clean(input.Message, 3000),
            LanguageCode = lang.Code,
            PrivacyAccepted = input.PrivacyAccepted
        };
        db.AppointmentRequests.Add(request);
        await db.SaveChangesAsync(ct);

        var faLang = new SiteLanguage("fa", "fa-IR", "فارسی", "FA", true, true, true, 0, true);
        var serviceText = request.WantsConsultation ? "مشاوره (مطمئن نیست)" : request.ServiceNameSnapshot ?? "—";
        var dateText = request.PreferredDate is DateOnly pd ? DateDisplay.FormatDate(pd.ToDateTime(TimeOnly.MinValue), faLang) : "—";
        var link = $"{panelBaseUrl}/appointments/details/{request.Id}";

        notifications.Enqueue(new Notification(NotificationKinds.Appointment,
            $"📅 <b>درخواست نوبت جدید</b>\n\n" +
            $"👤 {Html(request.FullName)}\n📞 {Html(request.Phone)}\n" +
            (request.Email is null ? "" : $"✉️ {Html(request.Email)}\n") +
            $"💆‍♀️ {Html(serviceText)}\n🗓 {Html(dateText)}" + (request.TimeSlotSnapshot is null ? "" : $" — {Html(request.TimeSlotSnapshot)}") + "\n" +
            (request.Message is null ? "" : $"\n💬 {Html(request.Message)}\n") +
            $"\n🌐 زبان: {request.LanguageCode.ToUpperInvariant()}\n<a href=\"{Html(link)}\">مشاهده در پنل</a>",
            $"درخواست نوبت جدید: {request.FullName}",
            $"نام: {request.FullName}\nتلفن: {request.Phone}\nایمیل: {request.Email ?? "—"}\nخدمت: {serviceText}\nتاریخ: {dateText} {request.TimeSlotSnapshot}\n\n{request.Message}\n\n{link}",
            request.Email));

        return SubmitResult.Success(request.Id);
    }

    public async Task<SubmitResult> SubmitContactAsync(ContactInput input, SiteLanguage lang, string panelBaseUrl, CancellationToken ct = default)
    {
        var settings = await settingsService.GetAsync(ct);
        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(input.Name)) errors["Name"] = "form.error.required";
        if (string.IsNullOrWhiteSpace(input.ContactInfo)) errors["ContactInfo"] = "form.error.required";
        else if (input.ContactInfo.Contains('@') ? !IsValidEmail(input.ContactInfo) : !IsValidPhone(input.ContactInfo))
            errors["ContactInfo"] = input.ContactInfo.Contains('@') ? "form.error.email" : "form.error.phone";
        if (string.IsNullOrWhiteSpace(input.Message)) errors["Message"] = "form.error.required";
        if (settings.PrivacyConsentRequired && !input.PrivacyAccepted) errors["PrivacyAccepted"] = "form.error.privacy";
        if (errors.Count > 0) return SubmitResult.Invalid(errors);

        var msg = new ContactMessage
        {
            Name = Clean(input.Name, 150),
            ContactInfo = Clean(Digits.ToLatin(input.ContactInfo), 200),
            Message = Clean(input.Message, 3000),
            LanguageCode = lang.Code,
            PrivacyAccepted = input.PrivacyAccepted
        };
        db.ContactMessages.Add(msg);
        await db.SaveChangesAsync(ct);

        var link = $"{panelBaseUrl}/messages/details/{msg.Id}";
        notifications.Enqueue(new Notification(NotificationKinds.Message,
            $"✉️ <b>پیام جدید از فرم تماس</b>\n\n👤 {Html(msg.Name)}\n📞 {Html(msg.ContactInfo)}\n\n💬 {Html(msg.Message)}\n\n<a href=\"{Html(link)}\">مشاهده در پنل</a>",
            $"پیام جدید: {msg.Name}",
            $"نام: {msg.Name}\nراه تماس: {msg.ContactInfo}\n\n{msg.Message}\n\n{link}",
            IsValidEmail(msg.ContactInfo) ? msg.ContactInfo : null));
        return SubmitResult.Success(msg.Id);
    }

    public async Task<SubmitResult> SubmitReviewAsync(ReviewInput input, SiteLanguage lang, string panelBaseUrl, CancellationToken ct = default)
    {
        var settings = await settingsService.GetAsync(ct);
        var errors = new Dictionary<string, string>();
        if (!settings.AllowVisitorReviews) errors[""] = "form.error.generic";
        if (string.IsNullOrWhiteSpace(input.Name)) errors["Name"] = "form.error.required";
        if (string.IsNullOrWhiteSpace(input.Text)) errors["Text"] = "form.error.required";
        if (settings.PrivacyConsentRequired && !input.PrivacyAccepted) errors["PrivacyAccepted"] = "form.error.privacy";
        if (errors.Count > 0) return SubmitResult.Invalid(errors);

        var services = await content.GetServicesAsync(lang, ct);
        var review = new Review
        {
            AuthorName = Clean(input.Name, 100),
            ShowInitialsOnly = input.InitialsOnly,
            Rating = input.Rating is int r and >= 1 and <= 5 ? r : null,
            Text = Clean(input.Text, 3000),
            LanguageCode = lang.Code,
            ReviewDate = DateOnly.FromDateTime(DateTime.UtcNow),
            ServiceId = services.Any(s => s.Id == input.ServiceId) ? input.ServiceId : null,
            Source = ReviewSource.Website,
            Status = ReviewStatus.Pending,
            IsVisible = true,
            SortOrder = 0
        };
        db.Reviews.Add(review);
        await db.SaveChangesAsync(ct);

        notifications.Enqueue(new Notification(NotificationKinds.Review,
            $"⭐ <b>نظر جدید (منتظر تأیید)</b>\n\n👤 {Html(review.AuthorName)}" + (review.Rating is int st ? $" — {new string('★', st)}" : "") +
            $"\n\n{Html(review.Text)}\n\n<a href=\"{Html(panelBaseUrl)}/reviews?tab=pending\">تأیید در پنل</a>",
            "نظر جدید", review.Text));
        return SubmitResult.Success(review.Id);
    }

    public async Task<SubmitResult> SubscribeAsync(string? email, bool privacyAccepted, SiteLanguage lang, CancellationToken ct = default)
    {
        var settings = await settingsService.GetAsync(ct);
        var errors = new Dictionary<string, string>();
        if (!settings.NewsletterEnabled) errors[""] = "form.error.generic";
        if (string.IsNullOrWhiteSpace(email)) errors["email"] = "form.error.required";
        else if (!IsValidEmail(email)) errors["email"] = "form.error.email";
        if (settings.PrivacyConsentRequired && !privacyAccepted) errors["privacyAccepted"] = "form.error.privacy";
        if (errors.Count > 0) return SubmitResult.Invalid(errors);

        var normalized = email!.Trim().ToLowerInvariant();
        var existing = await db.NewsletterSubscribers.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Email == normalized, ct);
        if (existing is not null)
        {
            // Re-subscribing silently re-activates (no duplicate rows, no information leak).
            existing.IsActive = true; existing.DeletedAtUtc = null; existing.LanguageCode = lang.Code;
            await db.SaveChangesAsync(ct);
            return SubmitResult.Success(existing.Id);
        }

        var sub = new NewsletterSubscriber { Email = normalized, LanguageCode = lang.Code, PrivacyAccepted = privacyAccepted };
        db.NewsletterSubscribers.Add(sub);
        await db.SaveChangesAsync(ct);

        notifications.Enqueue(new Notification(NotificationKinds.Newsletter,
            $"📰 <b>عضو جدید خبرنامه</b>\n\n✉️ {Html(sub.Email)}\n🌐 {sub.LanguageCode.ToUpperInvariant()}",
            "عضو جدید خبرنامه", sub.Email));
        return SubmitResult.Success(sub.Id);
    }
}
