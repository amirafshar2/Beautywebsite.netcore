using BeautyByNegin.Business.Content;
using BeautyByNegin.DataAccess;
using BeautyByNegin.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace BeautyByNegin.Business.Admin;

// ------------------------------------------------------------------ edit models (one entry per language)

public sealed class ServiceTrInput
{
    public string? Name { get; set; }
    public string? Subtitle { get; set; }
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public string? SuitableFor { get; set; }
    public string? ExpectedResult { get; set; }
    public string? Duration { get; set; }
    public string? Slug { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}

public sealed class ServiceInput
{
    public int Id { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool ShowOnHome { get; set; }
    public decimal? Price { get; set; }
    public int? CoverImageId { get; set; }
    /// <summary>Extra images, comma separated ids in display order.</summary>
    public string? ImageIds { get; set; }
    public Dictionary<string, ServiceTrInput> Tr { get; set; } = [];
}

public sealed class PageTrInput
{
    public string? Title { get; set; }
    public string? Slug { get; set; }
    public string? Content { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}

public sealed class PageInput
{
    public int Id { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool ShowInFooter { get; set; } = true;
    public bool ShowInMenu { get; set; }
    public Dictionary<string, PageTrInput> Tr { get; set; } = [];
}

public sealed class ReviewInputModel
{
    public int Id { get; set; }
    public string? AuthorName { get; set; }
    public bool ShowInitialsOnly { get; set; }
    public int? Rating { get; set; }
    public string? Text { get; set; }
    public string LanguageCode { get; set; } = "fa";
    public DateOnly? ReviewDate { get; set; }
    public int? ServiceId { get; set; }
    public ReviewSource Source { get; set; }
    public bool IsVisible { get; set; } = true;
}

/// <summary>Simple "one text per language" items: gallery categories, time slots, list items, captions.</summary>
public sealed class NamedInput
{
    public int Id { get; set; }
    public bool IsVisible { get; set; } = true;
    public Dictionary<string, string?> Text { get; set; } = [];
    public Dictionary<string, string?> Detail { get; set; } = [];
}

public sealed class GalleryItemInput
{
    public int Id { get; set; }
    public int? CategoryId { get; set; }
    public int ImageId { get; set; }
    public int? AfterImageId { get; set; }
    public bool ShowOnHome { get; set; }
    public bool IsVisible { get; set; } = true;
    public Dictionary<string, string?> Caption { get; set; } = [];
}

public sealed class OpeningHourInput
{
    public int Id { get; set; }
    public bool IsClosed { get; set; }
    public string? Opens { get; set; }
    public string? Closes { get; set; }
}

/// <summary>Validation: field -> panel text key.</summary>
public sealed record SaveResult(bool Ok, int Id, Dictionary<string, string> Errors)
{
    public static SaveResult Success(int id) => new(true, id, []);
    public static SaveResult Fail(Dictionary<string, string> errors) => new(false, 0, errors);
}

public interface IAdminCatalogService
{
    // Services
    Task<IReadOnlyList<Service>> GetServicesAsync(CancellationToken ct = default);
    Task<Service?> GetServiceAsync(int id, CancellationToken ct = default);
    Task<SaveResult> SaveServiceAsync(ServiceInput input, string defaultLanguage, CancellationToken ct = default);
    Task<int?> DuplicateServiceAsync(int id, CancellationToken ct = default);
    Task<bool> ToggleServiceHomeAsync(int id, CancellationToken ct = default);

    // Pages
    Task<IReadOnlyList<Page>> GetPagesAsync(CancellationToken ct = default);
    Task<Page?> GetPageAsync(int id, CancellationToken ct = default);
    Task<SaveResult> SavePageAsync(PageInput input, string defaultLanguage, CancellationToken ct = default);

