using BeautyByNegin.Business;
using BeautyByNegin.DataAccess;
using Microsoft.AspNetCore.DataProtection;
using BeautyByNegin.Web.Infrastructure;
using BeautyByNegin.Web.Infrastructure.Localization;
using BeautyByNegin.Web.Infrastructure.Routing;
using BeautyByNegin.Web.Infrastructure.Security;
using BeautyByNegin.Web.Infrastructure.Startup;
using Microsoft.AspNetCore.ResponseCompression;
using Serilog;

// A published app always uses its own folder as content root (App_Data, wwwroot), even when it is
// started from another directory (systemd without WorkingDirectory, Plesk scheduled task, CLI calls).
// During development (dotnet run) the project folder is used as usual.
var publishedRoot = AppContext.BaseDirectory;
var builder = Directory.Exists(Path.Combine(publishedRoot, "wwwroot"))
    ? WebApplication.CreateBuilder(new WebApplicationOptions { Args = args, ContentRootPath = publishedRoot })
    : WebApplication.CreateBuilder(args);
var contentRoot = builder.Environment.ContentRootPath;

// ---------- Logging: rolling files in App_Data/logs (no technical details are ever shown to visitors)
Directory.CreateDirectory(AppPaths.Logs(contentRoot));
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(Path.Combine(AppPaths.Logs(contentRoot), "site-.log"),
        rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30));

// ---------- Services
builder.Services.AddDataAccess(builder.Configuration, contentRoot);
builder.Services.AddSingleton<BeautyByNegin.Business.Media.IMediaPaths, MediaPaths>();
builder.Services.AddBusiness();
builder.Services.AddAppIdentity(builder.Configuration);

// Encryption keys (login cookies, encrypted SMTP password / Telegram token) live in App_Data,
// so they move together with the database when the site is moved to another server.
builder.Services.AddDataProtection()
    .SetApplicationName("BeautyByNegin")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(AppPaths.AppData(contentRoot), "keys")));

builder.Services.AddRouting(o =>
{
    o.LowercaseUrls = true;
    o.ConstraintMap[CultureRouteConstraint.Name] = typeof(CultureRouteConstraint);
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddSpamProtection();
builder.Services.AddAntiforgery(o =>
{
    o.Cookie.Name = "bbn.af";
    o.HeaderName = "X-CSRF-TOKEN";   // used by the chat and newsletter fetch() calls
});
builder.Services.AddScoped<SiteContext>();
builder.Services.AddScoped<BeautyByNegin.Web.Infrastructure.Customers.CustomerContext>();
builder.Services.AddSingleton<AssetUrls>();
builder.Services.AddReverseProxySupport(builder.Configuration);
builder.Services.AddControllersWithViews();

// Output Persian/Turkish/German characters as-is instead of &#x...; entities (smaller, readable HTML).
builder.Services.Configure<Microsoft.Extensions.WebEncoders.WebEncoderOptions>(o =>
    o.TextEncoderSettings = new System.Text.Encodings.Web.TextEncoderSettings(System.Text.Unicode.UnicodeRanges.All));

builder.Services.AddResponseCompression(o =>
{
    o.EnableForHttps = true;
    o.Providers.Add<BrotliCompressionProvider>();
    o.Providers.Add<GzipCompressionProvider>();
});

var app = builder.Build();

// ---------- Database (auto-create + seed on first run)
await app.Services.InitializeDatabaseAsync();

// Starter images (brand artwork) + sample gallery/Instagram/aftercare page, added once per database.
if (!AdminCli.IsCliCall(args))
{
    using var seedScope = app.Services.CreateScope();
    await seedScope.ServiceProvider.GetRequiredService<BeautyByNegin.Business.Content.SampleContentSeeder>()
        .SeedAsync(Path.Combine(app.Environment.ContentRootPath, "SampleContent"));
}

// Command line: dotnet BeautyByNegin.Web.dll admin reset-password <user> <password>
if (AdminCli.IsCliCall(args))
    return await AdminCli.RunAsync(app.Services, args);

// ---------- Pipeline
app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
}
app.UseStatusCodePagesWithReExecute("/error/{0}");

// Compression only in production: in Development it blocks Visual Studio's browser-refresh script.
if (!app.Environment.IsDevelopment())
    app.UseResponseCompression();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseUploadsGuard();
app.UseMinifiedAssets();
app.UseStaticFiles(SecuritySetup.StaticFiles());
app.UseSerilogRequestLogging();

app.UseRouting();
app.UseMiddleware<SiteCultureMiddleware>();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Admin panel (MVC Area "Admin"), e.g. /admin, /admin/services/edit/3
var adminPath = IdentitySetup.AdminPath(app.Configuration).Trim('/');
app.MapAreaControllerRoute("admin-login", "Admin", adminPath + "/login", new { controller = "Account", action = "Login" });
app.MapAreaControllerRoute("admin-setup", "Admin", adminPath + "/setup", new { controller = "Setup", action = "Index" });
app.MapAreaControllerRoute(
    name: "admin",
    areaName: "Admin",
    pattern: adminPath + "/{controller=Dashboard}/{action=Index}/{id?}");

// Public site: every page lives under a language prefix with translated segments, e.g. /de/behandlungen
// Site colors chosen in the panel ("Colors"): overrides the defaults of site.css. Versioned (?v=), so cached long.
app.MapGet("/theme.css", async (HttpContext http, BeautyByNegin.Business.Settings.ISettingsService settings) =>
{
    var css = BeautyByNegin.Business.Content.Theme.BuildCss(await settings.GetAsync(http.RequestAborted));
    http.Response.Headers.CacheControl = http.Request.Query.ContainsKey("v") ? "public, max-age=31536000, immutable" : "no-cache";
    return Results.Text(css, "text/css; charset=utf-8");
});

app.MapSiteRoutes();

// "/" -> default language (or the visitor's browser language when it is enabled)
app.MapControllerRoute(
    name: "root",
    pattern: "",
    defaults: new { controller = "Root", action = "Index" });

app.MapControllerRoute(
    name: "error",
    pattern: "error/{code:int?}",
    defaults: new { controller = "Error", action = "Index" });

await app.RunAsync();
return 0;
