using BeautyByNegin.Business.Localization;
using BeautyByNegin.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BeautyByNegin.Business.Content;

/// <summary>
/// The editable texts of one language, with automatic fallback to the default language
/// when a text has not been translated yet.
/// </summary>
public sealed class TextBundle(string languageCode, IReadOnlyDictionary<string, string> values)
{
    public string LanguageCode { get; } = languageCode;

    /// <summary>Returns the text for <paramref name="key"/>; never throws, returns "" for unknown keys.</summary>
    public string this[string key] => values.TryGetValue(key, out var v) ? v : "";

    /// <summary>Returns the text with {placeholders} replaced, e.g. Format("wa.service", ("service", "Carboxy Facial")).</summary>
    public string Format(string key, params (string Name, string? Value)[] args)
    {
        var text = this[key];
        foreach (var (name, value) in args)
            text = text.Replace("{" + name + "}", value ?? "", StringComparison.Ordinal);
        return text;
    }

    public bool Has(string key) => values.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v);
}

public interface ITextService
{
    Task<TextBundle> GetAsync(string languageCode, CancellationToken ct = default);
    void Invalidate();
}

public sealed class TextService(IServiceScopeFactory scopeFactory, IMemoryCache cache, ILanguageService languages) : ITextService
{
    private const string CachePrefix = "site-texts:";
    private static CancellationTokenSource _reset = new();

    public async Task<TextBundle> GetAsync(string languageCode, CancellationToken ct = default)
    {
        var key = CachePrefix + languageCode;
        if (cache.TryGetValue(key, out TextBundle? cached) && cached is not null) return cached;

        var fallback = (await languages.GetDefaultAsync(ct)).Code;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rows = await db.SiteTextTranslations.AsNoTracking()
            .Where(t => t.LanguageCode == languageCode || t.LanguageCode == fallback || t.LanguageCode == "en")
            .Select(t => new { t.SiteTextKey, t.LanguageCode, t.Value })
            .ToListAsync(ct);

        var dict = new Dictionary<string, string>(StringComparer.Ordinal);
        // Priority: requested language > default language > English.
        foreach (var lang in new[] { "en", fallback, languageCode })
            foreach (var r in rows.Where(r => r.LanguageCode == lang && !string.IsNullOrWhiteSpace(r.Value)))
                dict[r.SiteTextKey] = r.Value;

        var bundle = new TextBundle(languageCode, dict);
        cache.Set(key, bundle, new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromHours(6))
            .AddExpirationToken(new Microsoft.Extensions.Primitives.CancellationChangeToken(_reset.Token)));
        return bundle;
    }

    /// <summary>Called after any text or language change in the panel.</summary>
    public void Invalidate()
    {
        var old = Interlocked.Exchange(ref _reset, new CancellationTokenSource());
        old.Cancel();
        old.Dispose();
    }
}
