using BeautyByNegin.Business.Media;
using BeautyByNegin.DataAccess;

namespace BeautyByNegin.Web.Infrastructure;

/// <summary>Physical folders for runtime data (App_Data) and uploads (App_Data/uploads, served as /uploads/).</summary>
public sealed class MediaPaths(IWebHostEnvironment env) : IMediaPaths
{
    public string UploadsRoot { get; } = Directory.CreateDirectory(AppPaths.Uploads(env.ContentRootPath)).FullName;
    public string AppDataRoot { get; } = Directory.CreateDirectory(AppPaths.AppData(env.ContentRootPath)).FullName;

    /// <summary>
    /// One-time move of uploads from the old place (wwwroot/uploads) to App_Data/uploads.
    /// Existing files are never overwritten; the old folder is removed when it is empty.
    /// </summary>
    public static void MoveLegacyUploads(IWebHostEnvironment env, ILogger logger)
    {
        var legacy = AppPaths.LegacyUploads(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"));
        var target = Directory.CreateDirectory(AppPaths.Uploads(env.ContentRootPath)).FullName;
        try
        {
            var info = new DirectoryInfo(legacy);
            if (!info.Exists || info.LinkTarget is not null) return; // nothing there, or a link (old Docker image)
            var moved = 0;
            foreach (var file in info.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                var dest = Path.Combine(target, Path.GetRelativePath(legacy, file.FullName));
                if (File.Exists(dest)) continue;
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                file.MoveTo(dest);
                moved++;
            }
            if (!info.EnumerateFiles("*", SearchOption.AllDirectories).Any()) info.Delete(recursive: true);
            if (moved > 0) logger.LogInformation("{Count} uploaded files moved from wwwroot/uploads to App_Data/uploads", moved);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Old uploads folder could not be moved; please move wwwroot/uploads into App_Data/uploads by hand");
        }
    }
}
