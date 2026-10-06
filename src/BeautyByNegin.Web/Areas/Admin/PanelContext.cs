using System.Globalization;
using BeautyByNegin.Business.Settings;
using BeautyByNegin.DataAccess.Entities;

namespace BeautyByNegin.Web.Areas.Admin;

/// <summary>Per-request panel state: panel language (independent of site languages), texts, user, content languages.</summary>
public sealed class PanelContext
{
    public static readonly string[] PanelLanguages = ["fa", "tr", "de", "en"];

    public required string Lang { get; init; }
    public required AppUser User { get; init; }
    public required bool IsAdmin { get; init; }
    public required SiteSettings Settings { get; init; }
    /// <summary>Site content languages (tabs FA | TR | DE | EN | AR), enabled ones first.</summary>
    public required IReadOnlyList<SiteLanguage> ContentLanguages { get; init; }
    public required SiteLanguage DefaultLanguage { get; init; }
    public required string BasePath { get; init; }

    public bool IsRtl => Lang == "fa";
    public string Dir => IsRtl ? "rtl" : "ltr";

    /// <summary>Panel text in the panel language.</summary>
    public string this[string key] => PanelText.Get(key, Lang);
    /// <summary>Panel text with placeholders; numbers are written with Persian digits in the Persian panel.</summary>
    public string F(string key, params object?[] args)
    {
        if (Lang == "fa")
            args = args.Select(a => a is int or long or decimal or double ? (object)Digits.ToPersian(Convert.ToString(a, CultureInfo.InvariantCulture)) : a).ToArray();
        return string.Format(CultureInfo.InvariantCulture, PanelText.Get(key, Lang), args);
    }

    public string Url(string path) => BasePath + "/" + path.TrimStart('/');

    /// <summary>Date/time in the salon's time zone; Jalali for the Persian panel.</summary>
    public string Date(DateTime utc, bool withTime = true)
    {
        var local = DateDisplay.ToLocal(utc, Settings.TimeZone);
        var lang = new SiteLanguage(Lang, Lang switch { "fa" => "fa-IR", "tr" => "tr-TR", "de" => "de-DE", _ => "en-GB" },
            "", "", IsRtl, true, false, 0, Lang == "fa");
        var d = DateDisplay.FormatShortDate(local, lang);
        return withTime ? $"{d} {Digits.Native(local.ToString("HH:mm", CultureInfo.InvariantCulture), lang)}" : d;
    }

    public string Day(DateOnly day)
    {
        var lang = new SiteLanguage(Lang, Lang switch { "fa" => "fa-IR", "tr" => "tr-TR", "de" => "de-DE", _ => "en-GB" },
            "", "", IsRtl, true, false, 0, Lang == "fa");
        return DateDisplay.FormatDate(day.ToDateTime(TimeOnly.MinValue), lang);
    }
}
