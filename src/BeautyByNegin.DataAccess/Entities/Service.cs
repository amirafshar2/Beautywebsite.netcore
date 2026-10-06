namespace BeautyByNegin.DataAccess.Entities;

/// <summary>A treatment (e.g. Carboxy Facial).</summary>
public class Service : ContentEntity, ITranslatable<ServiceTranslation>
{
    public bool ShowOnHome { get; set; }

    /// <summary>Optional internal price; only shown when the "show prices" setting is on.</summary>
    public decimal? Price { get; set; }

    public int? CoverImageId { get; set; }
    public MediaImage? CoverImage { get; set; }

    public List<ServiceImage> Images { get; set; } = [];
    public List<ServiceTranslation> Translations { get; set; } = [];
}

public class ServiceTranslation : TranslationBase
{
    public int ServiceId { get; set; }

    public string Name { get; set; } = "";
    public string? Subtitle { get; set; }
    /// <summary>Short text on the service card.</summary>
    public string? ShortDescription { get; set; }
    /// <summary>Long description (sanitized HTML from the rich text editor).</summary>
    public string? Description { get; set; }
    public string? SuitableFor { get; set; }
    public string? ExpectedResult { get; set; }
    /// <summary>Estimated duration as free text, e.g. "60 دقیقه" / "ca. 60 Min.".</summary>
    public string? Duration { get; set; }

    /// <summary>"Page address" part, e.g. "carboxy-facial".</summary>
    public string Slug { get; set; } = "";
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}

/// <summary>Additional images of a service (small gallery on the detail page).</summary>
public class ServiceImage : ISortable
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public int MediaImageId { get; set; }
    public MediaImage? MediaImage { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>Old page addresses of services; requests to them are 301-redirected to the current address.</summary>
public class ServiceSlugHistory
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public string LanguageCode { get; set; } = "";
    public string OldSlug { get; set; } = "";
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
}
