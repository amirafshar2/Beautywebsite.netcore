using BeautyByNegin.Business.Media;
using BeautyByNegin.DataAccess.Entities;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Areas.Admin.Controllers;

/// <summary>Video upload for treatments: stores the file, compression runs in the background; the panel asks for the status.</summary>
public class VideosController(IVideoService videos) : AdminController
{
    private const long Limit = VideoService.MaxBytes + 1024 * 1024;

    [HttpPost, RequestSizeLimit(Limit), RequestFormLimits(MultipartBodyLengthLimit = Limit)]
    public async Task<IActionResult> Upload(IFormFile? file)
    {
        if (file is null || file.Length == 0) return Fail("media.empty");
        await using var stream = file.OpenReadStream();
        var result = await videos.UploadAsync(stream, file.FileName, file.Length, HttpContext.RequestAborted);
        if (!result.Ok) return Fail(result.ErrorKey!);
        return Json(Describe(result.Video!));
    }

    [HttpGet]
    public async Task<IActionResult> Status(int id)
    {
        var v = await videos.GetAsync(id, HttpContext.RequestAborted);
        return v is null ? Fail("video.err.gone") : Json(Describe(v));
    }

    private object Describe(MediaVideo v) => new
    {
        ok = true,
        id = v.Id,
        status = v.Status switch { VideoStatus.Ready => "ready", VideoStatus.Failed => "failed", _ => "processing" },
        url = v.Status == VideoStatus.Ready ? VideoUrls.Src(v) : null,
        poster = v.Status == VideoStatus.Ready ? VideoUrls.Poster(v) : null,
        info = VideoInfo(P, v),
        message = v.Status == VideoStatus.Failed ? P["video.err.failed"] : null
    };

    /// <summary>"1920×1080 · 0:42 · 85 MB → 12 MB" (or the waiting text).</summary>
    public static string VideoInfo(PanelContext p, MediaVideo v)
    {
        static string Mb(long b) => (b / 1024d / 1024d).ToString(b < 10 * 1024 * 1024 ? "0.0" : "0", System.Globalization.CultureInfo.InvariantCulture) + " MB";
        if (v.Status == VideoStatus.Processing) return p["video.processing"];
        if (v.Status == VideoStatus.Failed) return p["video.err.failed"];
        var t = TimeSpan.FromSeconds(v.DurationSeconds);
        var size = v.NotOptimized ? Mb(v.SizeBytes) + " · " + p["video.notOptimized"] : $"{Mb(v.OriginalSizeBytes)} → {Mb(v.SizeBytes)}";
        return $"{v.Width}×{v.Height} · {(int)t.TotalMinutes}:{t.Seconds:00} · {size}";
    }
}
