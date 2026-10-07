using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using BeautyByNegin.DataAccess;
using BeautyByNegin.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;

namespace BeautyByNegin.Business.Media;

public sealed record VideoUploadResult(bool Ok, MediaVideo? Video = null, string? ErrorKey = null);

/// <summary>Video data for views (only ready videos).</summary>
public sealed record VideoView(string Src, string? Poster, int Width, int Height);

public static class VideoUrls
{
    public static string Src(MediaVideo v) => $"{MediaUrls.UploadsBase}{v.StorageKey}.mp4";
    public static string? Poster(MediaVideo v) => v.HasPoster ? $"{MediaUrls.UploadsBase}{v.StorageKey}.webp" : null;
    public static VideoView? ToView(MediaVideo? v)
        => v is { Status: VideoStatus.Ready } ? new VideoView(Src(v), Poster(v), v.Width, v.Height) : null;
}

public interface IVideoService
{
    /// <summary>Stores the upload and queues it for compression. Returns immediately (status "Processing").</summary>
    Task<VideoUploadResult> UploadAsync(Stream stream, string? fileName, long length, CancellationToken ct = default);
    Task<MediaVideo?> GetAsync(int id, CancellationToken ct = default);
    /// <summary>Deletes the video (row and files) when no service uses it any more.</summary>
    Task DeleteIfUnusedAsync(int id, CancellationToken ct = default);
    /// <summary>True when ffmpeg was found on the server, i.e. videos are compressed.</summary>
    bool CanOptimize { get; }
}

/// <summary>
/// Treatment videos. The upload is saved to App_Data/video-queue and compressed in the background by
/// <see cref="VideoWorker"/> with ffmpeg: H.264 (CRF 23, preset "slow", "medium" on small servers) + AAC 128 kbit/s in an MP4 with
/// "faststart", at most 1920 px on the long side and 30 fps, all metadata (e.g. GPS of phones) removed.
/// That keeps the picture visually unchanged while files usually get several times smaller.
/// Without ffmpeg an MP4 is published as uploaded (marked "not optimized"); other formats are refused.
/// </summary>
public sealed class VideoService(AppDbContext db, IMediaPaths paths, VideoQueue queue, FfmpegLocator ffmpeg, ILogger<VideoService> logger) : IVideoService
{
    public const long MaxBytes = 500L * 1024 * 1024;
    /// <summary>Largest file published unchanged when ffmpeg is missing.</summary>
    public const long MaxUnoptimizedBytes = 100L * 1024 * 1024;

    public bool CanOptimize => ffmpeg.Ffmpeg is not null;

    public async Task<VideoUploadResult> UploadAsync(Stream stream, string? fileName, long length, CancellationToken ct = default)
    {
        if (length <= 0) return new VideoUploadResult(false, ErrorKey: "media.empty");
        if (length > MaxBytes) return new VideoUploadResult(false, ErrorKey: "video.err.tooLarge");

        var now = DateTime.UtcNow;
        var key = $"videos/{now:yyyy}/{now:MM}/{Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant()}";
        var source = VideoQueue.SourcePath(paths, key);
        Directory.CreateDirectory(Path.GetDirectoryName(source)!);

        await using (var file = File.Create(source))
            await stream.CopyToAsync(file, ct);

        // Check the content (not the file name): MP4/MOV/M4V, WebM/MKV or AVI.
        var kind = await DetectAsync(source, ct);
        if (kind is null)
        {
            TryDelete(source);
            return new VideoUploadResult(false, ErrorKey: "video.err.format");
        }
        if (!CanOptimize && (kind != "mp4" || length > MaxUnoptimizedBytes))
        {
            TryDelete(source);
            return new VideoUploadResult(false, ErrorKey: kind != "mp4" ? "video.err.noFfmpegFormat" : "video.err.noFfmpegSize");
        }

        var name = fileName is null ? null : Path.GetFileName(fileName);
        var video = new MediaVideo
        {
            StorageKey = key,
            Status = VideoStatus.Processing,
            OriginalSizeBytes = length,
            OriginalFileName = name is { Length: > 200 } ? name[..200] : name
        };
        db.MediaVideos.Add(video);
        await db.SaveChangesAsync(ct);
        queue.Enqueue(video.Id);
        logger.LogInformation("Video {Id} uploaded ({Size} bytes), queued", video.Id, length);
        return new VideoUploadResult(true, video);
    }

