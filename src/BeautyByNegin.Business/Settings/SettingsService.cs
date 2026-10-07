using Microsoft.Extensions.Configuration;
using System.Globalization;
using BeautyByNegin.DataAccess;
using BeautyByNegin.DataAccess.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BeautyByNegin.Business.Settings;

/// <summary>Read-only snapshot of all non-translatable settings, cached in memory.</summary>
public sealed class SiteSettings(IReadOnlyDictionary<string, string?> values, bool devMailToFile = false)
{
    /// <summary>
    /// Development only ("Site:DevMailToFile" in appsettings.Development.json): without SMTP, e-mails are
    /// written to App_Data/dev-mail and the log instead of being sent, so login and chat can be tested locally.
    /// </summary>
    public bool DevMailToFile { get; } = devMailToFile;

    public string? Get(string key) => values.TryGetValue(key, out var v) ? v : null;
    public string Text(string key, string fallback = "") => string.IsNullOrWhiteSpace(Get(key)) ? fallback : Get(key)!.Trim();
    public bool Bool(string key) => string.Equals(Get(key), "true", StringComparison.OrdinalIgnoreCase);
    public int? Int(string key) => int.TryParse(Get(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : null;

    public string BrandName => Text(SettingKeys.BrandName, "Beauty by Negin");
    public bool SetupCompleted => Bool(SettingKeys.SetupCompleted);
    public bool MaintenanceMode => Bool(SettingKeys.MaintenanceMode);
    public bool ShowPrices => Bool(SettingKeys.ShowPrices);
    public bool AllowVisitorReviews => Bool(SettingKeys.AllowVisitorReviews);
    public bool PrivacyConsentRequired => Bool(SettingKeys.PrivacyConsentRequired);
    public bool NewsletterEnabled => Bool(SettingKeys.NewsletterEnabled);
    public bool ChatEnabled => Bool(SettingKeys.ChatEnabled);
    public bool SmtpConfigured => Bool(SettingKeys.SmtpEnabled) && !string.IsNullOrWhiteSpace(Get(SettingKeys.SmtpHost));
    /// <summary>E-mails can go out (real SMTP, or the local test folder during development).</summary>
    public bool EmailWorks => SmtpConfigured || DevMailToFile;

    /// <summary>The chat is for logged-in customers, so it needs customer accounts (and therefore e-mail).</summary>
    public bool ChatAvailable => ChatEnabled && AccountsAvailable;

    /// <summary>Customer accounts: login with an e-mail code, so they only work when e-mail sending works.</summary>
    public bool AccountsEnabled => Bool(SettingKeys.AccountsEnabled);
    public bool AccountsAvailable => AccountsEnabled && EmailWorks;
    public bool AccountsShowBookings => Bool(SettingKeys.AccountsShowBookings);
    /// <summary>Visitor reviews only from logged-in customers (only applies when accounts are available).</summary>
    public bool ReviewsRequireLogin => Bool(SettingKeys.ReviewsRequireLogin) && AccountsAvailable;

    public string Phone => Text(SettingKeys.Phone);
    public string WhatsApp => Text(SettingKeys.WhatsApp);
    public string Email => Text(SettingKeys.Email);
    public string Instagram => Text(SettingKeys.InstagramUsername).TrimStart('@');
    public string Telegram => Text(SettingKeys.TelegramUsername).TrimStart('@');
    public string MapUrl => Text(SettingKeys.MapUrl);
    public string SiteUrl => Text(SettingKeys.SiteUrl).TrimEnd('/');
    public string City => Text(SettingKeys.City);

    public int? LogoImageId => Int(SettingKeys.LogoImageId);
    public int? LogoDarkImageId => Int(SettingKeys.LogoDarkImageId);
    public int? FaviconImageId => Int(SettingKeys.FaviconImageId);
    public int? ShareImageId => Int(SettingKeys.ShareImageId);
    public int? HeroImageId => Int(SettingKeys.HeroImageId);
    public int? AboutImageId => Int(SettingKeys.AboutImageId);
    public int? ConsultationImageId => Int(SettingKeys.ConsultationImageId);
    public int? MapImageId => Int(SettingKeys.MapImageId);

    public TimeZoneInfo TimeZone
    {
        get
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(Text(SettingKeys.TimeZoneId, "UTC")); }
            catch { return TimeZoneInfo.Utc; }
        }
    }
}

public interface ISettingsService
{
    Task<SiteSettings> GetAsync(CancellationToken ct = default);
    Task SaveAsync(IDictionary<string, string?> values, CancellationToken ct = default);
    /// <summary>Decrypted value of a secret setting (SMTP password, Telegram token).</summary>
    Task<string?> GetSecretAsync(string key, CancellationToken ct = default);
    Task SaveSecretAsync(string key, string? plainValue, CancellationToken ct = default);
    void Invalidate();
}

public sealed class SettingsService(IServiceScopeFactory scopeFactory, IMemoryCache cache, IDataProtectionProvider protection, IConfiguration config) : ISettingsService
{
    private const string CacheKey = "site-settings";
    private readonly IDataProtector _protector = protection.CreateProtector("BeautyByNegin.Settings.Secrets");

    public async Task<SiteSettings> GetAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(CacheKey, out SiteSettings? cached) && cached is not null) return cached;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dict = await db.SiteSettings.AsNoTracking().ToDictionaryAsync(s => s.Key, s => s.Value, ct);
        var settings = new SiteSettings(dict, config.GetValue<bool>("Site:DevMailToFile"));
        cache.Set(CacheKey, settings, TimeSpan.FromHours(6));
        return settings;
    }

    public async Task SaveAsync(IDictionary<string, string?> values, CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existing = await db.SiteSettings.ToDictionaryAsync(s => s.Key, ct);
        foreach (var (key, value) in values)
        {
            if (existing.TryGetValue(key, out var row)) row.Value = value;
            else db.SiteSettings.Add(new SiteSetting { Key = key, Value = value });
        }
        await db.SaveChangesAsync(ct);
        Invalidate();
    }

    public async Task<string?> GetSecretAsync(string key, CancellationToken ct = default)
    {
        var raw = (await GetAsync(ct)).Get(key);
        if (string.IsNullOrEmpty(raw)) return null;
        try { return _protector.Unprotect(raw); }
        catch { return null; } // keys lost (e.g. App_Data/keys not copied) -> admin re-enters the secret
    }

    public Task SaveSecretAsync(string key, string? plainValue, CancellationToken ct = default)
        => SaveAsync(new Dictionary<string, string?>
        {
            [key] = string.IsNullOrEmpty(plainValue) ? null : _protector.Protect(plainValue)
        }, ct);

    public void Invalidate() => cache.Remove(CacheKey);
}
