using System.Globalization;
using BeautyByNegin.Business.Localization;
using BeautyByNegin.Business.Media;
using BeautyByNegin.DataAccess;
using BeautyByNegin.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BeautyByNegin.Business.Content;

public sealed record FooterPageLink(string Title, string Slug, string? SystemKey);

public sealed record OpeningHourView(DayOfWeek Day, string DayName, bool IsClosed, string? Hours);

/// <summary>Data shared by every public page (header/footer), per language.</summary>
public sealed record LayoutData(
    IReadOnlyList<FooterPageLink> FooterPages,
    IReadOnlyList<FooterPageLink> MenuPages,
    IReadOnlyList<OpeningHourView> Hours,
    ImageView? Logo,
    ImageView? LogoDark,
    string? FaviconUrl,
    /// <summary>Image shown when the site is shared (WhatsApp, Telegram…): share image, else hero image.</summary>
    string? ShareImageUrl);

public interface ILayoutService
{
    Task<LayoutData> GetAsync(SiteLanguage lang, CancellationToken ct = default);
    void Invalidate();
}

public sealed class LayoutService(
    IServiceScopeFactory scopeFactory,
    IMemoryCache cache,
    ILanguageService languages,
    Settings.ISettingsService settingsService) : ILayoutService
{
    private const string CachePrefix = "layout:";
    private static CancellationTokenSource _reset = new();

    public async Task<LayoutData> GetAsync(SiteLanguage lang, CancellationToken ct = default)
    {
        var key = CachePrefix + lang.Code;
        if (cache.TryGetValue(key, out LayoutData? cached) && cached is not null) return cached;

        var fallback = (await languages.GetDefaultAsync(ct)).Code;
        var settings = await settingsService.GetAsync(ct);

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var pages = await db.Pages.AsNoTracking()
            .Where(p => p.IsVisible && (p.ShowInFooter || p.ShowInMenu))
            .OrderBy(p => p.SortOrder)
            .Include(p => p.Translations)
            .ToListAsync(ct);

        FooterPageLink? Link(Page p)
        {
            var t = p.Translations.FirstOrDefault(x => x.LanguageCode == lang.Code)
                    ?? p.Translations.FirstOrDefault(x => x.LanguageCode == fallback);
            return t is null ? null : new FooterPageLink(t.Title, t.Slug, p.SystemKey);
        }

        var footer = pages.Where(p => p.ShowInFooter).Select(Link).OfType<FooterPageLink>().ToList();
        var menu = pages.Where(p => p.ShowInMenu).Select(Link).OfType<FooterPageLink>().ToList();

        var culture = lang.CreateCulture();
        var firstDay = WeekStart.For(settings.Text(SettingKeys.CountryCode));
        var hours = (await db.OpeningHours.AsNoTracking().ToListAsync(ct))
            .OrderBy(h => WeekStart.Position(h.Day, firstDay))
            .Select(h => new OpeningHourView(
                h.Day,
                culture.DateTimeFormat.GetDayName(h.Day),
                h.IsClosed || h.Opens is null || h.Closes is null,
                h.IsClosed || h.Opens is null || h.Closes is null
                    ? null
                    : FormatTime(h.Opens.Value, lang) + " – " + FormatTime(h.Closes.Value, lang)))
            .ToList();

        var imageIds = new[] { settings.LogoImageId, settings.LogoDarkImageId, settings.FaviconImageId, settings.ShareImageId, settings.HeroImageId }.OfType<int>().ToList();
        var images = await db.MediaImages.AsNoTracking().Include(i => i.Translations)
            .Where(i => imageIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, ct);
        ImageView? Img(int? id, int width) => id is int i && images.TryGetValue(i, out var m)
            ? MediaUrls.ToView(m, lang.Code, fallback, width) : null;

        var favicon = settings.FaviconImageId is int fid && images.TryGetValue(fid, out var fav)
            ? MediaUrls.Url(fav, fav.WidthList.Min()) : null;

        var shareId = settings.ShareImageId ?? settings.HeroImageId;
        var share = shareId is int sid && images.TryGetValue(sid, out var shareImg) ? MediaUrls.Url(shareImg, shareImg.WidthList.First(w => w >= Math.Min(960, shareImg.WidthList.Max()))) : null;
        var data = new LayoutData(footer, menu, hours, Img(settings.LogoImageId, 480), Img(settings.LogoDarkImageId, 480), favicon, share);
        cache.Set(key, data, new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromHours(6))
            .AddExpirationToken(new Microsoft.Extensions.Primitives.CancellationChangeToken(_reset.Token)));
        return data;
    }

    private static string FormatTime(TimeOnly t, SiteLanguage lang)
    {
        return Digits.Native(t.ToString("HH:mm", CultureInfo.InvariantCulture), lang);
    }

    public void Invalidate()
    {
        var old = Interlocked.Exchange(ref _reset, new CancellationTokenSource());
        old.Cancel();
        old.Dispose();
    }
}