    public Task<MediaVideo?> GetAsync(int id, CancellationToken ct = default)
        => db.MediaVideos.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task DeleteIfUnusedAsync(int id, CancellationToken ct = default)
    {
        if (await db.Services.IgnoreQueryFilters().AnyAsync(s => s.VideoId == id, ct)) return;
        var video = await db.MediaVideos.FirstOrDefaultAsync(v => v.Id == id, ct);
        if (video is null) return;
        VideoQueue.DeleteFiles(paths, video.StorageKey);
        db.MediaVideos.Remove(video);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Video {Id} deleted", id);
    }

    private static async Task<string?> DetectAsync(string path, CancellationToken ct)
    {
        var head = new byte[16];
        await using var f = File.OpenRead(path);
        var read = await f.ReadAsync(head, ct);
        if (read < 12) return null;
        if (head[4] == 'f' && head[5] == 't' && head[6] == 'y' && head[7] == 'p') return "mp4";       // MP4, MOV, M4V, 3GP
        if (head[0] == 0x1A && head[1] == 0x45 && head[2] == 0xDF && head[3] == 0xA3) return "mkv";     // WebM, MKV
        if (head[0] == 'R' && head[1] == 'I' && head[2] == 'F' && head[3] == 'F' && head[8] == 'A' && head[9] == 'V' && head[10] == 'I') return "avi";
        return null;
    }

    internal static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}

/// <summary>Queue of video ids waiting for compression (one at a time, so the server stays responsive).</summary>
public sealed class VideoQueue
{
    private readonly Channel<int> channel = Channel.CreateUnbounded<int>();
    public void Enqueue(int id) => channel.Writer.TryWrite(id);
    public ChannelReader<int> Reader => channel.Reader;

    public static string SourcePath(IMediaPaths paths, string key)
        => Path.Combine(paths.AppDataRoot, "video-queue", key.Replace('/', '_') + ".src");
    public static string OutputPath(IMediaPaths paths, string key, string ext)
        => Path.Combine(paths.UploadsRoot, (key + ext).Replace('/', Path.DirectorySeparatorChar));

    public static void DeleteFiles(IMediaPaths paths, string key)
    {
        VideoService.TryDelete(SourcePath(paths, key));
        VideoService.TryDelete(OutputPath(paths, key, ".mp4"));
        VideoService.TryDelete(OutputPath(paths, key, ".webp"));
    }
}

/// <summary>
/// Finds ffmpeg/ffprobe: setting "Site:FfmpegPath" (file or folder), App_Data/tools, the app folder's "tools",
/// or the system PATH (Docker image / Linux: "apt install ffmpeg").
/// </summary>
public sealed class FfmpegLocator(IConfiguration config, IMediaPaths paths, IHostEnvironment env)
{
    private (string? Ffmpeg, string? Ffprobe)? found;

    public string? Ffmpeg => (found ??= Find()).Ffmpeg;
    public string? Ffprobe => (found ??= Find()).Ffprobe;

    private (string?, string?) Find()
    {
        var exe = OperatingSystem.IsWindows() ? ".exe" : "";
        var folders = new List<string>();
        var configured = config["Site:FfmpegPath"];
        if (!string.IsNullOrWhiteSpace(configured))
            folders.Add(File.Exists(configured) ? Path.GetDirectoryName(Path.GetFullPath(configured))! : configured);
        folders.Add(Path.Combine(paths.AppDataRoot, "tools"));
        folders.Add(Path.Combine(env.ContentRootPath, "tools"));
        folders.AddRange((Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries));

        string? Locate(string name) => folders.Select(f => Path.Combine(f, name + exe)).FirstOrDefault(File.Exists);
        return (Locate("ffmpeg"), Locate("ffprobe"));
    }
}

/// <summary>Compresses queued videos in the background (see <see cref="VideoService"/>).</summary>
public sealed class VideoWorker(IServiceScopeFactory scopes, VideoQueue queue, FfmpegLocator ffmpeg, IMediaPaths paths, ILogger<VideoWorker> logger) : BackgroundService
{
    private static readonly TimeSpan MaxEncodeTime = TimeSpan.FromMinutes(45);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Videos that were still waiting when the site stopped, and leftovers that were never used.
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            foreach (var id in await db.MediaVideos.Where(v => v.Status == VideoStatus.Processing).Select(v => v.Id).ToListAsync(stoppingToken))
                queue.Enqueue(id);
            await CleanupAsync(db, stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException) { logger.LogWarning(ex, "Video queue start failed"); }

        await foreach (var id in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try { await ProcessAsync(id, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex) { logger.LogError(ex, "Video {Id} could not be processed", id); }
        }
    }

