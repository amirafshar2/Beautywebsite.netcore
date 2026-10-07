using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using BeautyByNegin.Business.Settings;

namespace BeautyByNegin.Business.Content;

/// <summary>A group of site colors that change together (e.g. "all browns"), with ~20 matching suggestions.</summary>
public sealed record ColorGroup(string Key, string Default, IReadOnlyList<string> Presets);

/// <summary>
/// Site colors chosen in the panel ("Colors"). Each group is one setting ("theme.&lt;key&gt;") from which all
/// related shades are derived, so e.g. every brown on the site follows the chosen brown.
/// Only changed groups are written to /theme.css, which overrides the defaults of site.css.
/// Text on buttons is chosen automatically (light or dark) so it always stays readable.
/// </summary>
public static class Theme
{
    public const string SettingPrefix = "theme.";

    public static readonly IReadOnlyList<ColorGroup> Groups =
    [
        // Browns: small labels, numbers, links, icons, hover of buttons
        new("brown", "#8A6D5A", ["#8A6D5A", "#785C4A", "#6B4F3F", "#9C7A63", "#7A5F4D", "#8C6A52", "#A0826D", "#6E5546", "#7D6450", "#94735C",
                                  "#5E4A3D", "#8B5E4A", "#9E6B57", "#7B6151", "#876A5C", "#6F5B4E", "#A47C66", "#806052", "#735A49", "#957566"]),
        // Creams: page background and all light surfaces (cards, header, forms)
        new("cream", "#F5F0E8", ["#F5F0E8", "#FBF8F3", "#F7F3EE", "#F3ECE2", "#EFE6D8", "#F6EFE9", "#F8F1F1", "#F2EDE6", "#EEF0EA", "#F0F2F4",
                                  "#FFFFFF", "#FAF7F2", "#F4EEE3", "#EDE3D3", "#F5EBE0", "#EFE9E4", "#F1EFE8", "#F7F4EF", "#EAE4DA", "#F3F0EA"]),
        // Dark backgrounds: footer, consultation block, chat header, hero behind the photo
        new("darkBg", "#3A2E2B", ["#3A2E2B", "#2B211E", "#4A3428", "#4E3B31", "#5C4033", "#3D3027", "#42352F", "#5E5149", "#4A2E3A", "#5A2332",
                                   "#6D2E3B", "#3F4232", "#2F3D33", "#24303F", "#36404A", "#2E2E2E", "#1C1A19", "#1F3B3D", "#6E4B4B", "#2A2A35"]),
        // Text on light backgrounds
        new("textLight", "#3A2E2B", ["#3A2E2B", "#2B211E", "#1C1A19", "#333333", "#4A3428", "#2F3D33", "#24303F", "#36404A", "#5A2332", "#4E3B31",
                                      "#3F4232", "#2E2E2E", "#42352F", "#3B3B3B", "#1F3B3D", "#4A2E3A", "#5C4033", "#222222", "#3D3027", "#2A2A35"]),
        // Text on dark backgrounds
        new("textDark", "#FBF8F3", ["#FBF8F3", "#FFFFFF", "#F5F0E8", "#EFE6D8", "#F3ECE2", "#F8F1F1", "#EEF0EA", "#F0F2F4", "#E9DFD0", "#F6EFE9",
                                     "#FAF7F2", "#EDE3D3", "#F4EEE3", "#E8E2D8", "#F7F4EF", "#DCCFBF", "#F2E6D9", "#EFE9E4", "#E6DCCB", "#FFF8EE"]),
        // Buttons on light backgrounds ("Book an appointment", chat button, selected filters)
        new("btnLight", "#3A2E2B", ["#3A2E2B", "#2B211E", "#4A3428", "#5C4033", "#6B4F3F", "#8A6D5A", "#785C4A", "#5A2332", "#6D2E3B", "#4A2E3A",
                                     "#2F3D33", "#3F4232", "#24303F", "#36404A", "#1F3B3D", "#2E2E2E", "#1C1A19", "#8C4A36", "#A67C52", "#9C6B5B"]),
        // Buttons on dark backgrounds (hero, consultation block)
        new("btnDark", "#FBF8F3", ["#FBF8F3", "#FFFFFF", "#F5F0E8", "#EFE6D8", "#F3ECE2", "#E9DFD0", "#DCCFBF", "#C2A878", "#D4B483", "#E0C9A6",
                                    "#CBB28A", "#B8976A", "#F6EFE9", "#F8F1F1", "#EEF0EA", "#F0F2F4", "#E8D5C4", "#D9BF8C", "#C9A96E", "#F2E6D9"]),
        // Gold accent: thin lines, numbers, active language, focus ring
        new("gold", "#C2A878", ["#C2A878", "#B8976A", "#D4B483", "#A88A5C", "#C9A96E", "#BFA27A", "#D8C3A5", "#B39069", "#CBB28A", "#A67C52",
                                 "#E0C9A6", "#9E8466", "#C4A484", "#B5A07A", "#D1B48C", "#A89060", "#C8B08A", "#BC9B6A", "#D9BF8C", "#AD9171"]),
    ];

    public static string SettingKey(string group) => SettingPrefix + group;

