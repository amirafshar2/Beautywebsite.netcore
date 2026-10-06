using System.Globalization;
using System.Text;

namespace BeautyByNegin.Web.Infrastructure.Localization;

/// <summary>
/// Display-only date helpers. The database always stores UTC / Gregorian; Persian pages show
/// Jalali (Shamsi) dates, other languages show Gregorian dates in their own format.
/// </summary>
public static class DateDisplay
{
    private static readonly PersianCalendar Persian = new();

    private static readonly string[] PersianMonths =
    [
        "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
        "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"
    ];

    /// <summary>Converts a UTC timestamp to the site's time zone (set in the admin panel).</summary>
    public static DateTime ToLocal(DateTime utc, TimeZoneInfo zone)
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), zone);

    /// <summary>Long date, e.g. "۱۴ مهر ۱۴۰۵" (fa) or "6. Oktober 2026" (de).</summary>
    public static string FormatDate(DateTime date, SiteLanguage lang)
    {
        if (lang.Code == "fa")
        {
            var text = $"{Persian.GetDayOfMonth(date)} {PersianMonths[Persian.GetMonth(date) - 1]} {Persian.GetYear(date)}";
            return lang.UseNativeDigits ? Digits.ToPersian(text) : text;
        }
        return date.ToString("D", lang.CreateCulture());
    }

    /// <summary>Short numeric date, e.g. "۱۴۰۵/۰۷/۱۴" (fa) or "06.10.2026" (de).</summary>
    public static string FormatShortDate(DateTime date, SiteLanguage lang)
    {
        if (lang.Code == "fa")
        {
            var text = $"{Persian.GetYear(date):0000}/{Persian.GetMonth(date):00}/{Persian.GetDayOfMonth(date):00}";
            return lang.UseNativeDigits ? Digits.ToPersian(text) : text;
        }
        return date.ToString("d", lang.CreateCulture());
    }

    /// <summary>Converts a Jalali date to a Gregorian <see cref="DateOnly"/> (for form input).</summary>
    public static DateOnly FromJalali(int year, int month, int day)
        => DateOnly.FromDateTime(Persian.ToDateTime(year, month, day, 0, 0, 0, 0));

    /// <summary>Converts a Gregorian date to Jalali parts.</summary>
    public static (int Year, int Month, int Day) ToJalali(DateTime date)
        => (Persian.GetYear(date), Persian.GetMonth(date), Persian.GetDayOfMonth(date));
}

public static class Digits
{
    private const string PersianDigits = "۰۱۲۳۴۵۶۷۸۹";
    private const string ArabicDigits = "٠١٢٣٤٥٦٧٨٩";

    public static string ToPersian(string? input)
    {
        if (string.IsNullOrEmpty(input)) return input ?? "";
        var sb = new StringBuilder(input.Length);
        foreach (var c in input)
            sb.Append(c is >= '0' and <= '9' ? PersianDigits[c - '0'] : c);
        return sb.ToString();
    }

    /// <summary>Normalises Persian/Arabic digits typed by users (e.g. phone numbers) to Latin digits.</summary>
    public static string ToLatin(string? input)
    {
        if (string.IsNullOrEmpty(input)) return input ?? "";
        var sb = new StringBuilder(input.Length);
        foreach (var c in input)
        {
            var p = PersianDigits.IndexOf(c);
            if (p >= 0) { sb.Append((char)('0' + p)); continue; }
            var a = ArabicDigits.IndexOf(c);
            sb.Append(a >= 0 ? (char)('0' + a) : c);
        }
        return sb.ToString();
    }

    /// <summary>Formats a number for display in the given language.</summary>
    public static string Format(int number, SiteLanguage lang)
    {
        var text = number.ToString(CultureInfo.InvariantCulture);
        return lang.UseNativeDigits ? ToPersian(text) : text;
    }
}
