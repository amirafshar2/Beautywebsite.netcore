using BeautyByNegin.DataAccess;
using BeautyByNegin.DataAccess.Entities;
using BeautyByNegin.Business.Settings;
using Microsoft.AspNetCore.Identity;

namespace BeautyByNegin.Web.Infrastructure.Startup;

/// <summary>
/// Optional: creates the first admin from configuration (e.g. environment variables on Render/Docker:
/// Site__InitialAdmin__UserName, Site__InitialAdmin__Password) when the database has no panel user yet.
/// Useful on hosts with a temporary file system, where the setup wizard would otherwise be open to anyone
/// after every restart. Without these values the normal setup wizard (/admin/setup) is used.
/// </summary>
public static class InitialAdmin
{
    public static async Task EnsureAsync(IServiceProvider services, IConfiguration config, ILogger logger)
    {
        var userName = config["Site:InitialAdmin:UserName"]?.Trim();
        var password = config["Site:InitialAdmin:Password"];
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password)) return;

        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        if (users.Users.Any()) return;

        var user = new AppUser
        {
            UserName = userName,
            DisplayName = config["Site:InitialAdmin:DisplayName"]?.Trim() is { Length: > 0 } d ? d : userName,
            PanelLanguage = "fa"
        };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            logger.LogWarning("Initial admin could not be created: {Errors}", string.Join(" ", result.Errors.Select(e => e.Description)));
            return;
        }
        await users.AddToRoleAsync(user, AppRoles.Admin);
        await scope.ServiceProvider.GetRequiredService<ISettingsService>()
            .SaveAsync(new Dictionary<string, string?> { [SettingKeys.SetupCompleted] = "true" });
        logger.LogInformation("Initial admin '{User}' created from configuration", userName);
    }
}
