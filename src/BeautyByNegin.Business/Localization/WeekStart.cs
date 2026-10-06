namespace BeautyByNegin.Business.Localization;

/// <summary>
/// First day of the week for the business country (Iran: Saturday, Turkey/Germany: Monday ...),
/// so opening hours are listed the way local customers expect. Changes automatically with the
/// country chosen in the panel when the business moves.
/// </summary>
public static class WeekStart
{
    private static readonly HashSet<string> SaturdayCountries =
        ["IR", "AF", "AE", "SA", "QA", "KW", "BH", "OM", "EG", "IQ", "JO", "SY", "LY", "DZ", "SD", "YE"];
    private static readonly HashSet<string> SundayCountries =
        ["US", "CA", "IL", "JP", "BR", "MX", "PH", "IN", "AU"];

    public static DayOfWeek For(string? countryCode)
    {
        var c = (countryCode ?? "").Trim().ToUpperInvariant();
        return SaturdayCountries.Contains(c) ? DayOfWeek.Saturday
             : SundayCountries.Contains(c) ? DayOfWeek.Sunday
             : DayOfWeek.Monday;
    }

    /// <summary>0 for the first day of the week, 6 for the last.</summary>
    public static int Position(DayOfWeek day, DayOfWeek first) => ((int)day - (int)first + 7) % 7;
}
