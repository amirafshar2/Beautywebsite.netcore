using BeautyByNegin.Business.Media;
using BeautyByNegin.DataAccess;

namespace BeautyByNegin.Web.Infrastructure;

/// <summary>Physical folders for uploads (wwwroot/uploads) and runtime data (App_Data).</summary>
public sealed class MediaPaths(IWebHostEnvironment env) : IMediaPaths
{
    public string UploadsRoot { get; } = Directory.CreateDirectory(AppPaths.Uploads(env.WebRootPath)).FullName;
    public string AppDataRoot { get; } = Directory.CreateDirectory(AppPaths.AppData(env.ContentRootPath)).FullName;
}
