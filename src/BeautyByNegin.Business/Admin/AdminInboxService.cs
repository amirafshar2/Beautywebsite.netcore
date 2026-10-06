using System.Globalization;
using System.Text;
using BeautyByNegin.DataAccess;
using BeautyByNegin.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace BeautyByNegin.Business.Admin;

public sealed record DashboardStats(
    int NewAppointments, int AppointmentsLast7Days, int UnreadMessages, int UnreadChats,
    int PendingReviews, int Subscribers, int TrashCount, IReadOnlyList<AppointmentRequest> LatestAppointments);

public sealed record AppointmentFilter(AppointmentStatus? Status, DateOnly? From, DateOnly? To, string? Search, int Page = 1, int PageSize = 30);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize)
{
    public int Pages => Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize));
}

public sealed record ChatThread(ChatVisitor Visitor, string? LastText, int Unread);

public interface IAdminInboxService
{
    Task<DashboardStats> GetDashboardAsync(CancellationToken ct = default);
    Task<int> CountNewAppointmentsAsync(CancellationToken ct = default);
    Task<PagedResult<AppointmentRequest>> GetAppointmentsAsync(AppointmentFilter filter, CancellationToken ct = default);
    Task<AppointmentRequest?> GetAppointmentAsync(int id, CancellationToken ct = default);
    Task<bool> UpdateAppointmentAsync(int id, AppointmentStatus status, string? notes, CancellationToken ct = default);
    Task<byte[]> ExportAppointmentsCsvAsync(AppointmentFilter filter, CancellationToken ct = default);

    Task<PagedResult<ContactMessage>> GetMessagesAsync(int page, CancellationToken ct = default);
    Task<ContactMessage?> GetMessageAsync(int id, bool markRead, CancellationToken ct = default);
    Task<bool> UpdateMessageNotesAsync(int id, string? notes, CancellationToken ct = default);

    Task<IReadOnlyList<ChatThread>> GetChatsAsync(CancellationToken ct = default);
    Task<(ChatVisitor? Visitor, IReadOnlyList<ChatMessage> Messages)> GetConversationAsync(int visitorId, CancellationToken ct = default);
    Task<bool> SetChatBlockedAsync(int visitorId, bool blocked, CancellationToken ct = default);

    Task<IReadOnlyList<NewsletterSubscriber>> GetSubscribersAsync(CancellationToken ct = default);
    Task<byte[]> ExportSubscribersCsvAsync(CancellationToken ct = default);
}

public sealed class AdminInboxService(AppDbContext db) : IAdminInboxService
{
    public async Task<DashboardStats> GetDashboardAsync(CancellationToken ct = default)
    {
        var weekAgo = DateTime.UtcNow.AddDays(-7);
        var trash = 0;
        trash += await db.Services.IgnoreQueryFilters().CountAsync(x => x.DeletedAtUtc != null, ct);
        trash += await db.GalleryItems.IgnoreQueryFilters().CountAsync(x => x.DeletedAtUtc != null, ct);
        trash += await db.Reviews.IgnoreQueryFilters().CountAsync(x => x.DeletedAtUtc != null, ct);
        trash += await db.AppointmentRequests.IgnoreQueryFilters().CountAsync(x => x.DeletedAtUtc != null, ct);
        trash += await db.ContactMessages.IgnoreQueryFilters().CountAsync(x => x.DeletedAtUtc != null, ct);
        return new DashboardStats(
            await db.AppointmentRequests.CountAsync(a => a.Status == AppointmentStatus.New, ct),
            await db.AppointmentRequests.CountAsync(a => a.CreatedAtUtc >= weekAgo, ct),
            await db.ContactMessages.CountAsync(m => !m.IsRead, ct),
            await db.ChatMessages.Where(m => !m.FromAdmin && !m.ReadByAdmin).Select(m => m.VisitorId).Distinct().CountAsync(ct),
            await db.Reviews.CountAsync(r => r.Status == ReviewStatus.Pending, ct),
            await db.NewsletterSubscribers.CountAsync(s => s.IsActive, ct),
            trash,
            await db.AppointmentRequests.AsNoTracking().OrderByDescending(a => a.CreatedAtUtc).Take(5).ToListAsync(ct));
    }

    public Task<int> CountNewAppointmentsAsync(CancellationToken ct = default)
        => db.AppointmentRequests.CountAsync(a => a.Status == AppointmentStatus.New, ct);

