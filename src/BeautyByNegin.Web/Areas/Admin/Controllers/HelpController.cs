using Microsoft.AspNetCore.Mvc;

namespace BeautyByNegin.Web.Areas.Admin.Controllers;

/// <summary>Shows the simple Persian user guide (docs/panel-rehberi-fa.md, copied next to the app).</summary>
public class HelpController(IWebHostEnvironment env) : AdminController
{
    [HttpGet]
    public IActionResult Index()
    {
        var candidates = new[]
        {
            Path.Combine(env.ContentRootPath, "docs", "panel-rehberi-fa.md"),
            Path.Combine(env.ContentRootPath, "..", "..", "docs", "panel-rehberi-fa.md"),
        };
        var path = candidates.FirstOrDefault(System.IO.File.Exists);
        ViewData["Html"] = path is null ? null : Markdown.ToHtml(System.IO.File.ReadAllText(path));
        return View();
    }
}

/// <summary>Tiny Markdown renderer for the guide (headings, lists, bold, paragraphs). Input is our own file.</summary>
public static class Markdown
{
    public static string ToHtml(string md)
    {
        var sb = new System.Text.StringBuilder();
        string? list = null;
        void CloseList() { if (list is not null) { sb.Append($"</{list}>"); list = null; } }
        static string Inline(string s)
        {
            var e = System.Net.WebUtility.HtmlEncode(s);
            e = System.Text.RegularExpressions.Regex.Replace(e, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
            e = System.Text.RegularExpressions.Regex.Replace(e, @"`(.+?)`", "<code>$1</code>");
            return e;
        }
        foreach (var raw in md.Replace("\r", "").Split('\n'))
        {
            var line = raw.TrimEnd();
            if (line.Length == 0) { CloseList(); continue; }
            if (line.StartsWith("### ")) { CloseList(); sb.Append($"<h3>{Inline(line[4..])}</h3>"); }
            else if (line.StartsWith("## ")) { CloseList(); sb.Append($"<h2>{Inline(line[3..])}</h2>"); }
            else if (line.StartsWith("# ")) { CloseList(); sb.Append($"<h1>{Inline(line[2..])}</h1>"); }
            else if (line.StartsWith("- ") || line.StartsWith("* "))
            {
                if (list != "ul") { CloseList(); sb.Append("<ul>"); list = "ul"; }
                sb.Append($"<li>{Inline(line[2..])}</li>");
            }
            else if (System.Text.RegularExpressions.Regex.IsMatch(line, @"^\d+\.\s"))
            {
                if (list != "ol") { CloseList(); sb.Append("<ol>"); list = "ol"; }
                sb.Append($"<li>{Inline(line[(line.IndexOf('.') + 1)..].Trim())}</li>");
            }
            else if (line.StartsWith("> ")) { CloseList(); sb.Append($"<p class=\"hint-note\">{Inline(line[2..])}</p>"); }
            else { CloseList(); sb.Append($"<p>{Inline(line)}</p>"); }
        }
        CloseList();
        return sb.ToString();
    }
}
