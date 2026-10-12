using System.Globalization;
using System.Text;
using BeautyByNegin.Business.Content;
using BeautyByNegin.Business.Localization;

namespace BeautyByNegin.Business.Notifications;

/// <summary>
/// Builds the Telegram messages for the owner: always in Persian, with a short explanation of what happened
/// and what to do next. Phone numbers and e-mails stay in Latin characters so Telegram makes them tappable.
/// </summary>
public sealed class TelegramText
{
    public static readonly SiteLanguage Fa = new("fa", "fa-IR", "فارسی", "FA", true, true, true, 0, true);

    private readonly StringBuilder _sb = new();

    public TelegramText(string emoji, string title, string explanation)
    {
        _sb.Append(emoji).Append(" <b>").Append(Html(title)).Append("</b>\n");
        _sb.Append("<i>").Append(Html(explanation)).Append("</i>\n\n");
    }

    /// <summary>"👤 نام: …" (value is HTML-escaped; empty values are written as the given placeholder).</summary>
    public TelegramText Line(string emoji, string label, string? value, string? emptyText = null)
    {
        var v = string.IsNullOrWhiteSpace(value) ? emptyText : value;
        if (v is null) return this;
        _sb.Append(emoji).Append(' ').Append("<b>").Append(Html(label)).Append(":</b> ").Append(Html(v)).Append('\n');
        return this;
    }

    /// <summary>A longer text (message of the client) in its own block.</summary>
    public TelegramText Block(string emoji, string label, string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return this;
        _sb.Append('\n').Append(emoji).Append(" <b>").Append(Html(label)).Append(":</b>\n").Append(Html(text.Trim())).Append('\n');
        return this;
    }

    public TelegramText Raw(string html) { _sb.Append(html); return this; }

    /// <summary>"👉 قدم بعدی: …" and the link(s) to the panel.</summary>
    public TelegramText Next(string whatToDo, params (string Text, string? Url)[] links)
    {
        _sb.Append("\n👉 <b>قدم بعدی:</b> ").Append(Html(whatToDo)).Append('\n');
        foreach (var (text, url) in links)
            if (!string.IsNullOrWhiteSpace(url))
                _sb.Append("🔗 <a href=\"").Append(Html(url)).Append("\">").Append(Html(text)).Append("</a>\n");
        return this;
    }

    public override string ToString() => _sb.ToString().TrimEnd();

    /// <summary>Telegram HTML needs only &lt; &gt; &amp; and quotes escaped; Persian, Turkish and German letters stay readable.</summary>
    private static string Html(string? text) => (text ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    // ------------------------------------------------------------------ helpers

    /// <summary>Persian name of a site language, plus a hint when the client wrote in another language.</summary>
    public static string LanguageName(string code) => code switch
    {
        "fa" => "فارسی",
        "tr" => "ترکی",
        "de" => "آلمانی",
        "en" => "انگلیسی",
        "ar" => "عربی",
        _ => code.ToUpperInvariant()
    };

    public static string LanguageLine(string code)
        => code == "fa" ? "فارسی" : $"{LanguageName(code)} (بهتر است به همین زبان جواب بدهید)";

    /// <summary>"شنبه ۲۰ مهر ۱۴۰۵، ساعت ۱۴:۳۰" in the site's time zone.</summary>
    public static string Now(TimeZoneInfo zone)
    {
        var local = DateDisplay.ToLocal(DateTime.UtcNow, zone);
        var day = new CultureInfo("fa-IR").DateTimeFormat.GetDayName(local.DayOfWeek);
        return $"{day} {DateDisplay.FormatDate(local, Fa)}، ساعت {Digits.ToPersian(local.ToString("HH:mm", CultureInfo.InvariantCulture))}";
    }

    public static string Date(DateOnly date)
    {
        var dt = date.ToDateTime(TimeOnly.MinValue);
        var day = new CultureInfo("fa-IR").DateTimeFormat.GetDayName(dt.DayOfWeek);
        return $"{day} {DateDisplay.FormatDate(dt, Fa)}";
    }

    public static string Stars(int rating)
        => new string('★', rating) + new string('☆', 5 - rating) + $" ({Digits.ToPersian(rating.ToString(CultureInfo.InvariantCulture))} از ۵)";

    /// <summary>Phone or e-mail? (contact form has one field for both)</summary>
    public static bool IsEmail(string contact) => contact.Contains('@');

    public static string? WhatsApp(string? phone) => ContactLinks.WhatsApp(phone);
}
