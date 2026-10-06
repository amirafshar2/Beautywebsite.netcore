using BeautyByNegin.Business.Localization;
using BeautyByNegin.Business.Media;
using BeautyByNegin.DataAccess;
using BeautyByNegin.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace BeautyByNegin.Business.Content;

public interface IContentService
{
    Task<HomeData> GetHomeAsync(SiteLanguage lang, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceCard>> GetServicesAsync(SiteLanguage lang, CancellationToken ct = default);
    Task<ServiceLookup> GetServiceAsync(SiteLanguage lang, string slug, CancellationToken ct = default);
    Task<AboutData> GetAboutAsync(SiteLanguage lang, CancellationToken ct = default);
    Task<GalleryData> GetGalleryAsync(SiteLanguage lang, CancellationToken ct = default);
    Task<IReadOnlyList<ReviewView>> GetReviewsAsync(SiteLanguage lang, int? take = null, CancellationToken ct = default);
    Task<PageLookup> GetPageAsync(SiteLanguage lang, string slug, CancellationToken ct = default);
    Task<IReadOnlyList<OptionView>> GetTimeSlotsAsync(SiteLanguage lang, CancellationToken ct = default);
    Task<ContactData> GetContactAsync(SiteLanguage lang, CancellationToken ct = default);
    void Invalidate();
}

/// <summary>
/// Read side of the public site. Every result is per language, falls back to the default language
/// when a translation is missing, and is cached in memory until the admin saves something.
/// </summary>
public sealed class ContentService(
    IServiceScopeFactory scopeFactory,
    IMemoryCache cache,
    ILanguageService languages,
    Settings.ISettingsService settingsService) : IContentService
{
    private static CancellationTokenSource _reset = new();

    // ------------------------------------------------------------------ helpers

    private async Task<T> Cached<T>(string key, Func<AppDbContext, string, Task<T>> load, CancellationToken ct)
    {
        if (cache.TryGetValue(key, out T? value) && value is not null) return value;
        var fallback = (await languages.GetDefaultAsync(ct)).Code;
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        value = await load(db, fallback);
        cache.Set(key, value, new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromHours(6))
            .AddExpirationToken(new CancellationChangeToken(_reset.Token)));
        return value;
    }

    public void Invalidate()
    {
        var old = Interlocked.Exchange(ref _reset, new CancellationTokenSource());
        old.Cancel();
        old.Dispose();
    }

    /// <summary>Translation in the requested language, else default language, else any.</summary>
    internal static T? Tr<T>(IEnumerable<T> translations, string lang, string fallback) where T : TranslationBase
    {
        var list = translations as IList<T> ?? translations.ToList();
        return list.FirstOrDefault(t => t.LanguageCode == lang)
               ?? list.FirstOrDefault(t => t.LanguageCode == fallback)
               ?? list.FirstOrDefault();
    }

    private static string? Pick(string? value, string? fallbackValue) => string.IsNullOrWhiteSpace(value) ? fallbackValue : value;

    private static ImageView Img(MediaImage? image, string lang, string fallback, string ratio, int width = 960)
        => MediaUrls.ToView(image, lang, fallback, width) ?? ImageView.Placeholder(ratio);

    private static IQueryable<Service> ServiceQuery(AppDbContext db) => db.Services.AsNoTracking()
        .Where(s => s.IsVisible)
        .OrderBy(s => s.SortOrder).ThenBy(s => s.Id)
        .Include(s => s.Translations)
        .Include(s => s.CoverImage!).ThenInclude(i => i.Translations)
        .AsSplitQuery();

    private static List<ServiceCard> ToCards(IEnumerable<Service> services, string lang, string fallback, bool showPrices)
    {
        var result = new List<ServiceCard>();
        var n = 0;
        foreach (var s in services)
        {
            n++;
            var t = Tr(s.Translations, lang, fallback);
            if (t is null) continue;
            var f = Tr(s.Translations, fallback, fallback);
            result.Add(new ServiceCard(s.Id, n, t.Name,
                Pick(t.Subtitle, f?.Subtitle),
                Pick(t.ShortDescription, f?.ShortDescription),
                t.Slug,
                Img(s.CoverImage, lang, fallback, "4x5"),
                showPrices ? s.Price : null));
        }
        return result;
    }

