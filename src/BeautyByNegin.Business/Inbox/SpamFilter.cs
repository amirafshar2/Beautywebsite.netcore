using System.Text.RegularExpressions;

namespace BeautyByNegin.Business.Inbox;

/// <summary>
/// Recognises typical form spam (SEO / "Google index" offers, look-alike domains, link drops).
/// Works with points so a real client is not caught by one harmless detail: a single link alone
/// (e.g. "see my Instagram") is allowed; a link plus sales words, or a domain that imitates the
/// site's own name, is spam. Spam is not deleted: it goes to the panel trash with the reason,
/// without Telegram/e-mail notification, and can be restored there.
/// </summary>
public static partial class SpamFilter
{
    public const string NoteMarker = "🚫 Spam";
    private const int Threshold = 3;

    private static readonly string[] SalesPhrases =
    [
        "seo", "search index", "search engine", "search results", "google's search", "google search console",
        "first page of google", "page 1 of google", "rank higher", "ranking", "backlink", "link building", "indexing",
        "index your", "submit your site", "submit your website", "website traffic", "more traffic", "organic traffic",
        "web design services", "website redesign", "digital marketing", "marketing services", "lead generation",
        "guest post", "sponsored post", "dear website owner", "website owner", "business owner", "unsubscribe",
        "opt out", "boost your", "grow your business", "increase your sales", "virtual assistant", "outsourc",
        "crypto", "bitcoin", "forex", "casino", "betting", "viagra", "cialis", "loan offer", "payday",
        "free trial", "limited offer", "click here", "visit our website", "feature your", "listed on",
    ];

    [GeneratedRegex(@"https?://|www\.", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();

    // bare domains like "indexregister.live" or "mail.search-site.com"
    [GeneratedRegex(@"\b((?:[a-z0-9-]+\.)+(?:com|net|org|info|biz|live|online|site|xyz|top|club|io|co|ru|su|cn|store|shop|click|link|pro|app|website|space|today|world|agency|digital|services|tech|cc|me))\b", RegexOptions.IgnoreCase)]
    private static partial Regex DomainRegex();

    [GeneratedRegex(@"[Ѐ-ӿ一-鿿぀-ヿ가-힯]")]
    private static partial Regex ForeignScriptRegex();

    [GeneratedRegex(@"[^a-z0-9]")]
    private static partial Regex NonAlnum();

    /// <summary>Returns a short reason when the submission is spam, otherwise null.</summary>
    /// <param name="brandName">Site brand ("Beauty by Negin"): domains containing it but not owned by the site are fakes.</param>
    /// <param name="ownHosts">The site's own host(s) and contact e-mail domains (never counted as fakes).</param>
    public static string? Check(string brandName, IEnumerable<string> ownHosts, params string?[] fields)
    {
        var text = string.Join("\n", fields.Where(f => !string.IsNullOrWhiteSpace(f)));
        if (text.Length == 0) return null;
        var lower = text.ToLowerInvariant();
        var score = 0;
        var reasons = new List<string>();

        // 1) look-alike domain: contains the brand ("…beautybynegin…") but is not the site's own
        var slug = NonAlnum().Replace(brandName.ToLowerInvariant(), "");
        var own = ownHosts.Where(h => !string.IsNullOrWhiteSpace(h))
            .Select(h => h.Trim().ToLowerInvariant().Replace("www.", "")).ToHashSet();
        var domains = DomainRegex().Matches(lower).Select(m => m.Value.Replace("www.", "")).Distinct().ToList();
        // the brand's own domain ("beautybynegin.com", ".de" …) is always fine, even when the site URL is not configured
        foreach (var d in domains)
            if (slug.Length >= 5 && d.Contains('.') && NonAlnum().Replace(d[..d.LastIndexOf('.')], "") == slug) own.Add(d);
        if (slug.Length >= 5 && domains.Any(d => NonAlnum().Replace(d, "").Contains(slug) && !own.Contains(d)))
        {
            score += 3; reasons.Add("fake domain");
        }

        // 2) links (domains of the site itself don't count)
        var links = UrlRegex().Matches(lower).Count + domains.Count(d => !own.Contains(d) && !lower.Contains("@" + d));
        if (links > 0) { score += 2; reasons.Add("link"); }
        if (links > 2) score += 1;

        // 3) sales / SEO phrases
        var hits = SalesPhrases.Count(p => p == "seo" ? Regex.IsMatch(lower, @"\bseo\b") : lower.Contains(p));
        if (hits > 0) { score += hits >= 3 ? 3 : 2; reasons.Add("advertising"); }

        // 4) writing systems the site does not use (Cyrillic, Chinese, Japanese, Korean)
        if (ForeignScriptRegex().IsMatch(text)) { score += 2; reasons.Add("foreign script"); }

        return score >= Threshold ? string.Join(", ", reasons) : null;
    }
}
