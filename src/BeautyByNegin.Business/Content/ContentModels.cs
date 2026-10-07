using BeautyByNegin.Business.Media;

namespace BeautyByNegin.Business.Content;

public sealed record ServiceCard(
    int Id,
    int Number,
    string Name,
    string? Subtitle,
    string? ShortDescription,
    string Slug,
    ImageView Image,
    decimal? Price)
{
    /// <summary>"01", "02" … (always Latin digits, part of the brand style "01 — CARBOXY FACIAL").</summary>
    public string NumberLabel => Number.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
}

public sealed record ServiceDetail(
    ServiceCard Card,
    string? DescriptionHtml,
    string? SuitableFor,
    string? ExpectedResult,
    string? Duration,
    IReadOnlyList<ImageView> Gallery,
    /// <summary>Treatment video (null when there is none or it is not ready yet).</summary>
    VideoView? Video,
    string? MetaTitle,
    string? MetaDescription,
    /// <summary>Slug of this service in every language (for the language switcher).</summary>
    IReadOnlyDictionary<string, string> Slugs,
    IReadOnlyList<ServiceCard> Related);

/// <summary>Result of looking up a service address: found, moved (301), or not found.</summary>
public sealed record ServiceLookup(ServiceDetail? Service, string? RedirectSlug);

public sealed record GalleryCategoryView(int Id, string Name);

public sealed record GalleryItemView(int Id, int? CategoryId, ImageView Image, ImageView? AfterImage, string? Caption)
{
    public bool IsBeforeAfter => AfterImage is not null;
}

public sealed record ReviewView(int Id, string Name, int? Rating, string Text, string LanguageCode, DateOnly? Date, string? ServiceName);

public sealed record InstagramView(ImageView Image, string Url);

public sealed record ListItemView(string Text, string? Detail);

public sealed record HomeData(
    IReadOnlyList<string> Sections,
    ImageView Hero,
    ImageView IntroImage,
    ImageView? ConsultationImage,
    IReadOnlyList<ServiceCard> Services,
    IReadOnlyList<GalleryItemView> Gallery,
    IReadOnlyList<ReviewView> Reviews,
    IReadOnlyList<InstagramView> Instagram);

public sealed record AboutData(ImageView Image, IReadOnlyList<ListItemView> Expertise, IReadOnlyList<ListItemView> Certificates);

public sealed record GalleryData(IReadOnlyList<GalleryCategoryView> Categories, IReadOnlyList<GalleryItemView> Items);

public sealed record PageView(int Id, string? SystemKey, string Title, string? ContentHtml, string? MetaTitle, string? MetaDescription,
    IReadOnlyDictionary<string, string> Slugs);

/// <summary>Result of looking up a custom page address in a language.</summary>
public sealed record PageLookup(PageView? Page, string? RedirectSlug);

public sealed record OptionView(int Id, string Label);

public sealed record ContactData(ImageView? MapImage);
