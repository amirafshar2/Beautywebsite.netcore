using BeautyByNegin.Web.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BeautyByNegin.Web.Data;

public static class DatabaseSetup
{
    /// <summary>
    /// Registers the DbContext. The database is a single SQLite file in App_Data, so moving the
    /// site to another server = copying the folder (or restoring a backup zip).
    /// </summary>
    public static IServiceCollection AddAppDatabase(this IServiceCollection services, IConfiguration config, string contentRoot)
    {
        var appData = AppPaths.AppData(contentRoot);
        Directory.CreateDirectory(appData);

        var connectionString = config.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = $"Data Source={Path.Combine(appData, "site.db")}";
        else
            connectionString = connectionString.Replace("|DataDirectory|", appData);

        services.AddDbContext<AppDbContext>(o => o.UseSqlite(connectionString));
        return services;
    }

    /// <summary>Creates/updates the database schema and inserts the minimum seed data.</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider rootServices)
    {
        using var scope = rootServices.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSetup");

        if (db.Database.GetMigrations().Any())
            await db.Database.MigrateAsync();
        else
            await db.Database.EnsureCreatedAsync();

        await SeedLanguagesAsync(db);
        await SeedRolesAsync(sp.GetRequiredService<RoleManager<IdentityRole>>());

        logger.LogInformation("Database ready");
    }

    private static async Task SeedLanguagesAsync(AppDbContext db)
    {
        if (await db.Languages.AnyAsync()) return;

        db.Languages.AddRange(
            new Language { Code = "fa", CultureName = "fa-IR", NativeName = "فارسی", ShortLabel = "FA", IsRtl = true, IsDefault = true, SortOrder = 1, UseNativeDigits = true },
            new Language { Code = "tr", CultureName = "tr-TR", NativeName = "Türkçe", ShortLabel = "TR", SortOrder = 2 },
            new Language { Code = "de", CultureName = "de-DE", NativeName = "Deutsch", ShortLabel = "DE", SortOrder = 3 },
            new Language { Code = "en", CultureName = "en-US", NativeName = "English", ShortLabel = "EN", SortOrder = 4 });
        await db.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roles)
    {
        foreach (var role in AppRoles.All)
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new IdentityRole(role));
    }
}

/// <summary>All mutable data lives in two places: App_Data/ and wwwroot/uploads/.</summary>
public static class AppPaths
{
    public static string AppData(string contentRoot) => Path.Combine(contentRoot, "App_Data");
    public static string Logs(string contentRoot) => Path.Combine(AppData(contentRoot), "logs");
    public static string Backups(string contentRoot) => Path.Combine(AppData(contentRoot), "backups");
    public static string Uploads(string webRoot) => Path.Combine(webRoot, "uploads");
}