    private IQueryable<AppointmentRequest> Filtered(AppointmentFilter f)
    {
        var q = db.AppointmentRequests.AsNoTracking().AsQueryable();
        if (f.Status is AppointmentStatus s) q = q.Where(a => a.Status == s);
        if (f.From is DateOnly from) { var d = from.ToDateTime(TimeOnly.MinValue); q = q.Where(a => a.CreatedAtUtc >= d); }
        if (f.To is DateOnly to) { var d = to.ToDateTime(TimeOnly.MinValue).AddDays(1); q = q.Where(a => a.CreatedAtUtc < d); }
        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var term = f.Search.Trim();
            q = q.Where(a => a.FullName.Contains(term) || a.Phone.Contains(term) || (a.Email != null && a.Email.Contains(term))
                             || (a.ServiceNameSnapshot != null && a.ServiceNameSnapshot.Contains(term)));
        }
        return q.OrderByDescending(a => a.CreatedAtUtc);
    }

    public async Task<PagedResult<AppointmentRequest>> GetAppointmentsAsync(AppointmentFilter filter, CancellationToken ct = default)
    {
        var q = Filtered(filter);
        var page = Math.Max(1, filter.Page);
        return new PagedResult<AppointmentRequest>(
            await q.Skip((page - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync(ct),
            await q.CountAsync(ct), page, filter.PageSize);
    }

    public Task<AppointmentRequest?> GetAppointmentAsync(int id, CancellationToken ct = default)
        => db.AppointmentRequests.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<bool> UpdateAppointmentAsync(int id, AppointmentStatus status, string? notes, CancellationToken ct = default)
    {
        var a = await db.AppointmentRequests.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (a is null) return false;
        a.Status = status;
        a.AdminNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        a.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>CSV with UTF-8 BOM so Excel shows Persian correctly.</summary>
    public async Task<byte[]> ExportAppointmentsCsvAsync(AppointmentFilter filter, CancellationToken ct = default)
    {
        var rows = await Filtered(filter with { Page = 1 }).ToListAsync(ct);
        var sb = new StringBuilder();
        sb.AppendLine("Id;Created (UTC);Name;Phone;Email;Service;Preferred date;Time;Status;Language;Message;Notes");
        foreach (var a in rows)
            sb.AppendLine(string.Join(';', new[]
            {
                a.Id.ToString(CultureInfo.InvariantCulture), a.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                a.FullName, a.Phone, a.Email, a.WantsConsultation ? "Consultation" : a.ServiceNameSnapshot,
                a.PreferredDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), a.TimeSlotSnapshot, a.Status.ToString(),
                a.LanguageCode, a.Message, a.AdminNotes
            }.Select(Csv)));
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static string Csv(string? v)
    {
        v ??= "";
        // Neutralise spreadsheet formulas (CSV injection) and quote.
        if (v.Length > 0 && "=+-@".Contains(v[0])) v = "'" + v;
        return "\"" + v.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\"";
    }

    public async Task<PagedResult<ContactMessage>> GetMessagesAsync(int page, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        var q = db.ContactMessages.AsNoTracking().OrderByDescending(m => m.CreatedAtUtc);
        return new PagedResult<ContactMessage>(await q.Skip((page - 1) * 30).Take(30).ToListAsync(ct), await q.CountAsync(ct), page, 30);
    }

    public async Task<ContactMessage?> GetMessageAsync(int id, bool markRead, CancellationToken ct = default)
    {
        var m = await db.ContactMessages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (m is not null && markRead && !m.IsRead) { m.IsRead = true; await db.SaveChangesAsync(ct); }
        return m;
    }

    public async Task<bool> UpdateMessageNotesAsync(int id, string? notes, CancellationToken ct = default)
    {
        var m = await db.ContactMessages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (m is null) return false;
        m.AdminNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<ChatThread>> GetChatsAsync(CancellationToken ct = default)
    {
        var visitors = await db.ChatVisitors.AsNoTracking().Where(v => v.IsEmailVerified)
            .OrderByDescending(v => v.LastMessageAtUtc ?? v.CreatedAtUtc).Take(200).ToListAsync(ct);
        var ids = visitors.Select(v => v.Id).ToList();
        var unread = await db.ChatMessages.Where(m => ids.Contains(m.VisitorId) && !m.FromAdmin && !m.ReadByAdmin)
            .GroupBy(m => m.VisitorId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        var last = await db.ChatMessages.Where(m => ids.Contains(m.VisitorId))
            .GroupBy(m => m.VisitorId).Select(g => g.OrderByDescending(m => m.Id).Select(m => new { m.VisitorId, m.Text }).First())
            .ToListAsync(ct);
        return visitors.Select(v => new ChatThread(v, last.FirstOrDefault(l => l.VisitorId == v.Id)?.Text, unread.GetValueOrDefault(v.Id))).ToList();
    }

    public async Task<(ChatVisitor? Visitor, IReadOnlyList<ChatMessage> Messages)> GetConversationAsync(int visitorId, CancellationToken ct = default)
    {
        var v = await db.ChatVisitors.AsNoTracking().FirstOrDefaultAsync(x => x.Id == visitorId, ct);
        if (v is null) return (null, []);
        await db.ChatMessages.Where(m => m.VisitorId == visitorId && !m.FromAdmin && !m.ReadByAdmin)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.ReadByAdmin, true), ct);
        var messages = await db.ChatMessages.AsNoTracking().Where(m => m.VisitorId == visitorId).OrderBy(m => m.Id).ToListAsync(ct);
        return (v, messages);
    }

    public async Task<bool> SetChatBlockedAsync(int visitorId, bool blocked, CancellationToken ct = default)
        => await db.ChatVisitors.Where(v => v.Id == visitorId).ExecuteUpdateAsync(s => s.SetProperty(v => v.IsBlocked, blocked), ct) > 0;

    public async Task<IReadOnlyList<NewsletterSubscriber>> GetSubscribersAsync(CancellationToken ct = default)
        => await db.NewsletterSubscribers.AsNoTracking().OrderByDescending(s => s.CreatedAtUtc).ToListAsync(ct);

    public async Task<byte[]> ExportSubscribersCsvAsync(CancellationToken ct = default)
    {
        var sb = new StringBuilder("Email;Language;Subscribed (UTC)\n");
        foreach (var s in await GetSubscribersAsync(ct))
            sb.AppendLine($"{Csv(s.Email)};{Csv(s.LanguageCode)};{Csv(s.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture))}");
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }
}
