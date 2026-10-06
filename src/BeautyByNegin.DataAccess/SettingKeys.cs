namespace BeautyByNegin.DataAccess;

/// <summary>Keys of the <c>SiteSettings</c> table (non-translatable settings).</summary>
public static class SettingKeys
{
    // General
    public const string SetupCompleted = "setup.completed";
    public const string BrandName = "brand.name";
    public const string LogoImageId = "brand.logoImageId";
    public const string LogoDarkImageId = "brand.logoDarkImageId";
    public const string FaviconImageId = "brand.faviconImageId";
    public const string ShareImageId = "brand.shareImageId";
    public const string SiteUrl = "site.url";
    public const string CountryCode = "site.country";
    public const string TimeZoneId = "site.timeZone";
    public const string City = "site.city";
    public const string AddCityToTitles = "site.addCityToTitles";
    public const string MaintenanceMode = "site.maintenance";
    public const string HttpsRedirect = "security.httpsRedirect";
    public const string Hsts = "security.hsts";

    // Features
    public const string ShowPrices = "features.showPrices";
    public const string AllowVisitorReviews = "features.visitorReviews";
    public const string PrivacyConsentRequired = "features.privacyConsentRequired";
    public const string NewsletterEnabled = "features.newsletter";
    public const string ChatEnabled = "features.chat";

    // Page images
    public const string HeroImageId = "images.hero";
    public const string AboutImageId = "images.about";
    public const string ConsultationImageId = "images.consultation";
    public const string MapImageId = "images.map";

    // Contact & social
    public const string Phone = "contact.phone";
    public const string WhatsApp = "contact.whatsapp";
    public const string Email = "contact.email";
    public const string InstagramUsername = "contact.instagram";
    public const string TelegramUsername = "contact.telegram";
    public const string MapUrl = "contact.mapUrl";

    // E-mail (SMTP)
    public const string SmtpEnabled = "smtp.enabled";
    public const string SmtpHost = "smtp.host";
    public const string SmtpPort = "smtp.port";
    public const string SmtpUseSsl = "smtp.ssl";
    public const string SmtpUser = "smtp.user";
    /// <summary>Stored encrypted (ASP.NET Core Data Protection).</summary>
    public const string SmtpPassword = "smtp.password";
    public const string SmtpFromAddress = "smtp.from";
    public const string SmtpFromName = "smtp.fromName";
    public const string NotificationEmail = "notify.email";

    // Telegram bot notifications
    public const string TelegramEnabled = "telegram.enabled";
    /// <summary>Stored encrypted (ASP.NET Core Data Protection).</summary>
    public const string TelegramBotToken = "telegram.botToken";
    public const string TelegramChatId = "telegram.chatId";
    public const string TelegramNotifyAppointments = "telegram.notifyAppointments";
    public const string TelegramNotifyMessages = "telegram.notifyMessages";
    public const string TelegramNotifyChat = "telegram.notifyChat";
    public const string TelegramNotifyNewsletter = "telegram.notifyNewsletter";

    // Backups
    public const string AutoBackupEnabled = "backup.auto";
    public const string AutoBackupKeep = "backup.keep";

    /// <summary>Keys whose values are encrypted at rest.</summary>
    public static readonly string[] Secret = [SmtpPassword, TelegramBotToken];
}
