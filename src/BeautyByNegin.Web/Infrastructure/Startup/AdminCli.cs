using BeautyByNegin.DataAccess.Entities;
using Microsoft.AspNetCore.Identity;

namespace BeautyByNegin.Web.Infrastructure.Startup;

/// <summary>
/// Command-line maintenance (works without e-mail settings):
///   BeautyByNegin.Web admin create  &lt;user&gt; &lt;password&gt; [Admin|Editor]
///   BeautyByNegin.Web admin reset-password &lt;user&gt; &lt;new-password&gt;
///   BeautyByNegin.Web admin list
/// </summary>
public static class AdminCli
{
    public static bool IsCliCall(string[] args) => args.Length >= 2 && args[0] == "admin";

    public static async Task<int> RunAsync(IServiceProvider services, string[] args)
    {
        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        switch (args[1])
        {
            case "create" when args.Length >= 4:
            {
                var role = args.Length >= 5 && args[4].Equals("Editor", StringComparison.OrdinalIgnoreCase) ? AppRoles.Editor : AppRoles.Admin;
                var user = new AppUser { UserName = args[2], DisplayName = args[2], PanelLanguage = "fa", MustChangePassword = true };
                var result = await users.CreateAsync(user, args[3]);
                if (!result.Succeeded) return Fail(result);
                await users.AddToRoleAsync(user, role);
                Console.WriteLine($"OK: user '{args[2]}' created ({role}). The password must be changed at first login.");
                return 0;
            }
            case "reset-password" when args.Length >= 4:
            {
                var user = await users.FindByNameAsync(args[2]);
                if (user is null) { Console.Error.WriteLine($"User '{args[2]}' not found."); return 1; }
                var token = await users.GeneratePasswordResetTokenAsync(user);
                var result = await users.ResetPasswordAsync(user, token, args[3]);
                if (!result.Succeeded) return Fail(result);
                await users.SetLockoutEndDateAsync(user, null);
                await users.ResetAccessFailedCountAsync(user);
                Console.WriteLine($"OK: password of '{args[2]}' changed and account unlocked.");
                return 0;
            }
            case "list":
                foreach (var u in users.Users.ToList())
                    Console.WriteLine($"{u.UserName}\t{string.Join(",", await users.GetRolesAsync(u))}");
                return 0;
            default:
                Console.WriteLine("Usage:\n  admin create <user> <password> [Admin|Editor]\n  admin reset-password <user> <new-password>\n  admin list");
                return 1;
        }
    }

    private static int Fail(IdentityResult r)
    {
        Console.Error.WriteLine("Failed: " + string.Join("; ", r.Errors.Select(e => e.Description)));
        return 1;
    }
}
