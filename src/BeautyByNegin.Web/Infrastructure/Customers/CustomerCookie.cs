using BeautyByNegin.Business.Customers;

namespace BeautyByNegin.Web.Infrastructure.Customers;

/// <summary>The customer login cookie (random token, HttpOnly). Shared by the account pages and the chat.</summary>
public static class CustomerCookie
{
    public const string Name = "bbn.customer";

    public static string? Read(HttpRequest request) => request.Cookies[Name];

    public static void Write(HttpContext http, string token) => http.Response.Cookies.Append(Name, token, new CookieOptions
    {
        HttpOnly = true,
        SameSite = SameSiteMode.Lax,
        Secure = http.Request.IsHttps,
        IsEssential = true,
        Expires = DateTimeOffset.UtcNow.AddDays(180),
        Path = "/"
    });

    public static void Delete(HttpContext http) => http.Response.Cookies.Delete(Name, new CookieOptions { Path = "/" });
}

/// <summary>Who is visiting in this request (loaded once per request, used by the header, forms and account pages).</summary>
public sealed class CustomerContext(IHttpContextAccessor http, ICustomerAccountService accounts)
{
    private CurrentCustomer? _current;

    public async Task<CurrentCustomer> GetAsync()
    {
        if (_current is not null) return _current;
        var ctx = http.HttpContext;
        var token = ctx is null ? null : CustomerCookie.Read(ctx.Request);
        _current = token is null ? CurrentCustomer.Anonymous : await accounts.GetCurrentAsync(token, ctx!.RequestAborted);
        return _current;
    }

    /// <summary>Forget the cached value after login/logout in the same request.</summary>
    public void Reset() => _current = null;
}
