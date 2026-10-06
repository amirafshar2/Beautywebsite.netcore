using System.Linq.Expressions;
using BeautyByNegin.Business.Content;
using BeautyByNegin.DataAccess;
using BeautyByNegin.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace BeautyByNegin.Business.Admin;

/// <summary>Generic panel operations shared by all lists: show/hide, reorder, move to Trash.</summary>
public interface IAdminData
{
    Task<bool> ToggleVisibilityAsync<T>(int id, CancellationToken ct = default) where T : class, IVisibility, ISortable;
    Task ReorderAsync<T>(IReadOnlyList<int> orderedIds, CancellationToken ct = default) where T : class, ISortable;
    Task<bool> MoveToTrashAsync<T>(int id, CancellationToken ct = default) where T : class, ISoftDelete;
    Task<int> NextSortOrderAsync<T>(Expression<Func<T, bool>>? filter = null, CancellationToken ct = default) where T : class, ISortable;
    void Changed();
}

public sealed class AdminData(AppDbContext db, ISiteCache cache) : IAdminData
{
    private static Expression<Func<T, bool>> ById<T>(int id)
    {
        var p = Expression.Parameter(typeof(T), "e");
        return Expression.Lambda<Func<T, bool>>(Expression.Equal(Expression.Property(p, "Id"), Expression.Constant(id)), p);
    }

    public async Task<bool> ToggleVisibilityAsync<T>(int id, CancellationToken ct = default) where T : class, IVisibility, ISortable
    {
        var entity = await db.Set<T>().FirstOrDefaultAsync(ById<T>(id), ct);
        if (entity is null) return false;
        entity.IsVisible = !entity.IsVisible;
        await db.SaveChangesAsync(ct);
        Changed();
        return entity.IsVisible;
    }

    public async Task ReorderAsync<T>(IReadOnlyList<int> orderedIds, CancellationToken ct = default) where T : class, ISortable
    {
        var ids = orderedIds.ToList();
        var param = Expression.Parameter(typeof(T), "e");
        var contains = Expression.Call(Expression.Constant(ids), typeof(List<int>).GetMethod("Contains")!, Expression.Property(param, "Id"));
        var items = await db.Set<T>().Where(Expression.Lambda<Func<T, bool>>(contains, param)).ToListAsync(ct);
        foreach (var item in items)
        {
            var id = (int)typeof(T).GetProperty("Id")!.GetValue(item)!;
            item.SortOrder = ids.IndexOf(id) + 1;
        }
        await db.SaveChangesAsync(ct);
        Changed();
    }

    public async Task<bool> MoveToTrashAsync<T>(int id, CancellationToken ct = default) where T : class, ISoftDelete
    {
        var entity = await db.Set<T>().FirstOrDefaultAsync(ById<T>(id), ct);
        if (entity is null) return false;
        entity.DeletedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        Changed();
        return true;
    }

    public async Task<int> NextSortOrderAsync<T>(Expression<Func<T, bool>>? filter = null, CancellationToken ct = default) where T : class, ISortable
    {
        var q = db.Set<T>().IgnoreQueryFilters().AsQueryable();
        if (filter is not null) q = q.Where(filter);
        return (await q.MaxAsync(x => (int?)x.SortOrder, ct) ?? 0) + 1;
    }

    public void Changed() => cache.InvalidateAll();
}
