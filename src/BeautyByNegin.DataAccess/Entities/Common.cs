namespace BeautyByNegin.DataAccess.Entities;

/// <summary>Deleted items are not removed: they go to the Trash and can be restored for 30 days.</summary>
public interface ISoftDelete
{
    DateTime? DeletedAtUtc { get; set; }
}

/// <summary>Items the admin can drag &amp; drop to reorder.</summary>
public interface ISortable
{
    int SortOrder { get; set; }
}

/// <summary>Show/Hide switch: hidden items stay in the panel but are not published.</summary>
public interface IVisibility
{
    bool IsVisible { get; set; }
}

public interface ITimestamped
{
    DateTime CreatedAtUtc { get; set; }
    DateTime UpdatedAtUtc { get; set; }
}

/// <summary>Base class for "content item" tables (services, gallery, pages, …).</summary>
public abstract class ContentEntity : ISoftDelete, ISortable, IVisibility, ITimestamped
{
    public int Id { get; set; }
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAtUtc { get; set; }
}

/// <summary>
/// Base class of every translation row. Each translatable table "X" has a matching "XTranslation"
/// table with one row per language; a missing row means "not translated yet" (falls back to default language).
/// </summary>
public abstract class TranslationBase
{
    public int Id { get; set; }
    public string LanguageCode { get; set; } = "";
}

public interface ITranslatable<TTranslation> where TTranslation : TranslationBase
{
    List<TTranslation> Translations { get; set; }
}
