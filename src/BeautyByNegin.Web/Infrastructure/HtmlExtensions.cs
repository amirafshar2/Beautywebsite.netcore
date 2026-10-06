using System.Text.Encodings.Web;
using BeautyByNegin.Business.Media;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BeautyByNegin.Web.Infrastructure;

public static class HtmlExtensions
{
    /// <summary>
    /// Responsive &lt;img&gt; with WebP srcset/sizes, width/height (no layout shift) and lazy loading.
    /// Use <paramref name="priority"/> for the hero image (eager + fetchpriority=high).
    /// </summary>
    public static IHtmlContent Img(this IHtmlHelper _, ImageView image, string sizes = "100vw",
        string? alt = null, bool priority = false, string? cssClass = null)
    {
        var enc = HtmlEncoder.Default;
        var html = new System.Text.StringBuilder("<img");
        html.Append(" src=\"").Append(enc.Encode(image.Src)).Append('"');
        if (!string.IsNullOrEmpty(image.SrcSet))
        {
            html.Append(" srcset=\"").Append(enc.Encode(image.SrcSet)).Append('"');
            html.Append(" sizes=\"").Append(enc.Encode(sizes)).Append('"');
        }
        html.Append(" width=\"").Append(image.Width).Append("\" height=\"").Append(image.Height).Append('"');
        html.Append(" alt=\"").Append(enc.Encode(alt ?? image.Alt)).Append('"');
        if (priority) html.Append(" fetchpriority=\"high\" decoding=\"async\"");
        else html.Append(" loading=\"lazy\" decoding=\"async\"");
        if (cssClass is not null) html.Append(" class=\"").Append(enc.Encode(cssClass)).Append('"');
        html.Append(" />");
        return new HtmlString(html.ToString());
    }

    /// <summary>Plain multi-line text -> paragraphs (HTML-encoded).</summary>
    public static IHtmlContent Paragraphs(this IHtmlHelper _, string? text, string? cssClass = null)
    {
        if (string.IsNullOrWhiteSpace(text)) return HtmlString.Empty;
        var enc = HtmlEncoder.Default;
        var cls = cssClass is null ? "" : $" class=\"{enc.Encode(cssClass)}\"";
        return new HtmlString(string.Concat(text.Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => $"<p{cls}>{enc.Encode(p.Trim())}</p>")));
    }
}