    // Reviews
    Task<IReadOnlyList<Review>> GetReviewsAsync(ReviewStatus status, CancellationToken ct = default);
    Task<Review?> GetReviewAsync(int id, CancellationToken ct = default);
    Task<SaveResult> SaveReviewAsync(ReviewInputModel input, CancellationToken ct = default);
    Task<bool> ApproveReviewAsync(int id, CancellationToken ct = default);

    // Gallery
    Task<IReadOnlyList<GalleryCategory>> GetGalleryCategoriesAsync(CancellationToken ct = default);
    Task<SaveResult> SaveGalleryCategoryAsync(NamedInput input, string defaultLanguage, CancellationToken ct = default);
    Task<IReadOnlyList<GalleryItem>> GetGalleryItemsAsync(int? categoryId, CancellationToken ct = default);
    Task<GalleryItem?> GetGalleryItemAsync(int id, CancellationToken ct = default);
    Task<SaveResult> SaveGalleryItemAsync(GalleryItemInput input, CancellationToken ct = default);
    Task<int> AddGalleryItemsAsync(IEnumerable<int> imageIds, int? categoryId, CancellationToken ct = default);
    Task<bool> ToggleGalleryHomeAsync(int id, CancellationToken ct = default);

    // Simple lists
    Task<IReadOnlyList<ListItem>> GetListItemsAsync(string listKey, CancellationToken ct = default);
    Task<SaveResult> SaveListItemAsync(string listKey, NamedInput input, string defaultLanguage, CancellationToken ct = default);
    Task<IReadOnlyList<TimeSlot>> GetTimeSlotsAsync(CancellationToken ct = default);
    Task<SaveResult> SaveTimeSlotAsync(NamedInput input, string defaultLanguage, CancellationToken ct = default);
    Task<IReadOnlyList<InstagramPost>> GetInstagramPostsAsync(CancellationToken ct = default);
    Task<int> AddInstagramPostsAsync(IEnumerable<int> imageIds, CancellationToken ct = default);
    Task<bool> SetInstagramLinkAsync(int id, string? url, CancellationToken ct = default);

    // Home & hours
    Task<IReadOnlyList<HomeSection>> GetHomeSectionsAsync(CancellationToken ct = default);
    Task<bool> ToggleHomeSectionAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<OpeningHour>> GetOpeningHoursAsync(CancellationToken ct = default);
    Task SaveOpeningHoursAsync(IEnumerable<OpeningHourInput> hours, CancellationToken ct = default);
}

public sealed class AdminCatalogService(AppDbContext db, IAdminData data) : IAdminCatalogService
{
    // ================================================================ Services

    public async Task<IReadOnlyList<Service>> GetServicesAsync(CancellationToken ct = default)
        => await db.Services.AsNoTracking().Include(s => s.Translations).Include(s => s.CoverImage)
            .OrderBy(s => s.SortOrder).ThenBy(s => s.Id).AsSplitQuery().ToListAsync(ct);

    public Task<Service?> GetServiceAsync(int id, CancellationToken ct = default)
        => db.Services.Include(s => s.Translations)
            .Include(s => s.CoverImage!).ThenInclude(i => i.Translations)
            .Include(s => s.Images.OrderBy(i => i.SortOrder)).ThenInclude(i => i.MediaImage)
            .AsSplitQuery().FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<SaveResult> SaveServiceAsync(ServiceInput input, string defaultLanguage, CancellationToken ct = default)
    {
        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(input.Tr.GetValueOrDefault(defaultLanguage)?.Name))
            errors[$"Tr[{defaultLanguage}].Name"] = "err.nameRequired";

        // Page address: auto from the name when empty, must be unique per language.
        foreach (var (lang, t) in input.Tr)
        {
            if (string.IsNullOrWhiteSpace(t.Name)) continue;
            t.Slug = Slug.From(string.IsNullOrWhiteSpace(t.Slug) ? t.Name : t.Slug);
            if (await db.ServiceTranslations.AnyAsync(x => x.LanguageCode == lang && x.Slug == t.Slug && x.ServiceId != input.Id, ct))
                errors[$"Tr[{lang}].Slug"] = "err.slugTaken";
        }
        if (errors.Count > 0) return SaveResult.Fail(errors);

