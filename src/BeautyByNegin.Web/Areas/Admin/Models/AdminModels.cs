using BeautyByNegin.Business.Localization;

namespace BeautyByNegin.Web.Areas.Admin.Models;

/// <summary>Tab header FA | TR | DE | EN | AR; Missing = languages without text (orange dot).</summary>
public sealed record LangTabs(string Group, IReadOnlyList<SiteLanguage> Languages, ISet<string> Missing);

/// <summary>Image picker with crop: hidden input <see cref="Name"/> holds the MediaImage id.</summary>
public sealed record ImageField(
    string Name,
    int? Value,
    string? PreviewUrl,
    string LabelKey,
    string HintKey,
    /// <summary>Crop ratio, e.g. 0.8 (4:5), 1.7778 (16:9), 1, or 0 for free cropping.</summary>
    double Ratio,
    string? Id = null);

/// <summary>Small schematic of the site page with the edited area highlighted ("Where does this appear?").</summary>
public sealed record WhereOnSite(string Area);
