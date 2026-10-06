using System.Globalization;
using BeautyByNegin.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BeautyByNegin.Web.Infrastructure.Localization;

/// <summary>Immutable snapshot of a language row, safe to cache and share between requests.</summary>
public sealed record SiteLanguage(
    string Code,
    string CultureName,
    string NativeName,
    string ShortLabel,
    bool IsRtl,
    bool IsEnabled,
    bool IsDefault,
    int SortOrder,
    bool UseNativeDigits)
{
    public string Dir => IsRtl ? "rtl" : "ltr";

    /// <summary>
    /// Culture used for the request. Persian keeps the Gregorian calendar for parsing/formatting so
    /// model binding never misreads dates; Jalali display is done explicitly by <see cref="DateDisplay"/>.
    /// </summary>
    public CultureInfo CreateCulture()
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo(CultureName).Clone();
        if (culture.DateTimeFormat.Calendar is not GregorianCalendar)
            culture.DateTimeFormat.Calendar = new GregorianCalendar();
        return culture;
    }
}

public interface ILanguageService
{
    Task<IReadOnlyList<SiteLanguage>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SiteLanguage>> GetEnabledAsync(CancellationToken ct = default);
    Task<SiteLanguage> GetDefaultAsync(CancellationToken ct = default);
    Task<SiteLanguage?> FindAsync(string? code, CancellationToken ct = default);
    void Invalidate();
}

public sealed class LanguageService(IServiceScopeFactory scopeFactory, IMemoryCache cache) : ILanguageService
{
    private const string CacheKey = "site-languages";

    /// <summary>Every language code the site knows about. Used by the URL constraint.</summary>
    public static readonly string[] KnownCodes = ["fa", "tr", "de", "en"];

    public async Task<IReadOnlyList<SiteLanguage>> GetAllAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(CacheKey, out IReadOnlyList<SiteLanguage>? cached) && cached is not null)
            return cached;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var list = await db.Languages.AsNoTracking()
            .OrderBy(l => l.SortOrder)
            .Select(l => new SiteLanguage(l.Code, l.CultureName, l.NativeName, l.ShortLabel,
                l.IsRtl, l.IsEnabled, l.IsDefault, l.SortOrder, l.UseNativeDigits))
            .ToListAsync(ct);

        cache.Set(CacheKey, (IReadOnlyList<SiteLanguage>)list, TimeSpan.FromHours(6));
        return list;
    }

    public async Task<IReadOnlyList<SiteLanguage>> GetEnabledAsync(CancellationToken ct = default)
        => (await GetAllAsync(ct)).Where(l => l.IsEnabled).ToList();

    public async Task<SiteLanguage> GetDefaultAsync(CancellationToken ct = default)
    {
        var all = await GetAllAsync(ct);
        return all.FirstOrDefault(l => l.IsDefault && l.IsEnabled)
               ?? all.FirstOrDefault(l => l.IsEnabled)
               ?? all.FirstOrDefault()
               ?? new SiteLanguage("en", "en-US", "English", "EN", false, true, true, 0, false);
    }

    public async Task<SiteLanguage?> FindAsync(string? code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        return (await GetAllAsync(ct)).FirstOrDefault(l => string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase));
    }

    public void Invalidate() => cache.Remove(CacheKey);
}
