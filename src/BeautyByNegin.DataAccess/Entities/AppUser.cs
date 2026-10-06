using Microsoft.AspNetCore.Identity;

namespace BeautyByNegin.DataAccess.Entities;

public class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = "";

    /// <summary>Language of the admin panel UI for this user (independent of site content languages).</summary>
    public string PanelLanguage { get; set; } = "fa";

    /// <summary>Forces a password change on next login (first setup / reset).</summary>
    public bool MustChangePassword { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Editor = "Editor";
    public static readonly string[] All = [Admin, Editor];
}
