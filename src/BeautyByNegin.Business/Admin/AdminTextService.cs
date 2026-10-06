using BeautyByNegin.DataAccess;
using BeautyByNegin.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace BeautyByNegin.Business.Admin;

public interface IAdminTextService
{
    Task<IReadOnlyList<SiteText>> GetTextsAsync(string? group = null, string? search = null, CancellationToken ct = default);
    Task<IReadOnlyList<SiteText>> GetTextsByKeysAsync(IEnumerable<string> keys, CancellationToken ct = default);
    Task<IReadOnlyList<(string Group, int Count, int Missing)>> GetGroupsAsync(IEnumerable<string> languages, CancellationToken ct = default);
    /// <summary>Saves values: key -> (language -> value). Rich texts are sanitized.</summary>
    Task SaveTextsAsync(IDictionary<string, Dictionary<string, string?>> values, CancellationToken ct = default);

    Task<IReadOnlyList<Language>> GetLanguagesAsync(CancellationToken ct = default);
    Task<string?> SaveLanguagesAsync(IReadOnlyList<(string Code, bool Enabled, bool NativeDigits)> languages, string defaultCode, CancellationToken ct = default);
}

public sealed class AdminTextService(AppDbContext db, IAdminData data) : IAdminTextService
{
    public async Task<IReadOnlyList<SiteText>> GetTextsAsync(string? group = null, string? search = null, CancellationToken ct = default)
    {
        var q = db.SiteTexts.AsNoTracking().Include(t => t.Translations).AsQueryable();
        if (!string.IsNullOrWhiteSpace(group)) q = q.Where(t => t.Group == group);
        var list = await q.OrderBy(t => t.SortOrder).ToListAsync(ct);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            list = list.Where(t => t.Key.Contains(s, StringComparison.OrdinalIgnoreCase)
                                   || (t.Hint ?? "").Contains(s, StringComparison.OrdinalIgnoreCase)
                                   || t.Translations.Any(x => x.Value.Contains(s, StringComparison.OrdinalIgnoreCase))).ToList();
        }
        return list;
    }

    public async Task<IReadOnlyList<SiteText>> GetTextsByKeysAsync(IEnumerable<string> keys, CancellationToken ct = default)
    {
        var k = keys.ToList();
        var list = await db.SiteTexts.AsNoTracking().Include(t => t.Translations).Where(t => k.Contains(t.Key)).ToListAsync(ct);
        return list.OrderBy(t => k.IndexOf(t.Key)).ToList();
    }

    public async Task<IReadOnlyList<(string Group, int Count, int Missing)>> GetGroupsAsync(IEnumerable<string> languages, CancellationToken ct = default)
    {
        var langs = languages.ToList();
        var texts = await db.SiteTexts.AsNoTracking().Include(t => t.Translations).OrderBy(t => t.SortOrder).ToListAsync(ct);
        return texts.GroupBy(t => t.Group)
            .Select(g => (g.Key, g.Count(), g.Count(t => langs.Any(l => string.IsNullOrWhiteSpace(t.Translations.FirstOrDefault(x => x.LanguageCode == l)?.Value)))))
            .ToList();
    }

    public async Task SaveTextsAsync(IDictionary<string, Dictionary<string, string?>> values, CancellationToken ct = default)
    {
        var keys = values.Keys.ToList();
        var texts = await db.SiteTexts.Include(t => t.Translations).Where(t => keys.Contains(t.Key)).ToListAsync(ct);
        foreach (var text in texts)
        {
            foreach (var (lang, raw) in values[text.Key])
            {
                var value = text.Kind == TextKind.Html ? RichText.Clean(raw) ?? "" : (raw ?? "").Trim();
                var row = text.Translations.FirstOrDefault(t => t.LanguageCode == lang);
                if (row is null)
                {
                    if (value.Length == 0) continue;
                    text.Translations.Add(new SiteTextTranslation { SiteTextKey = text.Key, LanguageCode = lang, Value = value, IsCustomized = true });
                }
                else if (row.Value != value)
                {
                    row.Value = value;
                    row.IsCustomized = true;
                }
            }
        }
        await db.SaveChangesAsync(ct);
        data.Changed();
    }

    public async Task<IReadOnlyList<Language>> GetLanguagesAsync(CancellationToken ct = default)
        => await db.Languages.AsNoTracking().OrderBy(l => l.SortOrder).ToListAsync(ct);

    /// <summary>Order = list order. Returns a panel error key, or null on success.</summary>
    public async Task<string?> SaveLanguagesAsync(IReadOnlyList<(string Code, bool Enabled, bool NativeDigits)> languages, string defaultCode, CancellationToken ct = default)
    {
        if (!languages.Any(l => l.Code == defaultCode && l.Enabled)) return "err.defaultMustBeEnabled";
        var rows = await db.Languages.ToDictionaryAsync(l => l.Code, ct);
        for (var i = 0; i < languages.Count; i++)
        {
            if (!rows.TryGetValue(languages[i].Code, out var row)) continue;
            row.IsEnabled = languages[i].Enabled;
            row.UseNativeDigits = languages[i].NativeDigits;
            row.SortOrder = i + 1;
            row.IsDefault = row.Code == defaultCode;
        }
        await db.SaveChangesAsync(ct);
        data.Changed();
        return null;
    }
}
