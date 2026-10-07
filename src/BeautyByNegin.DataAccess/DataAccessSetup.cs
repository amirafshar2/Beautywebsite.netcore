using BeautyByNegin.DataAccess.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BeautyByNegin.DataAccess;

public static class DataAccessSetup
{
    /// <summary>
    /// Registers the DbContext. The database is a single SQLite file in App_Data, so moving the
    /// site to another server = copying the folder (or restoring a backup zip).
    /// </summary>
    public static IServiceCollection AddDataAccess(this IServiceCollection services, IConfiguration config, string contentRoot)
    {
        var appData = AppPaths.AppData(contentRoot);
        Directory.CreateDirectory(appData);

        var connectionString = config.GetConnectionString("Default");
        connectionString = string.IsNullOrWhiteSpace(connectionString)
            ? $"Data Source={Path.Combine(appData, "site.db")}"
            : connectionString.Replace("|DataDirectory|", appData);

        services.AddDbContext<AppDbContext>(o => o.UseSqlite(connectionString));
        services.AddScoped<DatabaseSeeder>();
        return services;
    }

    /// <summary>Applies migrations and inserts seed data that is still missing (never overwrites admin edits).</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider rootServices)
    {
        using var scope = rootServices.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Database");

        await db.Database.MigrateAsync();
        await sp.GetRequiredService<DatabaseSeeder>().SeedAsync();

        logger.LogInformation("Database ready");
    }
}

/// <summary>
/// All mutable data lives in ONE folder, App_Data/: database, keys, logs, backups and uploads/ (images, videos).
/// One folder to back up, one Docker volume, one Render disk. Uploads are served under the URL /uploads/.
/// </summary>
public static class AppPaths
{
    public static string AppData(string contentRoot) => Path.Combine(contentRoot, "App_Data");
    public static string Logs(string contentRoot) => Path.Combine(AppData(contentRoot), "logs");
    public static string Backups(string contentRoot) => Path.Combine(AppData(contentRoot), "backups");
    public static string Uploads(string contentRoot) => Path.Combine(AppData(contentRoot), "uploads");
    /// <summary>Where uploads were stored by earlier versions (moved automatically at start).</summary>
    public static string LegacyUploads(string webRoot) => Path.Combine(webRoot, "uploads");
}