    /// <summary>Uploaded but never attached to a service (e.g. the form was not saved) – removed after a day.</summary>
    private async Task CleanupAsync(AppDbContext db, CancellationToken ct)
    {
        var limit = DateTime.UtcNow.AddDays(-1);
        var unused = await db.MediaVideos
            .Where(v => v.CreatedAtUtc < limit && !db.Services.IgnoreQueryFilters().Any(s => s.VideoId == v.Id))
            .ToListAsync(ct);
        foreach (var v in unused) VideoQueue.DeleteFiles(paths, v.StorageKey);
        db.MediaVideos.RemoveRange(unused);
        if (unused.Count > 0) { await db.SaveChangesAsync(ct); logger.LogInformation("{Count} unused videos removed", unused.Count); }
    }

    private async Task ProcessAsync(int id, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var video = await db.MediaVideos.FirstOrDefaultAsync(v => v.Id == id, ct);
        if (video is null || video.Status != VideoStatus.Processing) return;

        var source = VideoQueue.SourcePath(paths, video.StorageKey);
        var output = VideoQueue.OutputPath(paths, video.StorageKey, ".mp4");
        var poster = VideoQueue.OutputPath(paths, video.StorageKey, ".webp");
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        if (!File.Exists(source)) { Fail(video, "source file missing"); await db.SaveChangesAsync(ct); return; }

        var sw = Stopwatch.StartNew();
        if (ffmpeg.Ffmpeg is null)
        {
            // No ffmpeg on this server: publish the (already checked) MP4 unchanged.
            File.Move(source, output, overwrite: true);
            video.NotOptimized = true;
        }
        else
        {
            var temp = output + ".tmp.mp4";
            // Small servers: a faster preset and fewer threads keep memory and waiting time reasonable.
            var cores = Environment.ProcessorCount;
            var threads = Math.Clamp(cores - 1, 1, 8).ToString(CultureInfo.InvariantCulture);
            var preset = cores >= 4 ? "slow" : "medium";
            var (code, log) = await RunAsync(ffmpeg.Ffmpeg,
            [
                "-hide_banner", "-nostdin", "-y", "-i", source,
                "-map", "0:v:0", "-map", "0:a:0?",
                "-vf", "scale=w='min(iw,1920)':h='min(ih,1920)':force_original_aspect_ratio=decrease:force_divisible_by=2",
                "-fpsmax", "30",
                "-c:v", "libx264", "-preset", preset, "-crf", "23", "-profile:v", "high", "-pix_fmt", "yuv420p",
                "-c:a", "aac", "-b:a", "128k", "-ac", "2",
                "-map_metadata", "-1", "-map_chapters", "-1",
                "-movflags", "+faststart", "-threads", threads,
                temp
            ], MaxEncodeTime, ct);
            if (code != 0 || !File.Exists(temp) || new FileInfo(temp).Length == 0)
            {
                VideoService.TryDelete(temp);
                logger.LogWarning("ffmpeg failed for video {Id} (exit {Code}): {Log}", id, code, Tail(log));
                Fail(video, "ffmpeg exit " + code + ": " + Tail(log, 300));
                VideoService.TryDelete(source);
                await db.SaveChangesAsync(ct);
                return;
            }
            File.Move(temp, output, overwrite: true);
            VideoService.TryDelete(source);
        }

        // Size, length and a poster image (first second of the video, as WebP).
        var info = await ProbeAsync(output, ct);
        video.Width = info.Width;
        video.Height = info.Height;
        video.DurationSeconds = info.Duration;
        video.SizeBytes = new FileInfo(output).Length;
        video.HasPoster = ffmpeg.Ffmpeg is not null && await PosterAsync(output, poster, info.Duration, ct);
        video.Status = VideoStatus.Ready;
        video.Error = null;
        video.ProcessedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        scope.ServiceProvider.GetRequiredService<BeautyByNegin.Business.Content.ISiteCache>().InvalidateAll();
        logger.LogInformation("Video {Id} ready: {W}x{H}, {Dur:0.0}s, {Orig} → {Size} bytes in {Time:0.0}s{Note}",
            id, video.Width, video.Height, video.DurationSeconds, video.OriginalSizeBytes, video.SizeBytes, sw.Elapsed.TotalSeconds,
            video.NotOptimized ? " (not optimized, ffmpeg missing)" : "");
    }

