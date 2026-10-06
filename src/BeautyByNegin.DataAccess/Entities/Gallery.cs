namespace BeautyByNegin.DataAccess.Entities;

public class GalleryCategory : ContentEntity, ITranslatable<GalleryCategoryTranslation>
{
    public List<GalleryCategoryTranslation> Translations { get; set; } = [];
    public List<GalleryItem> Items { get; set; } = [];
}

public class GalleryCategoryTranslation : TranslationBase
{
    public int GalleryCategoryId { get; set; }
    public string Name { get; set; } = "";
}

/// <summary>
/// A gallery photo. When <see cref="AfterImageId"/> is set the item is a before/after pair:
/// <see cref="ImageId"/> is "before" and <see cref="AfterImageId"/> is "after".
/// </summary>
public class GalleryItem : ContentEntity, ITranslatable<GalleryItemTranslation>
{
    public int? CategoryId { get; set; }
    public GalleryCategory? Category { get; set; }

    public int ImageId { get; set; }
    public MediaImage? Image { get; set; }

    public int? AfterImageId { get; set; }
    public MediaImage? AfterImage { get; set; }

    public bool ShowOnHome { get; set; }

    public bool IsBeforeAfter => AfterImageId.HasValue;

    public List<GalleryItemTranslation> Translations { get; set; } = [];
}

public class GalleryItemTranslation : TranslationBase
{
    public int GalleryItemId { get; set; }
    public string? Caption { get; set; }
}
