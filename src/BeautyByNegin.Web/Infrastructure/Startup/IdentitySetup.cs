using BeautyByNegin.Web.Data;
using BeautyByNegin.Web.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace BeautyByNegin.Web.Infrastructure.Startup;

public static class IdentitySetup
{
    public static IServiceCollection AddAppIdentity(this IServiceCollection services, IConfiguration config)
    {
        var adminPath = AdminPath(config);

        services
            .AddIdentity<AppUser, IdentityRole>(o =>
            {
                o.User.RequireUniqueEmail = false;
                o.SignIn.RequireConfirmedAccount = false;

                // Simple but safe rules: long enough, no forced symbol soup that a non-technical user forgets.
                o.Password.RequiredLength = 8;
                o.Password.RequireDigit = true;
                o.Password.RequireNonAlphanumeric = false;
                o.Password.RequireUppercase = false;
                o.Password.RequireLowercase = false;

                o.Lockout.AllowedForNewUsers = true;
                o.Lockout.MaxFailedAccessAttempts = 5;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(o =>
        {
            o.Cookie.Name = "bbn.auth";
            o.Cookie.HttpOnly = true;
            o.Cookie.SameSite = SameSiteMode.Lax;
            // SameAsRequest: the site keeps working before an SSL certificate is installed.
            o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            o.LoginPath = $"{adminPath}/login";
            o.LogoutPath = $"{adminPath}/logout";
            o.AccessDeniedPath = $"{adminPath}/login";
            o.ExpireTimeSpan = TimeSpan.FromDays(14);
            o.SlidingExpiration = true;
        });

        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.Panel, p => p.RequireRole(AppRoles.Admin, AppRoles.Editor))
            .AddPolicy(Policies.AdminOnly, p => p.RequireRole(AppRoles.Admin));

        return services;
    }

    /// <summary>Admin panel base path, e.g. "/admin". Configurable via appsettings "Site:AdminPath".</summary>
    public static string AdminPath(IConfiguration config)
    {
        var path = config["Site:AdminPath"];
        if (string.IsNullOrWhiteSpace(path)) path = "/admin";
        path = "/" + path.Trim().Trim('/');
        return path;
    }
}

public static class Policies
{
    public const string Panel = "Panel";
    public const string AdminOnly = "AdminOnly";
}