        var service = input.Id == 0 ? null : await db.Services.Include(s => s.Translations).Include(s => s.Images)
            .FirstOrDefaultAsync(s => s.Id == input.Id, ct);
        if (service is null)
        {
            service = new Service { SortOrder = await data.NextSortOrderAsync<Service>(null, ct) };
            db.Services.Add(service);
        }

        service.IsVisible = input.IsVisible;
        service.ShowOnHome = input.ShowOnHome;
        service.Price = input.Price;
        service.CoverImageId = input.CoverImageId;

        foreach (var (lang, t) in input.Tr)
        {
            var row = service.Translations.FirstOrDefault(x => x.LanguageCode == lang);
            if (string.IsNullOrWhiteSpace(t.Name))
            {
                if (row is not null) service.Translations.Remove(row); // language emptied -> falls back to default
                continue;
            }
            if (row is null)
            {
                row = new ServiceTranslation { LanguageCode = lang };
                service.Translations.Add(row);
            }
            else if (row.Slug != t.Slug && !string.IsNullOrEmpty(row.Slug) && service.Id != 0)
            {
                // Address changed: remember the old one for a 301 redirect.
                db.ServiceSlugHistory.Add(new ServiceSlugHistory { ServiceId = service.Id, LanguageCode = lang, OldSlug = row.Slug });
            }
            row.Name = t.Name!.Trim();
            row.Subtitle = t.Subtitle?.Trim();
            row.ShortDescription = t.ShortDescription?.Trim();
            row.Description = RichText.Clean(t.Description);
            row.SuitableFor = t.SuitableFor?.Trim();
            row.ExpectedResult = t.ExpectedResult?.Trim();
            row.Duration = t.Duration?.Trim();
            row.Slug = t.Slug!;
            row.MetaTitle = t.MetaTitle?.Trim();
            row.MetaDescription = t.MetaDescription?.Trim();
        }

        var ids = (input.ImageIds ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s, out var i) ? i : 0).Where(i => i > 0).Distinct().ToList();
        service.Images.RemoveAll(i => !ids.Contains(i.MediaImageId));
        for (var i = 0; i < ids.Count; i++)
        {
            var existing = service.Images.FirstOrDefault(x => x.MediaImageId == ids[i]);
            if (existing is null) service.Images.Add(new ServiceImage { MediaImageId = ids[i], SortOrder = i });
            else existing.SortOrder = i;
        }

        await db.SaveChangesAsync(ct);
        data.Changed();
        return SaveResult.Success(service.Id);
    }

    public async Task<int?> DuplicateServiceAsync(int id, CancellationToken ct = default)
    {
        var s = await db.Services.AsNoTracking().Include(x => x.Translations).Include(x => x.Images).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null) return null;
        var copy = new Service
        {
            SortOrder = await data.NextSortOrderAsync<Service>(null, ct),
            IsVisible = false, // copies start hidden so nothing half-finished appears on the site
            ShowOnHome = false,
            Price = s.Price,
            CoverImageId = s.CoverImageId,
            Images = s.Images.Select(i => new ServiceImage { MediaImageId = i.MediaImageId, SortOrder = i.SortOrder }).ToList()
        };
        foreach (var t in s.Translations)
        {
            var baseSlug = t.Slug + "-copy";
            var slug = baseSlug;
            for (var n = 2; await db.ServiceTranslations.AnyAsync(x => x.LanguageCode == t.LanguageCode && x.Slug == slug, ct); n++) slug = $"{baseSlug}-{n}";
            copy.Translations.Add(new ServiceTranslation
            {
                LanguageCode = t.LanguageCode, Name = t.Name + " (2)", Slug = slug, Subtitle = t.Subtitle,
                ShortDescription = t.ShortDescription, Description = t.Description, SuitableFor = t.SuitableFor,
                ExpectedResult = t.ExpectedResult, Duration = t.Duration
            });
        }
        db.Services.Add(copy);
        await db.SaveChangesAsync(ct);
        data.Changed();
        return copy.Id;
    }

