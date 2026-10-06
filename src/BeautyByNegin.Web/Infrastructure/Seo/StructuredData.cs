using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using BeautyByNegin.Business.Content;
using BeautyByNegin.DataAccess;
using Microsoft.AspNetCore.Html;

namespace BeautyByNegin.Web.Infrastructure.Seo;

/// <summary>schema.org JSON-LD (BeautySalon on every page, Service on treatment pages, Review list on the reviews page).</summary>
public static class StructuredData
{
    private static readonly JsonSerializerOptions Json = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // keep Persian readable; "<" is still escaped below
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public static string BaseUrl(PageContext ctx, HttpRequest request)
        => string.IsNullOrWhiteSpace(ctx.Settings.SiteUrl) ? $"{request.Scheme}://{request.Host}{request.PathBase}" : ctx.Settings.SiteUrl;

    public static object Salon(PageContext ctx, string baseUrl)
    {
        var address = ctx.T["contact.address"];
        var dayNames = new Dictionary<DayOfWeek, string>
        {
            [DayOfWeek.Monday] = "Monday", [DayOfWeek.Tuesday] = "Tuesday", [DayOfWeek.Wednesday] = "Wednesday",
            [DayOfWeek.Thursday] = "Thursday", [DayOfWeek.Friday] = "Friday", [DayOfWeek.Saturday] = "Saturday", [DayOfWeek.Sunday] = "Sunday"
        };
        var sameAs = new[] { ctx.Contact.InstagramUrl, ctx.Contact.TelegramUrl }.OfType<string>().ToArray();
        return new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "BeautySalon",
            ["@id"] = baseUrl + "/#salon",
            ["name"] = ctx.Settings.BrandName,
            ["url"] = $"{baseUrl}/{ctx.Code}",
            ["image"] = ctx.Layout.ShareImageUrl is null ? null : baseUrl + ctx.Layout.ShareImageUrl,
            ["logo"] = ctx.Layout.Logo is null ? null : baseUrl + ctx.Layout.Logo.Src,
            ["telephone"] = string.IsNullOrWhiteSpace(ctx.Contact.Phone) ? null : ContactLinks.NormalizePhone(ctx.Contact.Phone),
            ["email"] = string.IsNullOrWhiteSpace(ctx.Contact.Email) ? null : ctx.Contact.Email,
            ["address"] = string.IsNullOrWhiteSpace(address) ? null : new Dictionary<string, object?>
            {
                ["@type"] = "PostalAddress",
                ["streetAddress"] = address.Replace("\n", ", "),
                ["addressLocality"] = string.IsNullOrWhiteSpace(ctx.Settings.City) ? null : ctx.Settings.City,
                ["addressCountry"] = ctx.Settings.Text(SettingKeys.CountryCode, null!)
            },
            ["hasMap"] = ctx.Contact.MapUrl,
            ["sameAs"] = sameAs.Length > 0 ? sameAs : null,
            ["priceRange"] = null,
            ["openingHoursSpecification"] = OpeningHours(ctx, dayNames)
        };
    }

    private static object[]? OpeningHours(PageContext ctx, Dictionary<DayOfWeek, string> names)
    {
        // LayoutData keeps display strings; parse "HH:mm – HH:mm" back (Latin digits) for the schema.
        var list = new List<object>();
        foreach (var h in ctx.Layout.Hours.Where(h => !h.IsClosed && h.Hours is not null))
        {
            var day = h.Day;
            var parts = Business.Localization.Digits.ToLatin(h.Hours!).Split('–', StringSplitOptions.TrimEntries);
            if (parts.Length != 2) continue;
            list.Add(new Dictionary<string, object> { ["@type"] = "OpeningHoursSpecification", ["dayOfWeek"] = names[day], ["opens"] = parts[0], ["closes"] = parts[1] });
        }
        return list.Count > 0 ? list.ToArray() : null;
    }

    public static object Service(PageContext ctx, string baseUrl, ServiceDetail s, string url) => new Dictionary<string, object?>
    {
        ["@context"] = "https://schema.org",
        ["@type"] = "Service",
        ["name"] = s.Card.Name,
        ["description"] = s.Card.ShortDescription,
        ["serviceType"] = "Facial treatment",
        ["url"] = url,
        ["image"] = s.Card.Image.IsPlaceholder ? null : baseUrl + s.Card.Image.Src,
        ["provider"] = new Dictionary<string, object> { ["@id"] = baseUrl + "/#salon" },
        ["areaServed"] = string.IsNullOrWhiteSpace(ctx.Settings.City) ? null : ctx.Settings.City
    };

    public static object Reviews(string baseUrl, IEnumerable<ReviewView> reviews) => new Dictionary<string, object?>
    {
        ["@context"] = "https://schema.org",
        ["@type"] = "ItemList",
        ["itemListElement"] = reviews.Select((r, i) => new Dictionary<string, object?>
        {
            ["@type"] = "ListItem",
            ["position"] = i + 1,
            ["item"] = new Dictionary<string, object?>
            {
                ["@type"] = "Review",
                ["author"] = new Dictionary<string, object> { ["@type"] = "Person", ["name"] = r.Name },
                ["reviewBody"] = r.Text,
                ["inLanguage"] = r.LanguageCode,
                ["datePublished"] = r.Date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["reviewRating"] = r.Rating is int x ? new Dictionary<string, object> { ["@type"] = "Rating", ["ratingValue"] = x, ["bestRating"] = 5 } : null,
                ["itemReviewed"] = new Dictionary<string, object> { ["@id"] = baseUrl + "/#salon" }
            }
        }).ToArray()
    };

    public static IHtmlContent Script(object data)
    {
        var json = JsonSerializer.Serialize(data, Json).Replace("</", "<\\/");
        return new HtmlString($"<script type=\"application/ld+json\">{json}</script>");
    }
}
