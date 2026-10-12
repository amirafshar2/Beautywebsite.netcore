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
    int? TimeSlotId, string? Message, bool PrivacyAccepted, int? CustomerId = null);

public sealed record ContactInput(string? Name, string? ContactInfo, string? Message, bool PrivacyAccepted);

public sealed record ReviewInput(string? Name, bool InitialsOnly, int? Rating, string? Text, int? ServiceId, bool PrivacyAccepted, int? CustomerId = null);

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
    Task<SubmitResult> SubscribeAsync(string? email, bool privacyAccepted, SiteLanguage lang, CancellationToken ct = default, string panelBaseUrl = "");
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

    /// <summary>Spam check (see <see cref="SpamFilter"/>); the site's own host and contact e-mail domains never count as fake.</summary>
    private static string? SpamReason(SiteSettings settings, string panelBaseUrl, params string?[] fields)
    {
        var own = new List<string>();
        foreach (var url in new[] { settings.SiteUrl, panelBaseUrl })
            if (Uri.TryCreate(url, UriKind.Absolute, out var u)) own.Add(u.Host);
        foreach (var key in new[] { SettingKeys.Email }.Concat(Enumerable.Empty<string>()))
        {
            foreach (var v in new[] { settings.Text(key) }.Concat(new[] { "fa", "tr", "de", "en", "ar" }.Select(l => settings.Text(SettingKeys.ForLanguage(key, l)))))
                if (v.Contains('@')) own.Add(v[(v.IndexOf('@') + 1)..]);
        }
        return SpamFilter.Check(settings.BrandName, own, fields);
    }

    private static string SpamNote(string reason) => $"{SpamFilter.NoteMarker} ({reason})";

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
            PrivacyAccepted = input.PrivacyAccepted,
            CustomerId = input.CustomerId
        };
        // Spam: kept in the trash with the reason (restorable), no notification. The sender sees the normal "thank you".
        var spam = input.CustomerId is null ? SpamReason(settings, panelBaseUrl, request.FullName, request.Email, request.Message) : null;
        if (spam is not null) { request.DeletedAtUtc = DateTime.UtcNow; request.AdminNotes = SpamNote(spam); }
        db.AppointmentRequests.Add(request);
        await db.SaveChangesAsync(ct);
        if (spam is not null) return SubmitResult.Success(request.Id);

        var faLang = TelegramText.Fa;
        // Service name in Persian for the owner (the client may have booked on the Turkish or German page).
        var faServices = request.ServiceId is int fsid ? await content.GetServicesAsync(faLang, ct) : [];
        var serviceFa = request.WantsConsultation ? "هنوز مطمئن نیست؛ مشاوره می‌خواهد"
            : faServices.FirstOrDefault(x => x.Id == request.ServiceId)?.Name ?? request.ServiceNameSnapshot;
        var serviceText = request.WantsConsultation ? "مشاوره (مطمئن نیست)" : request.ServiceNameSnapshot ?? "—";
        var dateText = request.PreferredDate is DateOnly pd ? DateDisplay.FormatDate(pd.ToDateTime(TimeOnly.MinValue), faLang) : "—";
        var link = $"{panelBaseUrl}/appointments/details/{request.Id}";

        // Time slot label as written in the Persian panel (the client saw it in their own language).
        var slotFa = request.TimeSlotId is int fslot
            ? (await content.GetTimeSlotsAsync(faLang, ct)).FirstOrDefault(x => x.Id == fslot)?.Label ?? request.TimeSlotSnapshot
            : null;
        var when = request.PreferredDate is DateOnly wd
            ? TelegramText.Date(wd) + (slotFa is null ? "" : $" — {slotFa}")
            : slotFa;
        var tg = new TelegramText("📅", "درخواست نوبت جدید",
                "یک نفر در سایت فرم «رزرو نوبت» را پر کرده است. این نوبت هنوز قطعی نیست؛ سایت فقط درخواست را گرفته و شما باید با مشتری هماهنگ کنید.")
            .Line("👤", "نام", request.FullName)
            .Line("📞", "تلفن", request.Phone)
            .Line("✉️", "ایمیل", request.Email, "وارد نکرده")
            .Line("💆‍♀️", "خدمت", serviceFa, "انتخاب نکرده")
            .Line("🗓", "زمان دلخواه", when, "زمانی انتخاب نکرده")
            .Line("🌐", "زبان صفحه‌ی مشتری", TelegramText.LanguageLine(request.LanguageCode))
            .Line("🙋‍♀️", "حساب کاربری", request.CustomerId is null ? null : "مشتری عضو سایت است و درخواست را در صفحه‌ی «حساب من» می‌بیند")
            .Line("🕒", "زمان ارسال", TelegramText.Now(settings.TimeZone))
            .Block("💬", "توضیحات مشتری", request.Message)
            .Next("با مشتری تماس بگیرید یا در واتس‌اپ پیام بدهید و زمان را هماهنگ کنید. بعد در پنل وضعیت را «تماس گرفته شد» یا «تأیید شد» کنید تا یادتان بماند.",
                ("باز کردن درخواست در پنل", link), ("پیام در واتس‌اپ به مشتری", TelegramText.WhatsApp(request.Phone)));

        notifications.Enqueue(new Notification(NotificationKinds.Appointment,
            tg.ToString(),
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
        var spam = SpamReason(settings, panelBaseUrl, msg.Name, msg.ContactInfo, msg.Message);
        if (spam is not null) { msg.DeletedAtUtc = DateTime.UtcNow; msg.AdminNotes = SpamNote(spam); }
        db.ContactMessages.Add(msg);
        await db.SaveChangesAsync(ct);
        if (spam is not null) return SubmitResult.Success(msg.Id);

        var link = $"{panelBaseUrl}/messages/details/{msg.Id}";
        var isMail = TelegramText.IsEmail(msg.ContactInfo);
        var tg = new TelegramText("✉️", "پیام جدید از فرم «تماس با ما»",
                "یک نفر از طریق سایت برای شما پیام فرستاده است (فرم تماس یا دکمه‌ی «درباره‌ی این خدمت بپرسید»).")
            .Line("👤", "نام", msg.Name)
            .Line(isMail ? "📧" : "📞", isMail ? "ایمیل برای جواب" : "تلفن برای جواب", msg.ContactInfo)
            .Line("🌐", "زبان صفحه‌ی مشتری", TelegramText.LanguageLine(msg.LanguageCode))
            .Line("🕒", "زمان ارسال", TelegramText.Now(settings.TimeZone))
            .Block("💬", "متن پیام", msg.Message)
            .Next(isMail
                    ? "به همین ایمیل جواب بدهید. در پنل دکمه‌ی «ایمیل» متن آماده دارد."
                    : "با همین شماره تماس بگیرید یا در واتس‌اپ جواب بدهید. در پنل دکمه‌های «تماس» و «واتس‌اپ» آماده‌اند.",
                ("باز کردن پیام در پنل", link), ("پیام در واتس‌اپ به مشتری", isMail ? null : TelegramText.WhatsApp(msg.ContactInfo)));
        notifications.Enqueue(new Notification(NotificationKinds.Message,
            tg.ToString(),
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
        else if (settings.ReviewsRequireLogin && input.CustomerId is null) errors[""] = "account.loginRequired";
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
            CustomerId = input.CustomerId,
            IsVisible = true,
            SortOrder = 0
        };
        var spam = input.CustomerId is null ? SpamReason(settings, panelBaseUrl, review.AuthorName, review.Text) : null;
        if (spam is not null) review.DeletedAtUtc = DateTime.UtcNow;
        db.Reviews.Add(review);
        await db.SaveChangesAsync(ct);
        if (spam is not null) return SubmitResult.Success(review.Id);

        var reviewService = review.ServiceId is int rsid
            ? (await content.GetServicesAsync(TelegramText.Fa, ct)).FirstOrDefault(x => x.Id == rsid)?.Name : null;
        var tg = new TelegramText("⭐", "نظر جدید — منتظر تأیید شما",
                "یک مشتری در صفحه‌ی «نظرات» سایت نظر نوشته است. این نظر تا وقتی شما آن را تأیید نکنید در سایت نمایش داده نمی‌شود.")
            .Line("👤", "نام", review.AuthorName + (review.ShowInitialsOnly ? " (خواسته فقط حرف اول نامش نمایش داده شود)" : ""))
            .Line("⭐", "امتیاز", review.Rating is int st ? TelegramText.Stars(st) : null, "امتیاز نداده")
            .Line("💆‍♀️", "درباره‌ی خدمت", reviewService)
            .Line("🌐", "زبان", TelegramText.LanguageName(review.LanguageCode))
            .Line("🕒", "زمان ارسال", TelegramText.Now(settings.TimeZone))
            .Block("💬", "متن نظر", review.Text)
            .Next("نظر را در پنل بخوانید و «تأیید» یا «رد» را بزنید. فقط نظرهای تأییدشده در سایت دیده می‌شوند.",
                ("نظرهای منتظر تأیید در پنل", $"{panelBaseUrl}/reviews?tab=pending"));
        notifications.Enqueue(new Notification(NotificationKinds.Review,
            tg.ToString(),
            "نظر جدید", review.Text));
        return SubmitResult.Success(review.Id);
    }

    public async Task<SubmitResult> SubscribeAsync(string? email, bool privacyAccepted, SiteLanguage lang, CancellationToken ct = default, string panelBaseUrl = "")
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
            new TelegramText("📰", "عضو جدید خبرنامه",
                    "یک نفر در پایین صفحه‌ی اصلی سایت ایمیلش را برای دریافت خبرها و نکته‌های مراقبت پوست ثبت کرده است.")
                .Line("📧", "ایمیل", sub.Email)
                .Line("🌐", "زبان", TelegramText.LanguageName(sub.LanguageCode))
                .Line("🕒", "زمان ثبت", TelegramText.Now(settings.TimeZone))
                .Next("کاری لازم نیست. فهرست کامل اعضا در پنل، بخش «خبرنامه» است و می‌توانید آن را دانلود کنید.",
                    ("خبرنامه در پنل", panelBaseUrl.Length > 0 ? $"{panelBaseUrl}/newsletter" : null))
                .ToString(),
            "عضو جدید خبرنامه", sub.Email));
        return SubmitResult.Success(sub.Id);
    }
}