    public async Task<bool> ToggleServiceHomeAsync(int id, CancellationToken ct = default)
    {
        var s = await db.Services.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null) return false;
        s.ShowOnHome = !s.ShowOnHome;
        await db.SaveChangesAsync(ct);
        data.Changed();
        return s.ShowOnHome;
    }

    // ================================================================ Pages

    public async Task<IReadOnlyList<Page>> GetPagesAsync(CancellationToken ct = default)
        => await db.Pages.AsNoTracking().Include(p => p.Translations).OrderBy(p => p.SortOrder).ToListAsync(ct);

    public Task<Page?> GetPageAsync(int id, CancellationToken ct = default)
        => db.Pages.Include(p => p.Translations).FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<SaveResult> SavePageAsync(PageInput input, string defaultLanguage, CancellationToken ct = default)
    {
        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(input.Tr.GetValueOrDefault(defaultLanguage)?.Title))
            errors[$"Tr[{defaultLanguage}].Title"] = "err.titleRequired";
        string[] reserved = ["api", "admin", "services", "treatments", "behandlungen", "hizmetler", "about", "gallery", "reviews", "contact", "booking"];
        foreach (var (lang, t) in input.Tr)
        {
            if (string.IsNullOrWhiteSpace(t.Title)) continue;
            t.Slug = Slug.From(string.IsNullOrWhiteSpace(t.Slug) ? t.Title : t.Slug);
            if (string.IsNullOrEmpty(t.Slug) || reserved.Contains(t.Slug)
                || await db.PageTranslations.AnyAsync(x => x.LanguageCode == lang && x.Slug == t.Slug && x.PageId != input.Id, ct))
                errors[$"Tr[{lang}].Slug"] = "err.slugTaken";
        }
        if (errors.Count > 0) return SaveResult.Fail(errors);

