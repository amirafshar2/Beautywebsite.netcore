using BeautyByNegin.Business.Settings;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Net.Http.Headers;

namespace BeautyByNegin.Web.Infrastructure.Security;

/// <summary>
/// Security headers for every response. The site loads nothing from other domains, so the
/// Content-Security-Policy can be strict ('self' only, no inline scripts or styles).
/// HTTPS redirect and HSTS are switched on in the panel (so a site without SSL keeps working).
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    private const string Csp =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self'; " +
        "img-src 'self' data: blob:; " +
        "font-src 'self'; " +
        "connect-src 'self' blob:; " +
        "media-src 'self'; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'; " +
        "upgrade-insecure-requests";

    public async Task InvokeAsync(HttpContext context, ISettingsService settingsService)
    {
        var settings = await settingsService.GetAsync(context.RequestAborted);

        if (settings.Bool(DataAccess.SettingKeys.HttpsRedirect) && !context.Request.IsHttps)
        {
            var url = $"https://{context.Request.Host}{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";
            context.Response.Redirect(url, permanent: true);
            return;
        }

        var h = context.Response.Headers;
        // 'upgrade-insecure-requests' would break a plain-http site, so it is only sent over https.
        h[HeaderNames.ContentSecurityPolicy] = context.Request.IsHttps ? Csp : Csp.Replace("; upgrade-insecure-requests", "");
        h[HeaderNames.XContentTypeOptions] = "nosniff";
        h[HeaderNames.XFrameOptions] = "DENY";
        h["Referrer-Policy"] = "strict-origin-when-cross-origin";
        h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=(), interest-cohort=()";
        h["Cross-Origin-Opener-Policy"] = "same-origin";
        if (context.Request.IsHttps && settings.Bool(DataAccess.SettingKeys.Hsts))
            h[HeaderNames.StrictTransportSecurity] = "max-age=31536000; includeSubDomains";

        await next(context);
    }
}

public static class SecuritySetup
{
    /// <summary>Trust X-Forwarded-* from a local reverse proxy (Nginx on the same server, IIS).</summary>
    public static IServiceCollection AddReverseProxySupport(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<ForwardedHeadersOptions>(o =>
        {
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
            // Docker / remote proxy: set "Site:TrustAllProxies": true in appsettings or env Site__TrustAllProxies=true.
            if (config.GetValue<bool>("Site:TrustAllProxies"))
            {
                o.KnownIPNetworks.Clear();
                o.KnownProxies.Clear();
            }
        });
        return services;
    }

    /// <summary>
    /// Long browser caching for versioned assets (?v=hash) and uploaded images (random names never change).
    /// Only WebP files are served from /uploads, nothing else (no scripts, no HTML).
    /// </summary>
    public static StaticFileOptions StaticFiles() => new()
    {
        OnPrepareResponse = ctx =>
        {
            var path = ctx.Context.Request.Path.Value ?? "";
            var versioned = ctx.Context.Request.Query.ContainsKey("v");
            if (path.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/fonts/", StringComparison.OrdinalIgnoreCase) || versioned)
                ctx.Context.Response.Headers[HeaderNames.CacheControl] = "public, max-age=31536000, immutable";
            else
                ctx.Context.Response.Headers[HeaderNames.CacheControl] = path.StartsWith("/img/", StringComparison.OrdinalIgnoreCase) ? "public, max-age=2592000" : "public, max-age=86400";
        }
    };

    public static IApplicationBuilder UseUploadsGuard(this IApplicationBuilder app) => app.Use(async (ctx, next) =>
    {
        var path = ctx.Request.Path.Value ?? "";
        // Only generated files are served: WebP images and (compressed) MP4 videos.
        if (path.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)
            && !path.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)
            && !(path.StartsWith("/uploads/videos/", StringComparison.OrdinalIgnoreCase) && path.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)))
        {
            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
        await next();
    });
}
