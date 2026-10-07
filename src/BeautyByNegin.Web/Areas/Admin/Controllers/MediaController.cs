using BeautyByNegin.Business.Media;
using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Areas.Admin.Controllers;

/// <summary>Image upload used by every image field in the panel (returns the new image id and a preview URL).</summary>
public class MediaController(IMediaService media) : AdminController
{
    [HttpPost, RequestSizeLimit(25 * 1024 * 1024), RequestFormLimits(MultipartBodyLengthLimit = 25 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile? file, int? x, int? y, int? w, int? h)
    {
        if (file is null || file.Length == 0) return Fail("media.empty");
        CropBox? crop = w is > 0 && h is > 0 ? new CropBox(x ?? 0, y ?? 0, w.Value, h.Value) : null;
        await using var stream = file.OpenReadStream();
        var result = await media.SaveAsync(stream, file.FileName, crop, 1600, Ct);
        if (!result.Ok) return Fail(result.ErrorKey!);
        var img = result.Image!;
        return Json(new { ok = true, id = img.Id, url = MediaUrls.Url(img, img.WidthList.First(w2 => w2 >= Math.Min(480, img.WidthList.Max()))) });
    }
}
