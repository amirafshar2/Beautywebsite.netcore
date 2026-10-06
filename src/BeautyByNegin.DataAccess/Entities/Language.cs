namespace BeautyByNegin.DataAccess.Entities;

/// <summary>
/// A content language of the public site (fa, tr, de, en).
/// The admin can switch languages on/off, reorder them and pick the default one.
/// </summary>
public class Language
{
    /// <summary>Two-letter code used in URLs, e.g. "fa". Primary key.</summary>
    public string Code { get; set; } = "";

    /// <summary>Full .NET culture name used for formatting, e.g. "fa-IR".</summary>
    public string CultureName { get; set; } = "";

    /// <summary>Name of the language written in that language, e.g. "فارسی".</summary>
    public string NativeName { get; set; } = "";

    /// <summary>Short label for the language switcher, e.g. "FA".</summary>
    public string ShortLabel { get; set; } = "";

    public bool IsRtl { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }

    /// <summary>Show numbers with Persian digits (۱۲۳) instead of Latin digits.</summary>
    public bool UseNativeDigits { get; set; }
}
