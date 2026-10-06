using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BeautyByNegin.Business.Admin;

/// <summary>Builds "page addresses" (slugs): "Carboxy Facial" -> "carboxy-facial". Persian/Arabic letters are kept.</summary>
public static partial class Slug
{
    public static string From(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var normalized = text.Trim().ToLowerInvariant()
            .Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue").Replace("ß", "ss")
            .Replace("ı", "i").Replace("ş", "s").Replace("ğ", "g").Replace("ç", "c")
            .Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat == UnicodeCategory.NonSpacingMark) continue;           // accents
            if (char.IsLetterOrDigit(c)) sb.Append(c);
            else if (c is ' ' or '-' or '_' or '.' or '/' or '‌') sb.Append('-'); // ZWNJ in Persian
        }
        var slug = Dashes().Replace(sb.ToString().Normalize(NormalizationForm.FormC), "-").Trim('-');
        return slug.Length > 120 ? slug[..120].Trim('-') : slug;
    }

    [GeneratedRegex("-{2,}")]
    private static partial Regex Dashes();
}
