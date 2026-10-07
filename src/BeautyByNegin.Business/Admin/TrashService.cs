using BeautyByNegin.Business.Content;
using BeautyByNegin.Business.Media;
using BeautyByNegin.DataAccess;
using BeautyByNegin.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace BeautyByNegin.Business.Admin;

/// <summary>Kinds of items that can be in the Trash (stable strings, used in URLs and panel texts).</summary>
public static class TrashKinds
{
    public const string Service = "service";
    public const string GalleryItem = "gallery-item";
    public const string GalleryCategory = "gallery-category";
    public const string Review = "review";
    public const string Page = "page";
    public const string Appointment = "appointment";
    public const string Message = "message";
    public const string Chat = "chat";
    public const string Subscriber = "subscriber";
    public const string ListItem = "list-item";
    public const string Instagram = "instagram";
    public const string TimeSlot = "time-slot";
}

public sealed record TrashEntry(string Kind, int Id, string Title, DateTime DeletedAtUtc)
{
    public int DaysLeft => Math.Max(0, TrashService.RetentionDays - (int)(DateTime.UtcNow - DeletedAtUtc).TotalDays);
}

public interface ITrashService
{
    /// <param name="preferredLanguage">Titles are shown in this language when available (the panel language).</param>
    Task<IReadOnlyList<TrashEntry>> ListAsync(string? preferredLanguage = null, CancellationToken ct = default);
    Task<bool> RestoreAsync(string kind, int id, CancellationToken ct = default);
    Task<bool> DeleteForeverAsync(string kind, int id, CancellationToken ct = default);
    Task<int> PurgeExpiredAsync(CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
}

/// <summary>Deleted items stay here for 30 days and can be restored; afterwards they are removed for good.</summary>
public sealed class TrashService(AppDbContext db, IMediaService media, IVideoService videos, ISiteCache cache) : ITrashService
{
    public const int RetentionDays = 30;

    private IQueryable<T> Deleted<T>() where T : class, ISoftDelete
        => db.Set<T>().IgnoreQueryFilters().Where(e => e.DeletedAtUtc != null);

    private string? _preferred;

