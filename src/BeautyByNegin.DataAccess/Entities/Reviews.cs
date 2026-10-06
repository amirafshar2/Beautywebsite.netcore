namespace BeautyByNegin.DataAccess.Entities;

public enum ReviewSource
{
    InPerson = 0,
    Instagram = 1,
    Google = 2,
    Website = 3
}

public enum ReviewStatus
{
    /// <summary>Sent by a visitor through the site, waiting for approval.</summary>
    Pending = 0,
    Approved = 1
}

/// <summary>
/// A customer review. Reviews are written in one language (not translated);
/// visitors see reviews in their own language first.
/// </summary>
public class Review : ContentEntity
{
    public string AuthorName { get; set; } = "";
    /// <summary>Show only initials, e.g. "S. M.".</summary>
    public bool ShowInitialsOnly { get; set; }
    /// <summary>1–5 stars, optional.</summary>
    public int? Rating { get; set; }
    public string Text { get; set; } = "";
    public string LanguageCode { get; set; } = "";
    public DateOnly? ReviewDate { get; set; }

    public int? ServiceId { get; set; }
    public Service? Service { get; set; }

    public ReviewSource Source { get; set; } = ReviewSource.InPerson;
    public ReviewStatus Status { get; set; } = ReviewStatus.Approved;

    public string DisplayName
    {
        get
        {
            if (!ShowInitialsOnly) return AuthorName;
            var parts = AuthorName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", parts.Select(p => p[..1].ToUpperInvariant() + "."));
        }
    }
}