    // ------------------------------------------------------------------ queries

    public Task<IReadOnlyList<ServiceCard>> GetServicesAsync(SiteLanguage lang, CancellationToken ct = default)
        => Cached<IReadOnlyList<ServiceCard>>($"content:services:{lang.Code}", async (db, fallback) =>
        {
            var settings = await settingsService.GetAsync(ct);
            var services = await ServiceQuery(db).ToListAsync(ct);
            return ToCards(services, lang.Code, fallback, settings.ShowPrices);
        }, ct);

    public async Task<ServiceLookup> GetServiceAsync(SiteLanguage lang, string slug, CancellationToken ct = default)
    {
        slug = (slug ?? "").Trim().ToLowerInvariant();
        var all = await GetServicesAsync(lang, ct);
        var card = all.FirstOrDefault(c => c.Slug == slug);

        if (card is null)
        {
            // Old address (renamed service) or the slug of another language -> 301 to the current address.
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var serviceId = await db.ServiceSlugHistory.AsNoTracking()
                .Where(h => h.OldSlug == slug && h.LanguageCode == lang.Code)
                .OrderByDescending(h => h.ChangedAtUtc).Select(h => (int?)h.ServiceId).FirstOrDefaultAsync(ct)
                ?? await db.ServiceTranslations.AsNoTracking()
                .Where(t => t.Slug == slug).Select(t => (int?)t.ServiceId).FirstOrDefaultAsync(ct);
            var target = serviceId is null ? null : all.FirstOrDefault(c => c.Id == serviceId);
            return new ServiceLookup(null, target?.Slug);
        }

        var detail = await Cached($"content:service:{lang.Code}:{card.Id}", async (db, fallback) =>
        {
            var s = await db.Services.AsNoTracking()
                .Include(x => x.Translations)
                .Include(x => x.Images.OrderBy(i => i.SortOrder)).ThenInclude(i => i.MediaImage!).ThenInclude(m => m.Translations)
                .AsSplitQuery()
                .FirstAsync(x => x.Id == card.Id, ct);
            var t = Tr(s.Translations, lang.Code, fallback)!;
            var f = Tr(s.Translations, fallback, fallback);

            var gallery = s.Images.Select(i => MediaUrls.ToView(i.MediaImage, lang.Code, fallback, 960)).OfType<ImageView>().ToList();
            var slugs = s.Translations.ToDictionary(x => x.LanguageCode, x => x.Slug);

            // Related: next services in the list (wrapping around), max 3.
            var index = all.ToList().FindIndex(c => c.Id == card.Id);
            var related = all.Skip(index + 1).Concat(all.Take(index)).Take(3).ToList();

            return new ServiceDetail(card,
                Pick(t.Description, f?.Description),
                Pick(t.SuitableFor, f?.SuitableFor),
                Pick(t.ExpectedResult, f?.ExpectedResult),
                Pick(t.Duration, f?.Duration),
                gallery, t.MetaTitle, t.MetaDescription, slugs, related);
        }, ct);

        return new ServiceLookup(detail, null);
    }