    private string? Name<TTr>(IEnumerable<TTr> tr, Func<TTr, string?> pick) where TTr : TranslationBase
    {
        var list = tr.OrderBy(t => t.LanguageCode == _preferred ? 0 : 1).ToList();
        return list.Select(pick).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));
    }

    public async Task<IReadOnlyList<TrashEntry>> ListAsync(string? preferredLanguage = null, CancellationToken ct = default)
    {
        _preferred = preferredLanguage;
        var r = new List<TrashEntry>();
        foreach (var s in await Deleted<Service>().Include(x => x.Translations).ToListAsync(ct))
            r.Add(new(TrashKinds.Service, s.Id, Name(s.Translations, t => t.Name) ?? $"#{s.Id}", s.DeletedAtUtc!.Value));
        foreach (var g in await Deleted<GalleryItem>().Include(x => x.Translations).ToListAsync(ct))
            r.Add(new(TrashKinds.GalleryItem, g.Id, Name(g.Translations, t => t.Caption) ?? $"#{g.Id}", g.DeletedAtUtc!.Value));
        foreach (var c in await Deleted<GalleryCategory>().Include(x => x.Translations).ToListAsync(ct))
            r.Add(new(TrashKinds.GalleryCategory, c.Id, Name(c.Translations, t => t.Name) ?? $"#{c.Id}", c.DeletedAtUtc!.Value));
        foreach (var x in await Deleted<Review>().ToListAsync(ct))
            r.Add(new(TrashKinds.Review, x.Id, x.AuthorName, x.DeletedAtUtc!.Value));
        foreach (var p in await Deleted<Page>().Include(x => x.Translations).ToListAsync(ct))
            r.Add(new(TrashKinds.Page, p.Id, Name(p.Translations, t => t.Title) ?? $"#{p.Id}", p.DeletedAtUtc!.Value));
        foreach (var a in await Deleted<AppointmentRequest>().ToListAsync(ct))
            r.Add(new(TrashKinds.Appointment, a.Id, a.FullName, a.DeletedAtUtc!.Value));
        foreach (var m in await Deleted<ContactMessage>().ToListAsync(ct))
            r.Add(new(TrashKinds.Message, m.Id, m.Name, m.DeletedAtUtc!.Value));
        foreach (var v in await Deleted<ChatVisitor>().ToListAsync(ct))
            r.Add(new(TrashKinds.Chat, v.Id, $"{v.Name} ({v.Email})", v.DeletedAtUtc!.Value));
        foreach (var s in await Deleted<NewsletterSubscriber>().ToListAsync(ct))
            r.Add(new(TrashKinds.Subscriber, s.Id, s.Email, s.DeletedAtUtc!.Value));
        foreach (var l in await Deleted<ListItem>().Include(x => x.Translations).ToListAsync(ct))
            r.Add(new(TrashKinds.ListItem, l.Id, Name(l.Translations, t => t.Text) ?? $"#{l.Id}", l.DeletedAtUtc!.Value));
        foreach (var i in await Deleted<InstagramPost>().ToListAsync(ct))
            r.Add(new(TrashKinds.Instagram, i.Id, i.LinkUrl ?? $"Instagram #{i.Id}", i.DeletedAtUtc!.Value));
        foreach (var t in await Deleted<TimeSlot>().Include(x => x.Translations).ToListAsync(ct))
            r.Add(new(TrashKinds.TimeSlot, t.Id, Name(t.Translations, x => x.Label) ?? $"#{t.Id}", t.DeletedAtUtc!.Value));
        return r.OrderByDescending(x => x.DeletedAtUtc).ToList();
    }

    public async Task<int> CountAsync(CancellationToken ct = default) => (await ListAsync(null, ct)).Count;

    private async Task<ISoftDelete?> FindAsync(string kind, int id, CancellationToken ct) => kind switch
    {
        TrashKinds.Service => await Deleted<Service>().FirstOrDefaultAsync(x => x.Id == id, ct),
        TrashKinds.GalleryItem => await Deleted<GalleryItem>().FirstOrDefaultAsync(x => x.Id == id, ct),
        TrashKinds.GalleryCategory => await Deleted<GalleryCategory>().FirstOrDefaultAsync(x => x.Id == id, ct),
        TrashKinds.Review => await Deleted<Review>().FirstOrDefaultAsync(x => x.Id == id, ct),
        TrashKinds.Page => await Deleted<Page>().FirstOrDefaultAsync(x => x.Id == id, ct),
        TrashKinds.Appointment => await Deleted<AppointmentRequest>().FirstOrDefaultAsync(x => x.Id == id, ct),
        TrashKinds.Message => await Deleted<ContactMessage>().FirstOrDefaultAsync(x => x.Id == id, ct),
        TrashKinds.Chat => await Deleted<ChatVisitor>().FirstOrDefaultAsync(x => x.Id == id, ct),
        TrashKinds.Subscriber => await Deleted<NewsletterSubscriber>().FirstOrDefaultAsync(x => x.Id == id, ct),
        TrashKinds.ListItem => await Deleted<ListItem>().FirstOrDefaultAsync(x => x.Id == id, ct),
        TrashKinds.Instagram => await Deleted<InstagramPost>().FirstOrDefaultAsync(x => x.Id == id, ct),
        TrashKinds.TimeSlot => await Deleted<TimeSlot>().FirstOrDefaultAsync(x => x.Id == id, ct),
        _ => null
    };

    public async Task<bool> RestoreAsync(string kind, int id, CancellationToken ct = default)
    {
        var entity = await FindAsync(kind, id, ct);
        if (entity is null) return false;
        entity.DeletedAtUtc = null;
        await db.SaveChangesAsync(ct);
        cache.InvalidateAll();
        return true;
    }

    public async Task<bool> DeleteForeverAsync(string kind, int id, CancellationToken ct = default)
    {
        var entity = await FindAsync(kind, id, ct);
        if (entity is null) return false;

        // Collect images that belong only to this item, delete them after the row is gone.
        var imageIds = new List<int>();
        int? videoId = null;
        switch (entity)
        {
            case Service s:
                videoId = s.VideoId;
                if (s.CoverImageId is int cover) imageIds.Add(cover);
                imageIds.AddRange(await db.ServiceImages.Where(i => i.ServiceId == s.Id).Select(i => i.MediaImageId).ToListAsync(ct));
                break;
            case GalleryItem g:
                imageIds.Add(g.ImageId);
                if (g.AfterImageId is int after) imageIds.Add(after);
                break;
            case InstagramPost p:
                imageIds.Add(p.ImageId);
                break;
            case Page { SystemKey: not null }:
                return false; // Impressum / privacy pages can only be hidden, never removed
        }

        db.Remove(entity);
        await db.SaveChangesAsync(ct);
        foreach (var imageId in imageIds.Distinct())
            if (!await IsImageUsedAsync(imageId, ct))
                await media.DeleteAsync(imageId, ct);
        if (videoId is int v) await videos.DeleteIfUnusedAsync(v, ct);
        cache.InvalidateAll();
        return true;
    }

    private async Task<bool> IsImageUsedAsync(int imageId, CancellationToken ct)
    {
        var asSetting = imageId.ToString();
        return await db.Services.IgnoreQueryFilters().AnyAsync(s => s.CoverImageId == imageId, ct)
               || await db.ServiceImages.AnyAsync(i => i.MediaImageId == imageId, ct)
               || await db.GalleryItems.IgnoreQueryFilters().AnyAsync(g => g.ImageId == imageId || g.AfterImageId == imageId, ct)
               || await db.InstagramPosts.IgnoreQueryFilters().AnyAsync(p => p.ImageId == imageId, ct)
               || await db.SiteSettings.AnyAsync(s => s.Key.StartsWith("images.") || s.Key.StartsWith("brand.") ? s.Value == asSetting : false, ct);
    }

    public async Task<int> PurgeExpiredAsync(CancellationToken ct = default)
    {
        var limit = DateTime.UtcNow.AddDays(-RetentionDays);
        var expired = (await ListAsync(null, ct)).Where(e => e.DeletedAtUtc < limit).ToList();
        foreach (var e in expired) await DeleteForeverAsync(e.Kind, e.Id, ct);
        return expired.Count;
    }
}