    /// <summary>Chosen color of a group (valid #RRGGBB) or its default.</summary>
    public static string Value(SiteSettings s, ColorGroup g)
        => TryParse(s.Get(SettingKey(g.Key)), out _) ? Normalize(s.Get(SettingKey(g.Key))!) : g.Default;

    public static bool IsChanged(SiteSettings s) => Groups.Any(g => !string.Equals(Value(s, g), g.Default, StringComparison.OrdinalIgnoreCase));

    /// <summary>CSS that overrides the colors of site.css (empty when nothing was changed).</summary>
    public static string BuildCss(SiteSettings s)
    {
        if (!IsChanged(s)) return "";
        string V(string key) => Value(s, Groups.First(g => g.Key == key));
        var brown = V("brown"); var cream = V("cream"); var darkBg = V("darkBg"); var text = V("textLight");
        var onDark = V("textDark"); var btn = V("btnLight"); var btnDark = V("btnDark"); var gold = V("gold");

        var css = new StringBuilder("/* Site colors chosen in the admin panel */\n:root {\n");
        void Var(string name, string value) => css.Append("  --").Append(name).Append(": ").Append(value).Append(";\n");

        // light surfaces, all derived from the chosen cream
        Var("cream", cream);
        Var("ivory", Mix(cream, "#FFFFFF", .55));
        Var("sand", Mix(cream, brown, .30));
        Var("cream-2", Mix(cream, brown, .08));
        Var("cream-3", Mix(cream, brown, .11));
        // text on light
        Var("text", text);
        Var("text-soft", Readable(Mix(text, cream, .28), cream));
        // browns
        Var("mocha", brown);
        Var("mocha-text", Readable(brown, cream));
        Var("taupe", Mix(brown, cream, .35));
        // gold
        Var("champagne", gold);
        Var("gold-text", Readable(gold, cream));
        // dark sections and their text
        Var("bg-dark", darkBg);
        Var("on-dark", onDark);
        Var("chocolate", darkBg);
        // buttons (text color chosen automatically for contrast)
        Var("primary", btn);
        Var("on-primary", BestText(btn, onDark, text));
        Var("primary-hover", Luminance(btn) < .35 ? Mix(btn, brown, .55) : Mix(btn, "#000000", .15));
        Var("btn-on-dark", btnDark);
        Var("on-btn-on-dark", BestText(btnDark, onDark, text));
        css.Append("}\n");
        return css.ToString();
    }

    public static string Version(string css) => css.Length == 0 ? "0" : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(css)))[..10].ToLowerInvariant();

    // ---------------------------------------------------------------- color helpers

    public static bool TryParse(string? hex, out (double R, double G, double B) rgb)
    {
        rgb = default;
        if (string.IsNullOrWhiteSpace(hex)) return false;
        var h = hex.Trim().TrimStart('#');
        if (h.Length != 6 || !int.TryParse(h, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v)) return false;
        rgb = ((v >> 16) & 255, (v >> 8) & 255, v & 255);
        return true;
    }

    public static string Normalize(string hex) => "#" + hex.Trim().TrimStart('#').ToUpperInvariant();

    private static string Hex((double R, double G, double B) c)
        => $"#{(int)Math.Round(Math.Clamp(c.R, 0, 255)):X2}{(int)Math.Round(Math.Clamp(c.G, 0, 255)):X2}{(int)Math.Round(Math.Clamp(c.B, 0, 255)):X2}";

    /// <summary>a mixed with b; amount 0 = a, 1 = b.</summary>
    public static string Mix(string a, string b, double amount)
    {
        TryParse(a, out var x); TryParse(b, out var y);
        return Hex((x.R + (y.R - x.R) * amount, x.G + (y.G - x.G) * amount, x.B + (y.B - x.B) * amount));
    }

    public static double Luminance(string hex)
    {
        TryParse(hex, out var c);
        static double Ch(double v) { v /= 255; return v <= .03928 ? v / 12.92 : Math.Pow((v + .055) / 1.055, 2.4); }
        return .2126 * Ch(c.R) + .7152 * Ch(c.G) + .0722 * Ch(c.B);
    }

    public static double Contrast(string a, string b)
    {
        var l1 = Luminance(a); var l2 = Luminance(b);
        return (Math.Max(l1, l2) + .05) / (Math.Min(l1, l2) + .05);
    }

    /// <summary>Darkens (or lightens on dark backgrounds) a color until it is readable as small text (WCAG AA 4.5:1).</summary>
    public static string Readable(string color, string background)
    {
        var target = Luminance(background) > .4 ? "#000000" : "#FFFFFF";
        var c = color;
        for (var i = 0; i < 20 && Contrast(c, background) < 4.5; i++) c = Mix(c, target, .1);
        return c;
    }

    /// <summary>The more readable of the two text colors (or black/white if neither is readable).</summary>
    public static string BestText(string background, string light, string dark)
    {
        var best = Contrast(background, light) >= Contrast(background, dark) ? light : dark;
        if (Contrast(background, best) >= 4.5) return best;
        return Contrast(background, "#FFFFFF") >= Contrast(background, "#1A1A1A") ? "#FFFFFF" : "#1A1A1A";
    }
}