    public Task<HomeData> GetHomeAsync(SiteLanguage lang, CancellationToken ct = default)
        => Cached($"content:home:{lang.Code}", async (db, fallback) =>
        {
            var settings = await settingsService.GetAsync(ct);
            var sections = await db.HomeSections.AsNoTracking().Where(s => s.IsVisible)
                .OrderBy(s => s.SortOrder).Select(s => s.Key).ToListAsync(ct);

            var imageIds = new[] { settings.HeroImageId, settings.AboutImageId, settings.ConsultationImageId }.OfType<int>().ToList();
            var images = await db.MediaImages.AsNoTracking().Include(i => i.Translations)
                .Where(i => imageIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, ct);
            MediaImage? ById(int? id) => id is int i && images.TryGetValue(i, out var m) ? m : null;

            var services = await GetServicesAsync(lang, ct);
            var featuredIds = await db.Services.AsNoTracking().Where(s => s.IsVisible && s.ShowOnHome).Select(s => s.Id).ToListAsync(ct);
            var featured = services.Where(s => featuredIds.Contains(s.Id)).Take(6).ToList();

            var gallery = (await GetGalleryAsync(lang, ct)).Items;
            var homeGalleryIds = await db.GalleryItems.AsNoTracking().Where(g => g.IsVisible && g.ShowOnHome).Select(g => g.Id).ToListAsync(ct);
            var homeGallery = gallery.Where(g => homeGalleryIds.Contains(g.Id)).Take(6).ToList();
            if (homeGallery.Count == 0) homeGallery = gallery.Take(6).ToList();

            var reviews = await GetReviewsAsync(lang, 6, ct);

            var insta = await db.InstagramPosts.AsNoTracking().Where(p => p.IsVisible)
                .OrderBy(p => p.SortOrder).Include(p => p.Image!).ThenInclude(i => i.Translations)
                .Take(9).ToListAsync(ct);
            var profile = ContactLinks.Instagram(settings.Instagram);
            var instaViews = insta
                .Select(p => (Img: MediaUrls.ToView(p.Image, lang.Code, fallback, 480), Url: string.IsNullOrWhiteSpace(p.LinkUrl) ? profile : p.LinkUrl))
                .Where(x => x.Img is not null && x.Url is not null)
                .Select(x => new InstagramView(x.Img!, x.Url!)).ToList();

            return new HomeData(sections,
                Img(ById(settings.HeroImageId), lang.Code, fallback, "16x9", 1600),
                Img(ById(settings.AboutImageId), lang.Code, fallback, "4x5"),
                MediaUrls.ToView(ById(settings.ConsultationImageId), lang.Code, fallback, 960),
                featured, homeGallery, reviews, instaViews);
        }, ct);

    public Task<AboutData> GetAboutAsync(SiteLanguage lang, CancellationToken ct = default)
        => Cached($"content:about:{lang.Code}", async (db, fallback) =>
        {
            var settings = await settingsService.GetAsync(ct);
            var image = settings.AboutImageId is int id
                ? await db.MediaImages.AsNoTracking().Include(i => i.Translations).FirstOrDefaultAsync(i => i.Id == id, ct)
                : null;
            var items = await db.ListItems.AsNoTracking().Where(i => i.IsVisible)
                .OrderBy(i => i.SortOrder).Include(i => i.Translations).ToListAsync(ct);
            List<ListItemView> Of(string key) => items.Where(i => i.ListKey == key)
                .Select(i => Tr(i.Translations, lang.Code, fallback)).OfType<ListItemTranslation>()
                .Where(t => !string.IsNullOrWhiteSpace(t.Text))
                .Select(t => new ListItemView(t.Text, t.Detail)).ToList();
            return new AboutData(Img(image, lang.Code, fallback, "4x5"), Of(ListKeys.Expertise), Of(ListKeys.Certificates));
        }, ct);

    public Task<GalleryData> GetGalleryAsync(SiteLanguage lang, CancellationToken ct = default)
        => Cached($"content:gallery:{lang.Code}", async (db, fallback) =>
        {
            var categories = await db.GalleryCategories.AsNoTracking().Where(c => c.IsVisible)
                .OrderBy(c => c.SortOrder).Include(c => c.Translations).ToListAsync(ct);
            var visibleCategoryIds = categories.Select(c => c.Id).ToHashSet();

            var items = await db.GalleryItems.AsNoTracking()
                .Where(g => g.IsVisible)
                .OrderBy(g => g.SortOrder).ThenByDescending(g => g.Id)
                .Include(g => g.Translations)
                .Include(g => g.Image!).ThenInclude(i => i.Translations)
                .Include(g => g.AfterImage!).ThenInclude(i => i.Translations)
                .AsSplitQuery()
                .ToListAsync(ct);

            var views = items
                // Items of a hidden category are hidden too; items without category stay visible.
                .Where(g => g.CategoryId is null || visibleCategoryIds.Contains(g.CategoryId.Value))
                .Select(g => (g, img: MediaUrls.ToView(g.Image, lang.Code, fallback, 960)))
                .Where(x => x.img is not null)
                .Select(x => new GalleryItemView(x.g.Id, x.g.CategoryId, x.img!,
                    MediaUrls.ToView(x.g.AfterImage, lang.Code, fallback, 960),
                    Tr(x.g.Translations, lang.Code, fallback)?.Caption))
                .ToList();

            // Only categories that actually contain photos become filter tabs.
            var usedIds = views.Select(v => v.CategoryId).OfType<int>().ToHashSet();
            var cats = categories.Where(c => usedIds.Contains(c.Id))
                .Select(c => new GalleryCategoryView(c.Id, Tr(c.Translations, lang.Code, fallback)?.Name ?? ""))
                .ToList();
            return new GalleryData(cats, views);
        }, ct);

