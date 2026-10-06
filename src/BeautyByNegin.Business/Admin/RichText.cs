using Ganss.Xss;

namespace BeautyByNegin.Business.Admin;

/// <summary>Cleans HTML from the panel editor: only simple formatting survives (no scripts, styles, iframes).</summary>
public static class RichText
{
    private static readonly HtmlSanitizer Sanitizer = Create();

    private static HtmlSanitizer Create()
    {
        var s = new HtmlSanitizer();
        s.AllowedTags.Clear();
        foreach (var tag in new[] { "p", "br", "h2", "h3", "strong", "b", "em", "i", "u", "a", "ul", "ol", "li", "blockquote" })
            s.AllowedTags.Add(tag);
        s.AllowedAttributes.Clear();
        s.AllowedAttributes.Add("href");
        s.AllowedSchemes.Clear();
        foreach (var scheme in new[] { "http", "https", "mailto", "tel" }) s.AllowedSchemes.Add(scheme);
        s.AllowedCssProperties.Clear();
        s.AllowedClasses.Clear();
        s.PostProcessNode += (_, e) =>
        {
            if (e.Node is AngleSharp.Html.Dom.IHtmlAnchorElement a && a.Href.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                a.SetAttribute("rel", "noopener");
        };
        s.AllowedAttributes.Add("rel");
        return s;
    }

    public static string? Clean(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return null;
        var clean = Sanitizer.Sanitize(html).Trim();
        // Quill leaves "<p><br></p>" for an empty editor
        return clean is "" or "<p><br></p>" ? null : clean;
    }
}
