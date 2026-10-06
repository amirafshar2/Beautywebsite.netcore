using System.Text;
using BeautyByNegin.Business.Localization;
using BeautyByNegin.Business.Settings;

namespace BeautyByNegin.Business.Content;

/// <summary>Builds tel:, wa.me, mailto: and social links from the contact settings.</summary>
public static class ContactLinks
{
    /// <summary>Digits only (plus leading +) — accepts Persian digits and spaces as typed by the admin.</summary>
    public static string NormalizePhone(string? phone)
    {
        var latin = Digits.ToLatin(phone ?? "").Trim();
        var sb = new StringBuilder();
        foreach (var c in latin)
            if (char.IsAsciiDigit(c) || (c == '+' && sb.Length == 0)) sb.Append(c);
        var result = sb.ToString();
        if (result.StartsWith("00", StringComparison.Ordinal)) result = "+" + result[2..];
        return result;
    }

    public static string? Tel(string? phone)
    {
        var p = NormalizePhone(phone);
        return p.Length < 5 ? null : "tel:" + p;
    }

    /// <summary>https://wa.me/{number}?text=… (number in international format without +).</summary>
    public static string? WhatsApp(string? number, string? message = null)
    {
        var p = NormalizePhone(number).TrimStart('+');
        if (p.Length < 6) return null;
        var url = "https://wa.me/" + p;
        return string.IsNullOrWhiteSpace(message) ? url : url + "?text=" + Uri.EscapeDataString(message);
    }

    public static string? Mail(string? email, string? subject = null)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) return null;
        var url = "mailto:" + email.Trim();
        return string.IsNullOrWhiteSpace(subject) ? url : url + "?subject=" + Uri.EscapeDataString(subject);
    }

    public static string? Instagram(string? username)
        => string.IsNullOrWhiteSpace(username) ? null
            : username.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? username
            : "https://instagram.com/" + username.Trim().TrimStart('@');

    public static string? Telegram(string? username)
        => string.IsNullOrWhiteSpace(username) ? null
            : username.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? username
            : "https://t.me/" + username.Trim().TrimStart('@');

    /// <summary>All links of the site in one object (used by header, footer, mobile bar, modals).</summary>
    public static ContactInfo From(SiteSettings s) => new(
        Phone: s.Phone,
        PhoneUrl: Tel(s.Phone),
        WhatsAppNumber: s.WhatsApp,
        Email: s.Email,
        InstagramUser: s.Instagram,
        InstagramUrl: Instagram(s.Instagram),
        TelegramUrl: Telegram(s.Telegram),
        MapUrl: string.IsNullOrWhiteSpace(s.MapUrl) ? null : s.MapUrl);
}

public sealed record ContactInfo(
    string Phone,
    string? PhoneUrl,
    string WhatsAppNumber,
    string Email,
    string InstagramUser,
    string? InstagramUrl,
    string? TelegramUrl,
    string? MapUrl)
{
    public string? WhatsAppUrl(string? message = null) => ContactLinks.WhatsApp(WhatsAppNumber, message);
    public string? MailUrl(string? subject = null) => ContactLinks.Mail(Email, subject);
    public bool HasWhatsApp => ContactLinks.WhatsApp(WhatsAppNumber) is not null;
    public bool HasEmail => ContactLinks.Mail(Email) is not null;
}