    public Task<IReadOnlyList<ReviewView>> GetReviewsAsync(SiteLanguage lang, int? take = null, CancellationToken ct = default)
        => Cached<IReadOnlyList<ReviewView>>($"content:reviews:{lang.Code}:{take}", async (db, fallback) =>
        {
            var reviews = await db.Reviews.AsNoTracking()
                .Where(r => r.IsVisible && r.Status == ReviewStatus.Approved)
                .Include(r => r.Service!).ThenInclude(s => s.Translations)
                .ToListAsync(ct);
            // Visitor's language first, then the admin's order, newest first.
            var ordered = reviews
                .OrderBy(r => r.LanguageCode == lang.Code ? 0 : 1)
                .ThenBy(r => r.SortOrder)
                .ThenByDescending(r => r.ReviewDate ?? DateOnly.FromDateTime(r.CreatedAtUtc))
                .Select(r => new ReviewView(r.Id, r.DisplayName, r.Rating, r.Text, r.LanguageCode, r.ReviewDate,
                    r.Service is null ? null : Tr(r.Service.Translations, lang.Code, fallback)?.Name));
            return (take is int n ? ordered.Take(n) : ordered).ToList();
        }, ct);

    public async Task<PageLookup> GetPageAsync(SiteLanguage lang, string slug, CancellationToken ct = default)
    {
        slug = (slug ?? "").Trim().ToLowerInvariant();
        var pages = await Cached<IReadOnlyList<PageView>>($"content:pages:{lang.Code}", async (db, fallback) =>
        {
            var list = await db.Pages.AsNoTracking().Where(p => p.IsVisible)
                .Include(p => p.Translations).ToListAsync(ct);
            return list.Select(p =>
            {
                var t = Tr(p.Translations, lang.Code, fallback)!;
                return new PageView(p.Id, p.SystemKey, t.Title, t.Content, t.MetaTitle, t.MetaDescription,
                    p.Translations.ToDictionary(x => x.LanguageCode, x => x.Slug));
            }).ToList();
        }, ct);

        var page = pages.FirstOrDefault(p => p.Slugs.TryGetValue(lang.Code, out var s) ? s == slug : false)
                   ?? pages.FirstOrDefault(p => !p.Slugs.ContainsKey(lang.Code) && p.Slugs.Values.Contains(slug));
        if (page is not null) return new PageLookup(page, null);

        // Slug of another language (e.g. /de/privacy) -> redirect to this language's address.
        var other = pages.FirstOrDefault(p => p.Slugs.Values.Contains(slug));
        return new PageLookup(null, other is not null && other.Slugs.TryGetValue(lang.Code, out var own) ? own : null);
    }

    public Task<IReadOnlyList<OptionView>> GetTimeSlotsAsync(SiteLanguage lang, CancellationToken ct = default)
        => Cached<IReadOnlyList<OptionView>>($"content:slots:{lang.Code}", async (db, fallback) =>
        {
            var slots = await db.TimeSlots.AsNoTracking().Where(s => s.IsVisible)
                .OrderBy(s => s.SortOrder).Include(s => s.Translations).ToListAsync(ct);
            return slots.Select(s => new OptionView(s.Id, Tr(s.Translations, lang.Code, fallback)?.Label ?? ""))
                .Where(o => o.Label.Length > 0).ToList();
        }, ct);

    public Task<ContactData> GetContactAsync(SiteLanguage lang, CancellationToken ct = default)
        => Cached($"content:contact:{lang.Code}", async (db, fallback) =>
        {
            var settings = await settingsService.GetAsync(ct);
            var map = settings.MapImageId is int id
                ? await db.MediaImages.AsNoTracking().Include(i => i.Translations).FirstOrDefaultAsync(i => i.Id == id, ct)
                : null;
            return new ContactData(MediaUrls.ToView(map, lang.Code, fallback, 960));
        }, ct);
}
