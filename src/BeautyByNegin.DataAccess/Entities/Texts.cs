namespace BeautyByNegin.DataAccess.Entities;

public enum TextKind
{
    /// <summary>Single line (button, label, title).</summary>
    Line = 0,
    /// <summary>Multi-line plain text (paragraphs).</summary>
    Multiline = 1,
    /// <summary>Rich text (sanitized HTML from the editor).</summary>
    Html = 2
}

/// <summary>
/// Every editable text of the site that is not part of a content item: interface texts
/// (buttons, menu, form labels, errors) and section texts (hero title, About texts, …).
/// Identified by a stable key like "nav.services" or "home.hero.title".
/// </summary>
public class SiteText
{
    public string Key { get; set; } = "";
    /// <summary>Panel grouping, e.g. "nav", "buttons", "booking", "home", "about".</summary>
    public string Group { get; set; } = "";
    public TextKind Kind { get; set; }
    public int SortOrder { get; set; }
    /// <summary>Plain-language explanation for the admin: where does this text appear?</summary>
    public string? Hint { get; set; }

    public List<SiteTextTranslation> Translations { get; set; } = [];
}

public class SiteTextTranslation
{
    public int Id { get; set; }
    public string SiteTextKey { get; set; } = "";
    public string LanguageCode { get; set; } = "";
    public string Value { get; set; } = "";
    /// <summary>False for seed values; true once the admin edited it (seed never overwrites edited values).</summary>
    public bool IsCustomized { get; set; }
}

/// <summary>Non-translatable settings as key/value pairs (typed access via SiteSettings).</summary>
public class SiteSetting
{
    public string Key { get; set; } = "";
    public string? Value { get; set; }
}
