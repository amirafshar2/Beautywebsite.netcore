namespace BeautyByNegin.Web.Areas.Admin;

/// <summary>
/// Texts of the admin panel in its four languages (fa default, tr, de, en).
/// The panel language is separate from the site's content languages.
/// Values live in PanelText.Data.cs as P("key", fa, tr, de, en).
/// </summary>
public static partial class PanelText
{
    private static readonly Dictionary<string, string[]> Values = Build();

    private static readonly string[] Order = ["fa", "tr", "de", "en"];

    public static string Get(string key, string lang)
    {
        if (!Values.TryGetValue(key, out var v)) return key;
        var i = Array.IndexOf(Order, lang);
        var text = i >= 0 ? v[i] : v[3];
        return string.IsNullOrEmpty(text) ? v[3] : text;
    }

    public static bool Has(string key) => Values.ContainsKey(key);

    private static Dictionary<string, string[]> Build()
    {
        var d = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var e in Entries()) d[e.Key] = [e.Fa, e.Tr, e.De, e.En];
        return d;
    }

    private sealed record Entry(string Key, string Fa, string Tr, string De, string En);

    private static Entry P(string key, string fa, string tr, string de, string en) => new(key, fa, tr, de, en);
}