        var page = input.Id == 0 ? null : await db.Pages.Include(p => p.Translations).FirstOrDefaultAsync(p => p.Id == input.Id, ct);
        if (page is null)
        {
            page = new Page { SortOrder = await data.NextSortOrderAsync<Page>(null, ct) };
            db.Pages.Add(page);
        }
        page.IsVisible = input.IsVisible;
        page.ShowInFooter = input.ShowInFooter;
        page.ShowInMenu = input.ShowInMenu;
        foreach (var (lang, t) in input.Tr)
        {
            var row = page.Translations.FirstOrDefault(x => x.LanguageCode == lang);
            if (string.IsNullOrWhiteSpace(t.Title))
            {
                if (row is not null) page.Translations.Remove(row);
                continue;
            }
            if (row is null) { row = new PageTranslation { LanguageCode = lang }; page.Translations.Add(row); }
            row.Title = t.Title!.Trim();
            row.Slug = t.Slug!;
            row.Content = RichText.Clean(t.Content);
            row.MetaTitle = t.MetaTitle?.Trim();
            row.MetaDescription = t.MetaDescription?.Trim();
        }
        await db.SaveChangesAsync(ct);
        data.Changed();
        return SaveResult.Success(page.Id);
    }

    // ================================================================ Reviews

    public async Task<IReadOnlyList<Review>> GetReviewsAsync(ReviewStatus status, CancellationToken ct = default)
        => await db.Reviews.AsNoTracking().Include(r => r.Service!).ThenInclude(s => s.Translations)
            .Where(r => r.Status == status).OrderBy(r => r.SortOrder).ThenByDescending(r => r.CreatedAtUtc).ToListAsync(ct);

    public Task<Review?> GetReviewAsync(int id, CancellationToken ct = default) => db.Reviews.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<SaveResult> SaveReviewAsync(ReviewInputModel input, CancellationToken ct = default)
    {
        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(input.AuthorName)) errors["AuthorName"] = "err.nameRequired";
        if (string.IsNullOrWhiteSpace(input.Text)) errors["Text"] = "err.textRequired";
        if (errors.Count > 0) return SaveResult.Fail(errors);

        var r = input.Id == 0 ? null : await db.Reviews.FirstOrDefaultAsync(x => x.Id == input.Id, ct);
        if (r is null)
        {
            r = new Review { SortOrder = 0, Status = ReviewStatus.Approved };
            db.Reviews.Add(r);
        }
        r.AuthorName = input.AuthorName!.Trim();
        r.ShowInitialsOnly = input.ShowInitialsOnly;
        r.Rating = input.Rating is int x and >= 1 and <= 5 ? x : null;
        r.Text = input.Text!.Trim();
        r.LanguageCode = input.LanguageCode;
        r.ReviewDate = input.ReviewDate;
        r.ServiceId = input.ServiceId;
        r.Source = input.Source;
        r.IsVisible = input.IsVisible;
        await db.SaveChangesAsync(ct);
        data.Changed();
        return SaveResult.Success(r.Id);
    }

    public async Task<bool> ApproveReviewAsync(int id, CancellationToken ct = default)
    {
        var r = await db.Reviews.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return false;
        r.Status = ReviewStatus.Approved;
        r.IsVisible = true;
        await db.SaveChangesAsync(ct);
        data.Changed();
        return true;
    }

    // ================================================================ Gallery

    public async Task<IReadOnlyList<GalleryCategory>> GetGalleryCategoriesAsync(CancellationToken ct = default)
        => await db.GalleryCategories.AsNoTracking().Include(c => c.Translations).OrderBy(c => c.SortOrder).ToListAsync(ct);

    public async Task<SaveResult> SaveGalleryCategoryAsync(NamedInput input, string defaultLanguage, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(input.Text.GetValueOrDefault(defaultLanguage)))
            return SaveResult.Fail(new() { ["Text"] = "err.nameRequired" });
        var c = input.Id == 0 ? null : await db.GalleryCategories.Include(x => x.Translations).FirstOrDefaultAsync(x => x.Id == input.Id, ct);
        if (c is null)
        {
            c = new GalleryCategory { SortOrder = await data.NextSortOrderAsync<GalleryCategory>(null, ct) };
            db.GalleryCategories.Add(c);
        }
        c.IsVisible = input.IsVisible;
        SyncTranslations(c.Translations, input.Text, (t, v) => t.Name = v, l => new GalleryCategoryTranslation { LanguageCode = l });
        await db.SaveChangesAsync(ct);
        data.Changed();
        return SaveResult.Success(c.Id);
    }

    public async Task<IReadOnlyList<GalleryItem>> GetGalleryItemsAsync(int? categoryId, CancellationToken ct = default)
    {
        var q = db.GalleryItems.AsNoTracking().Include(g => g.Image).Include(g => g.AfterImage).Include(g => g.Translations).AsQueryable();
        if (categoryId is int c) q = c == 0 ? q.Where(g => g.CategoryId == null) : q.Where(g => g.CategoryId == c);
        return await q.OrderBy(g => g.SortOrder).ThenByDescending(g => g.Id).AsSplitQuery().ToListAsync(ct);
    }

    public Task<GalleryItem?> GetGalleryItemAsync(int id, CancellationToken ct = default)
        => db.GalleryItems.Include(g => g.Image).Include(g => g.AfterImage).Include(g => g.Translations).FirstOrDefaultAsync(g => g.Id == id, ct);

    public async Task<SaveResult> SaveGalleryItemAsync(GalleryItemInput input, CancellationToken ct = default)
    {
        var g = await db.GalleryItems.Include(x => x.Translations).FirstOrDefaultAsync(x => x.Id == input.Id, ct);
        if (g is null) return SaveResult.Fail(new() { [""] = "err.notFound" });
        g.CategoryId = input.CategoryId is > 0 ? input.CategoryId : null;
        if (input.ImageId > 0) g.ImageId = input.ImageId;
        g.AfterImageId = input.AfterImageId is > 0 ? input.AfterImageId : null;
        g.ShowOnHome = input.ShowOnHome;
        g.IsVisible = input.IsVisible;
        SyncTranslations(g.Translations, input.Caption, (t, v) => t.Caption = v, l => new GalleryItemTranslation { LanguageCode = l });
        await db.SaveChangesAsync(ct);
        data.Changed();
        return SaveResult.Success(g.Id);
    }

    public async Task<int> AddGalleryItemsAsync(IEnumerable<int> imageIds, int? categoryId, CancellationToken ct = default)
    {
        // New photos go to the top of the gallery.
        var min = await db.GalleryItems.IgnoreQueryFilters().MinAsync(g => (int?)g.SortOrder, ct) ?? 1;
        var count = 0;
        foreach (var id in imageIds.Reverse())
        {
            db.GalleryItems.Add(new GalleryItem { ImageId = id, CategoryId = categoryId is > 0 ? categoryId : null, SortOrder = --min, IsVisible = true });
            count++;
        }
        await db.SaveChangesAsync(ct);
        data.Changed();
        return count;
    }

    public async Task<bool> ToggleGalleryHomeAsync(int id, CancellationToken ct = default)
    {
        var g = await db.GalleryItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (g is null) return false;
        g.ShowOnHome = !g.ShowOnHome;
        await db.SaveChangesAsync(ct);
        data.Changed();
        return g.ShowOnHome;
    }

    // ================================================================ Lists, time slots, Instagram

    public async Task<IReadOnlyList<ListItem>> GetListItemsAsync(string listKey, CancellationToken ct = default)
        => await db.ListItems.AsNoTracking().Include(i => i.Translations).Where(i => i.ListKey == listKey).OrderBy(i => i.SortOrder).ToListAsync(ct);

    public async Task<SaveResult> SaveListItemAsync(string listKey, NamedInput input, string defaultLanguage, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(input.Text.GetValueOrDefault(defaultLanguage)))
            return SaveResult.Fail(new() { ["Text"] = "err.textRequired" });
        var item = input.Id == 0 ? null : await db.ListItems.Include(x => x.Translations).FirstOrDefaultAsync(x => x.Id == input.Id, ct);
        if (item is null)
        {
            item = new ListItem { ListKey = listKey, SortOrder = await data.NextSortOrderAsync<ListItem>(x => x.ListKey == listKey, ct) };
            db.ListItems.Add(item);
        }
        item.IsVisible = input.IsVisible;
        SyncTranslations(item.Translations, input.Text, (t, v) => t.Text = v, l => new ListItemTranslation { LanguageCode = l });
        foreach (var t in item.Translations) t.Detail = input.Detail.GetValueOrDefault(t.LanguageCode)?.Trim();
        await db.SaveChangesAsync(ct);
        data.Changed();
        return SaveResult.Success(item.Id);
    }

    public async Task<IReadOnlyList<TimeSlot>> GetTimeSlotsAsync(CancellationToken ct = default)
        => await db.TimeSlots.AsNoTracking().Include(t => t.Translations).OrderBy(t => t.SortOrder).ToListAsync(ct);

    public async Task<SaveResult> SaveTimeSlotAsync(NamedInput input, string defaultLanguage, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(input.Text.GetValueOrDefault(defaultLanguage)))
            return SaveResult.Fail(new() { ["Text"] = "err.textRequired" });
        var slot = input.Id == 0 ? null : await db.TimeSlots.Include(x => x.Translations).FirstOrDefaultAsync(x => x.Id == input.Id, ct);
        if (slot is null)
        {
            slot = new TimeSlot { SortOrder = await data.NextSortOrderAsync<TimeSlot>(null, ct) };
            db.TimeSlots.Add(slot);
        }
        slot.IsVisible = input.IsVisible;
        SyncTranslations(slot.Translations, input.Text, (t, v) => t.Label = v, l => new TimeSlotTranslation { LanguageCode = l });
        await db.SaveChangesAsync(ct);
        data.Changed();
        return SaveResult.Success(slot.Id);
    }

    public async Task<IReadOnlyList<InstagramPost>> GetInstagramPostsAsync(CancellationToken ct = default)
        => await db.InstagramPosts.AsNoTracking().Include(p => p.Image).OrderBy(p => p.SortOrder).ToListAsync(ct);

    public async Task<int> AddInstagramPostsAsync(IEnumerable<int> imageIds, CancellationToken ct = default)
    {
        var order = await data.NextSortOrderAsync<InstagramPost>(null, ct);
        var count = 0;
        foreach (var id in imageIds) { db.InstagramPosts.Add(new InstagramPost { ImageId = id, SortOrder = order++ }); count++; }
        await db.SaveChangesAsync(ct);
        data.Changed();
        return count;
    }

    public async Task<bool> SetInstagramLinkAsync(int id, string? url, CancellationToken ct = default)
    {
        var p = await db.InstagramPosts.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return false;
        url = url?.Trim();
        p.LinkUrl = string.IsNullOrEmpty(url) ? null : url.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? url : "https://" + url;
        await db.SaveChangesAsync(ct);
        data.Changed();
        return true;
    }

    // ================================================================ Home sections & hours

    public async Task<IReadOnlyList<HomeSection>> GetHomeSectionsAsync(CancellationToken ct = default)
        => await db.HomeSections.AsNoTracking().OrderBy(s => s.SortOrder).ToListAsync(ct);

    public async Task<bool> ToggleHomeSectionAsync(int id, CancellationToken ct = default)
    {
        var s = await db.HomeSections.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null) return false;
        s.IsVisible = !s.IsVisible;
        await db.SaveChangesAsync(ct);
        data.Changed();
        return s.IsVisible;
    }

    public async Task<IReadOnlyList<OpeningHour>> GetOpeningHoursAsync(CancellationToken ct = default)
        => await db.OpeningHours.AsNoTracking().OrderBy(h => h.SortOrder).ToListAsync(ct);

    public async Task SaveOpeningHoursAsync(IEnumerable<OpeningHourInput> hours, CancellationToken ct = default)
    {
        var rows = await db.OpeningHours.ToDictionaryAsync(h => h.Id, ct);
        foreach (var h in hours)
        {
            if (!rows.TryGetValue(h.Id, out var row)) continue;
            var opens = TimeOnly.TryParse(h.Opens, out var o) ? o : (TimeOnly?)null;
            var closes = TimeOnly.TryParse(h.Closes, out var c) ? c : (TimeOnly?)null;
            row.IsClosed = h.IsClosed || opens is null || closes is null;
            row.Opens = row.IsClosed ? null : opens;
            row.Closes = row.IsClosed ? null : closes;
        }
        await db.SaveChangesAsync(ct);
        data.Changed();
    }

    // ================================================================ helpers

    private static void SyncTranslations<T>(List<T> rows, Dictionary<string, string?> values, Action<T, string> set, Func<string, T> create)
        where T : TranslationBase
    {
        foreach (var (lang, value) in values)
        {
            var row = rows.FirstOrDefault(r => r.LanguageCode == lang);
            if (string.IsNullOrWhiteSpace(value))
            {
                if (row is not null) rows.Remove(row);
                continue;
            }
            if (row is null) { row = create(lang); rows.Add(row); }
            set(row, value.Trim());
        }
    }
}