    private static void Fail(MediaVideo video, string error)
    {
        video.Status = VideoStatus.Failed;
        video.Error = error.Length > 500 ? error[..500] : error;
        video.ProcessedAtUtc = DateTime.UtcNow;
    }

    private async Task<bool> PosterAsync(string video, string poster, double duration, CancellationToken ct)
    {
        var jpg = poster + ".tmp.jpg";
        var at = Math.Min(1.0, duration * 0.1).ToString("0.###", CultureInfo.InvariantCulture);
        var (code, _) = await RunAsync(ffmpeg.Ffmpeg!,
            ["-hide_banner", "-nostdin", "-y", "-ss", at, "-i", video, "-frames:v", "1", "-vf", "scale=w='min(iw,1280)':h=-2", "-q:v", "3", jpg],
            TimeSpan.FromMinutes(2), ct);
        try
        {
            if (code != 0 || !File.Exists(jpg)) return false;
            using var image = await Image.LoadAsync(jpg, ct);
            await image.SaveAsWebpAsync(poster, new WebpEncoder { Quality = 80, FileFormat = WebpFileFormatType.Lossy }, ct);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Poster for {Video} could not be created", video);
            return false;
        }
        finally { VideoService.TryDelete(jpg); }
    }

    private async Task<(int Width, int Height, double Duration)> ProbeAsync(string file, CancellationToken ct)
    {
        if (ffmpeg.Ffprobe is not null)
        {
            var (code, json) = await RunAsync(ffmpeg.Ffprobe,
                ["-v", "error", "-select_streams", "v:0", "-show_entries", "stream=width,height:format=duration", "-of", "json", file],
                TimeSpan.FromMinutes(1), ct, stdout: true);
            if (code == 0)
            {
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    var stream = doc.RootElement.GetProperty("streams")[0];
                    var duration = doc.RootElement.TryGetProperty("format", out var f) && f.TryGetProperty("duration", out var d)
                        && double.TryParse(d.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var dur) ? dur : 0;
                    return (stream.GetProperty("width").GetInt32(), stream.GetProperty("height").GetInt32(), duration);
                }
                catch (Exception ex) when (ex is JsonException or KeyNotFoundException or IndexOutOfRangeException or InvalidOperationException) { }
            }
        }
        if (ffmpeg.Ffmpeg is not null)
        {
            // Fallback: read "Duration: 00:00:12.34" and "1920x1080" from ffmpeg's own output.
            var (_, log) = await RunAsync(ffmpeg.Ffmpeg, ["-hide_banner", "-nostdin", "-i", file], TimeSpan.FromMinutes(1), ct);
            var size = Regex.Match(log, @"Video:.*?,\s(\d{2,5})x(\d{2,5})");
            var time = Regex.Match(log, @"Duration:\s(\d+):(\d+):(\d+(?:\.\d+)?)");
            var seconds = time.Success
                ? int.Parse(time.Groups[1].Value, CultureInfo.InvariantCulture) * 3600 + int.Parse(time.Groups[2].Value, CultureInfo.InvariantCulture) * 60
                  + double.Parse(time.Groups[3].Value, CultureInfo.InvariantCulture)
                : 0;
            if (size.Success) return (int.Parse(size.Groups[1].Value, CultureInfo.InvariantCulture), int.Parse(size.Groups[2].Value, CultureInfo.InvariantCulture), seconds);
        }
        return (16, 9, 0); // unknown: the player keeps a 16:9 box
    }

    private static string Tail(string log, int max = 1500) => log.Length > max ? log[^max..] : log;

    /// <summary>Runs a tool with arguments passed as a list (never through a shell) and a time limit.</summary>
    private static async Task<(int Code, string Output)> RunAsync(string exe, IEnumerable<string> args, TimeSpan timeout, CancellationToken ct, bool stdout = false)
    {
        var psi = new ProcessStartInfo(exe) { RedirectStandardError = true, RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = new Process { StartInfo = psi };
        p.Start();
        try { p.PriorityClass = ProcessPriorityClass.BelowNormal; } catch (Exception) { /* not allowed on some hosts */ }
        var outTask = p.StandardOutput.ReadToEndAsync(ct);
        var errTask = p.StandardError.ReadToEndAsync(ct);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);
        try { await p.WaitForExitAsync(cts.Token); }
        catch (OperationCanceledException)
        {
            try { p.Kill(entireProcessTree: true); } catch (Exception) { }
            if (ct.IsCancellationRequested) throw;
            return (-1, "timeout");
        }
        var o = await outTask; var e = await errTask;
        return (p.ExitCode, stdout ? o : e);
    }
}
