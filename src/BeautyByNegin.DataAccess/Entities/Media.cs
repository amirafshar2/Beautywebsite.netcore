namespace BeautyByNegin.DataAccess.Entities;

/// <summary>
/// An uploaded image. Files are removed when the owning item is permanently deleted from the Trash. The original is converted to WebP in several widths and stored as
/// wwwroot/uploads/{StorageKey}-{width}.webp (random key, never the original file name).
/// </summary>
public class MediaImage : ITranslatable<MediaImageTranslation>
{
    public int Id { get; set; }

    /// <summary>Random file name stem, e.g. "2026/10/3f9c1a7b2e".</summary>
    public string StorageKey { get; set; } = "";

    /// <summary>Generated widths, comma separated, e.g. "480,960,1600".</summary>
    public string Widths { get; set; } = "";

    public int Width { get; set; }
    public int Height { get; set; }
    public long SizeBytes { get; set; }
    public string? OriginalFileName { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Alternative text ("short description of the image") per language.</summary>
    public List<MediaImageTranslation> Translations { get; set; } = [];

    public IEnumerable<int> WidthList =>
        Widths.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).OrderBy(w => w);
}

public class MediaImageTranslation : TranslationBase
{
    public int MediaImageId { get; set; }
    public string? AltText { get; set; }
}

public enum VideoStatus
{
    /// <summary>Uploaded, waiting for / during compression.</summary>
    Processing = 0,
    Ready = 1,
    Failed = 2,
}

/// <summary>
/// An uploaded video (e.g. the video of a treatment). The original is compressed in the background to an
/// H.264/AAC MP4 (max. 1920 px on the long side, max. 30 fps, metadata removed) and stored as
/// wwwroot/uploads/videos/{StorageKey}.mp4 with a poster image {StorageKey}.webp.
/// </summary>
public class MediaVideo
{
    public int Id { get; set; }
    public string StorageKey { get; set; } = "";
    public VideoStatus Status { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public double DurationSeconds { get; set; }
    public long SizeBytes { get; set; }
    public long OriginalSizeBytes { get; set; }
    public string? OriginalFileName { get; set; }
    /// <summary>True when the file could not be compressed (no ffmpeg on the server) and is shown as uploaded.</summary>
    public bool NotOptimized { get; set; }
    public bool HasPoster { get; set; }
    /// <summary>Technical reason when processing failed (shown to the admin in plain words, details in the log).</summary>
    public string? Error { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAtUtc { get; set; }
}
