using BeautyByNegin.DataAccess.Entities;

namespace BeautyByNegin.Business.Media;

/// <summary>
/// Image data needed by views: URLs of the WebP variants and the alt text. Missing images render
/// as brand-coloured placeholders (wwwroot/img/placeholder-*.svg).
/// </summary>
public sealed record ImageView(string Src, string? SrcSet, int Width, int Height, string Alt, bool IsPlaceholder)
{
    public static ImageView Placeholder(string ratio, string alt = "") => new($"/img/placeholder-{ratio}.svg", null,
        ratio switch { "16x9" => 1600, "1x1" => 1000, _ => 800 },
        ratio switch { "16x9" => 900, "1x1" => 1000, _ => 1000 },
        alt, true);
}

public static class MediaUrls
{
    public const string UploadsBase = "/uploads/";

    public static string Url(MediaImage image, int width) => $"{UploadsBase}{image.StorageKey}-{width}.webp";

    /// <summary>Converts a stored image into view data; picks the variant closest to <paramref name="preferredWidth"/>.</summary>
    public static ImageView? ToView(MediaImage? image, string languageCode, string defaultLanguage, int preferredWidth = 960)
    {
        if (image is null) return null;
        var widths = image.WidthList.ToList();
        if (widths.Count == 0) return null;

        var src = widths.Where(w => w >= preferredWidth).DefaultIfEmpty(widths.Max()).Min();
        var srcSet = string.Join(", ", widths.Select(w => $"{Url(image, w)} {w}w"));
        var alt = image.Translations.FirstOrDefault(t => t.LanguageCode == languageCode)?.AltText
                  ?? image.Translations.FirstOrDefault(t => t.LanguageCode == defaultLanguage)?.AltText
                  ?? "";
        return new ImageView(Url(image, src), srcSet, image.Width, image.Height, alt, false);
    }
}
