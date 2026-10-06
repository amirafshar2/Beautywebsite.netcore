using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using NUglify;

namespace BeautyByNegin.Web.Infrastructure;

/// <summary>
/// CSS/JS minification without any build tool (works the same on IIS/Plesk, Linux and Docker):
/// "/css/site.min.css" is produced on first request from "/css/site.css" with NUglify and kept in memory
/// (refreshed automatically when the file changes). Views use <see cref="Url"/> which adds a content hash
/// (?v=…) so browsers can cache for a year. In Development the original files are used.
/// </summary>
public sealed class AssetUrls(IWebHostEnvironment env)
{
    private sealed record Entry(DateTime Modified, byte[] Content, string Hash);

    private readonly ConcurrentDictionary<string, Entry> _cache = new();
    private readonly bool _minify = !env.IsDevelopment();

    /// <summary>"/css/site.css" -> "/css/site.min.css?v=abc123" (production).</summary>
    public string Url(string path)
    {
        if (!_minify) return path + "?v=" + Hash(path);
        var min = ToMin(path);
        var entry = Get(min);
        return entry is null ? path : $"{min}?v={entry.Hash}";
    }

    private static string ToMin(string path) => path.EndsWith(".css") ? path[..^4] + ".min.css" : path[..^3] + ".min.js";

    private string Hash(string path)
    {
        var file = env.WebRootFileProvider.GetFileInfo(path);
        return file.Exists ? file.LastModified.ToUnixTimeSeconds().ToString("x") : "0";
    }

    /// <summary>Minified content for "/x/y.min.css|js", or null when the source file doesn't exist.</summary>
    public (byte[] Content, string ContentType)? Serve(string minPath)
    {
        var entry = Get(minPath);
        if (entry is null) return null;
        return (entry.Content, minPath.EndsWith(".css") ? "text/css; charset=utf-8" : "text/javascript; charset=utf-8");
    }

    private Entry? Get(string minPath)
    {
        var isCss = minPath.EndsWith(".min.css");
        var isJs = minPath.EndsWith(".min.js");
        if (!isCss && !isJs) return null;
        var source = isCss ? minPath[..^8] + ".css" : minPath[..^7] + ".js";
        var file = env.WebRootFileProvider.GetFileInfo(source);
        if (!file.Exists || file.PhysicalPath is null) return null;

        if (_cache.TryGetValue(minPath, out var cached) && cached.Modified == file.LastModified.UtcDateTime) return cached;

        var text = File.ReadAllText(file.PhysicalPath);
        var result = isCss ? Uglify.Css(text) : Uglify.Js(text);
        var output = result.HasErrors ? text : result.Code; // never break the site because of minification
        var bytes = Encoding.UTF8.GetBytes(output);
        var hash = Convert.ToHexString(SHA256.HashData(bytes))[..12].ToLowerInvariant();
        var entry = new Entry(file.LastModified.UtcDateTime, bytes, hash);
        _cache[minPath] = entry;
        return entry;
    }
}

public static class AssetMiddleware
{
    /// <summary>Serves "*.min.css" / "*.min.js" that don't exist on disk from the minifier cache.</summary>
    public static IApplicationBuilder UseMinifiedAssets(this IApplicationBuilder app) => app.Use(async (ctx, next) =>
    {
        var path = ctx.Request.Path.Value ?? "";
        if ((path.EndsWith(".min.css") || path.EndsWith(".min.js")) && !path.StartsWith("/lib/"))
        {
            var assets = ctx.RequestServices.GetRequiredService<AssetUrls>();
            if (assets.Serve(path) is { } asset)
            {
                ctx.Response.ContentType = asset.ContentType;
                ctx.Response.Headers.CacheControl = ctx.Request.Query.ContainsKey("v") ? "public, max-age=31536000, immutable" : "public, max-age=3600";
                await ctx.Response.Body.WriteAsync(asset.Content, ctx.RequestAborted);
                return;
            }
        }
        await next();
    });
}
