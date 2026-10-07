using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace BeautyByNegin.Web.Infrastructure.Security;

/// <summary>
/// Spam protection without reCAPTCHA (Google is blocked in Iran):
/// honeypot field + minimum fill time + IP rate limiting + antiforgery token.
/// </summary>
public static class SpamGuard
{
    public const string FormsPolicy = "forms";
    public const string ChatAuthPolicy = "chat-auth";
    public const string ChatPolicy = "chat";

    private static readonly TimeSpan MinFillTime = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan MaxFormAge = TimeSpan.FromDays(2);

    /// <summary>True when the submission looks automated (bots get a fake "success" and nothing is saved).</summary>
    public static bool LooksLikeBot(string? honeypot, long startedTicks)
    {
        if (!string.IsNullOrEmpty(honeypot)) return true;
        if (startedTicks <= 0) return true;
        var elapsed = DateTime.UtcNow - new DateTime(Math.Clamp(startedTicks, DateTime.MinValue.Ticks, DateTime.MaxValue.Ticks), DateTimeKind.Utc);
        return elapsed < MinFillTime || elapsed > MaxFormAge;
    }

    private static string ClientKey(HttpContext ctx) => ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    public static IServiceCollection AddSpamProtection(this IServiceCollection services)
    {
        services.AddRateLimiter(o =>
        {
            o.AddPolicy(FormsPolicy, ctx => RateLimitPartition.GetFixedWindowLimiter(ClientKey(ctx),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(10), QueueLimit = 0 }));
            o.AddPolicy(ChatAuthPolicy, ctx => RateLimitPartition.GetFixedWindowLimiter(ClientKey(ctx),
                // Login / code requests. Guessing codes is already blocked per code (5 tries, 15 min) and per e-mail (1 code per minute).
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(15), QueueLimit = 0 }));
            o.AddPolicy(ChatPolicy, ctx => RateLimitPartition.GetFixedWindowLimiter(ClientKey(ctx),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 90, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

            o.OnRejected = async (context, ct) =>
            {
                var http = context.HttpContext;
                http.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                var wantsJson = http.Request.Headers.Accept.ToString().Contains("application/json")
                                || http.Request.Headers.XRequestedWith == "fetch";
                if (wantsJson)
                {
                    await http.Response.WriteAsJsonAsync(new { ok = false, errorKey = "form.error.rateLimit" }, ct);
                    return;
                }
                // Normal form post: back to the form page with a friendly message.
                var referer = http.Request.Headers.Referer.ToString();
                var target = Uri.TryCreate(referer, UriKind.Absolute, out var u) && u.Host == http.Request.Host.Host
                    ? u.PathAndQuery : "/";
                http.Response.StatusCode = StatusCodes.Status303SeeOther;
                http.Response.Headers.Location = target + (target.Contains('?') ? "&" : "?") + "rl=1";
            };
        });
        return services;
    }
}
