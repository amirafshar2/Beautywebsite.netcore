namespace BeautyByNegin.DataAccess.Entities;

/// <summary>
/// Free content pages: Impressum, privacy policy (Datenschutz) and custom pages
/// (e.g. "Aftercare tips"). System pages have a <see cref="SystemKey"/> and cannot be deleted.
/// </summary>
public class Page : ContentEntity, ITranslatable<PageTranslation>
{
    /// <summary>"impressum", "privacy" or null for custom pages.</summary>
    public string? SystemKey { get; set; }
    public bool ShowInFooter { get; set; } = true;
    public bool ShowInMenu { get; set; }

    public List<PageTranslation> Translations { get; set; } = [];
}

public class PageTranslation : TranslationBase
{
    public int PageId { get; set; }
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    /// <summary>Sanitized HTML from the editor.</summary>
    public string? Content { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}

public static class SystemPages
{
    public const string Impressum = "impressum";
    public const string Privacy = "privacy";
}

/// <summary>A block of the home page; the admin can switch blocks on/off and reorder them.</summary>
public class HomeSection : ISortable, IVisibility
{
    public int Id { get; set; }
    /// <summary>One of <see cref="HomeSectionKeys"/>.</summary>
    public string Key { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; } = true;
}

public static class HomeSectionKeys
{
    public const string Hero = "hero";
    public const string Intro = "intro";
    public const string Services = "services";
    public const string Consultation = "consultation";
    public const string Gallery = "gallery";
    public const string Reviews = "reviews";
    public const string Instagram = "instagram";
    public const string Newsletter = "newsletter";
    public const string Contact = "contact";

    public static readonly string[] All = [Hero, Intro, Services, Consultation, Gallery, Reviews, Instagram, Newsletter, Contact];
}

/// <summary>
/// Simple translatable list items: "expertise" (About page specialties) and "certificates".
/// </summary>
public class ListItem : ContentEntity, ITranslatable<ListItemTranslation>
{
    /// <summary>One of <see cref="ListKeys"/>.</summary>
    public string ListKey { get; set; } = "";
    public List<ListItemTranslation> Translations { get; set; } = [];
}

public class ListItemTranslation : TranslationBase
{
    public int ListItemId { get; set; }
    public string Text { get; set; } = "";
    /// <summary>Optional second line (e.g. issuer / year of a certificate).</summary>
    public string? Detail { get; set; }
}

public static class ListKeys
{
    public const string Expertise = "expertise";
    public const string Certificates = "certificates";
}

/// <summary>Photos of the Instagram block (Instagram API is blocked in Iran, so images are uploaded).</summary>
public class InstagramPost : ContentEntity
{
    public int ImageId { get; set; }
    public MediaImage? Image { get; set; }
    /// <summary>Link of the post; empty = link to the Instagram profile.</summary>
    public string? LinkUrl { get; set; }
}

/// <summary>Opening hours, one row per weekday.</summary>
public class OpeningHour
{
    public int Id { get; set; }
    public DayOfWeek Day { get; set; }
    public int SortOrder { get; set; }
    public bool IsClosed { get; set; }
    public TimeOnly? Opens { get; set; }
    public TimeOnly? Closes { get; set; }
}
