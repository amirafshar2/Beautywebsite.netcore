using BeautyByNegin.Business;
using BeautyByNegin.DataAccess;
using Microsoft.AspNetCore.DataProtection;
using BeautyByNegin.Web.Infrastructure.Localization;
using BeautyByNegin.Web.Infrastructure.Startup;
using Microsoft.AspNetCore.ResponseCompression;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
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

// ---------- Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
}
app.UseStatusCodePagesWithReExecute("/error/{0}");

app.UseResponseCompression();
app.UseStaticFiles();
app.UseSerilogRequestLogging();

app.UseRouting();
app.UseMiddleware<SiteCultureMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

// Public site: every page lives under a language prefix, e.g. /fa/..., /de/...
app.MapControllerRoute(
    name: "localized",
    pattern: "{culture:culture}/{controller=Home}/{action=Index}/{id?}");

// "/" -> default language (or the visitor's browser language when it is enabled)
app.MapControllerRoute(
    name: "root",
    pattern: "",
    defaults: new { controller = "Root", action = "Index" });

app.MapControllerRoute(
    name: "error",
    pattern: "error/{code:int?}",
    defaults: new { controller = "Error", action = "Index" });

app.Run();
