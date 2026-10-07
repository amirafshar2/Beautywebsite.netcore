using System.Security.Cryptography;
using BeautyByNegin.DataAccess;
using BeautyByNegin.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace BeautyByNegin.Business.Media;

/// <summary>Crop rectangle in pixels of the ORIGINAL image (from the cropping tool in the panel).</summary>
public sealed record CropBox(int X, int Y, int Width, int Height);

public sealed record UploadResult(bool Ok, MediaImage? Image = null, string? ErrorKey = null);

public interface IMediaService
{
    Task<UploadResult> SaveAsync(Stream stream, string? fileName, CropBox? crop = null, int maxWidth = 1600, CancellationToken ct = default);
    Task<MediaImage?> GetAsync(int id, CancellationToken ct = default);
    Task SetAltTextAsync(int id, IDictionary<string, string?> altByLanguage, CancellationToken ct = default);
    /// <summary>Removes the database row and all files. Only call for images nothing else uses.</summary>
    Task DeleteAsync(int id, CancellationToken ct = default);
    string UploadsRoot { get; }
}

/// <summary>
/// Image pipeline: validates real image content, fixes phone rotation (EXIF), crops to the chosen box,
/// creates WebP variants (480 / 960 / 1600 px) with random file names in App_Data/uploads/yyyy/MM/ (URL /uploads/…).
/// Uses SixLabors.ImageSharp (pure .NET, runs the same on Windows/IIS, Linux and Docker).
/// </summary>
public sealed class MediaService(AppDbContext db, IMediaPaths paths, ILogger<MediaService> logger) : IMediaService
{
    public const long MaxBytes = 20 * 1024 * 1024;
    public static readonly int[] Widths = [480, 960, 1600];
    private static readonly string[] AllowedFormats = ["JPEG", "PNG", "WEBP", "GIF", "BMP", "TIFF"];

    public string UploadsRoot => paths.UploadsRoot;

    public async Task<UploadResult> SaveAsync(Stream stream, string? fileName, CropBox? crop = null, int maxWidth = 1600, CancellationToken ct = default)
    {
        await using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct);
        if (buffer.Length == 0) return new UploadResult(false, ErrorKey: "media.empty");
        if (buffer.Length > MaxBytes) return new UploadResult(false, ErrorKey: "media.tooLarge");
        buffer.Position = 0;

        Image image;
        try
        {
            // Detect by CONTENT, never by file extension.
            var format = await Image.DetectFormatAsync(buffer, ct);
            if (!AllowedFormats.Contains(format.Name.ToUpperInvariant())) return new UploadResult(false, ErrorKey: "media.notImage");
            buffer.Position = 0;
            image = await Image.LoadAsync(buffer, ct);
        }
        catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException or NotSupportedException)
        {
            return new UploadResult(false, ErrorKey: "media.notImage");
        }

        using (image)
        {
            image.Mutate(x => x.AutoOrient());
            image.Metadata.ExifProfile = null; // strip GPS/camera data
            image.Metadata.XmpProfile = null;

            if (crop is { Width: > 10, Height: > 10 })
            {
                var rect = Rectangle.Intersect(new Rectangle(crop.X, crop.Y, crop.Width, crop.Height), image.Bounds);
                if (rect.Width > 10 && rect.Height > 10) image.Mutate(x => x.Crop(rect));
            }

            var now = DateTime.UtcNow;
            var key = $"{now:yyyy}/{now:MM}/{Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant()}";
            var dir = Path.Combine(paths.UploadsRoot, now.ToString("yyyy"), now.ToString("MM"));
            Directory.CreateDirectory(dir);

            var targets = Widths.Where(w => w <= maxWidth && w < image.Width).ToList();
            targets.Add(Math.Min(image.Width, maxWidth)); // always keep one "full" variant
            targets = targets.Distinct().OrderBy(w => w).ToList();

            var encoder = new WebpEncoder { Quality = 82, FileFormat = WebpFileFormatType.Lossy, Method = WebpEncodingMethod.Level4 };
            long total = 0;
            foreach (var w in targets)
            {
                using var copy = image.Clone(x => x.Resize(new ResizeOptions { Size = new Size(w, 0), Mode = ResizeMode.Max, Sampler = KnownResamplers.Lanczos3 }));
                var path = Path.Combine(paths.UploadsRoot, $"{key}-{w}.webp".Replace('/', Path.DirectorySeparatorChar));
                await copy.SaveAsWebpAsync(path, encoder, ct);
                total += new FileInfo(path).Length;
            }

            var finalWidth = targets.Max();
            var finalHeight = (int)Math.Round(image.Height * (finalWidth / (double)image.Width));
            var entity = new MediaImage
            {
                StorageKey = key,
                Widths = string.Join(',', targets),
                Width = finalWidth,
                Height = finalHeight,
                SizeBytes = total,
                OriginalFileName = fileName is null ? null : Path.GetFileName(fileName).Length > 200 ? Path.GetFileName(fileName)[..200] : Path.GetFileName(fileName)
            };
            db.MediaImages.Add(entity);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Image {Key} saved ({Widths})", key, entity.Widths);
            return new UploadResult(true, entity);
        }
    }

    public Task<MediaImage?> GetAsync(int id, CancellationToken ct = default)
        => db.MediaImages.Include(m => m.Translations).FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task SetAltTextAsync(int id, IDictionary<string, string?> altByLanguage, CancellationToken ct = default)
    {
        var image = await GetAsync(id, ct);
        if (image is null) return;
        foreach (var (lang, alt) in altByLanguage)
        {
            var t = image.Translations.FirstOrDefault(x => x.LanguageCode == lang);
            if (t is null) image.Translations.Add(new MediaImageTranslation { LanguageCode = lang, AltText = alt?.Trim() });
            else t.AltText = alt?.Trim();
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var image = await db.MediaImages.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (image is null) return;
        foreach (var w in image.WidthList)
        {
            var path = Path.Combine(paths.UploadsRoot, $"{image.StorageKey}-{w}.webp".Replace('/', Path.DirectorySeparatorChar));
            try { if (File.Exists(path)) File.Delete(path); }
            catch (IOException ex) { logger.LogWarning(ex, "Could not delete {Path}", path); }
        }
        db.MediaImages.Remove(image);
        await db.SaveChangesAsync(ct);
    }
}

/// <summary>Physical folders, provided by the web layer (App_Data and App_Data/uploads).</summary>
public interface IMediaPaths
{
    string UploadsRoot { get; }
    string AppDataRoot { get; }
}
