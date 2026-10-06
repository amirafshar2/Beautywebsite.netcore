using BeautyByNegin.Business.Localization;
using BeautyByNegin.Business.Settings;

namespace BeautyByNegin.Business.Content;

/// <summary>Clears every cached part of the public site. Called by the admin panel after each save.</summary>
public interface ISiteCache
{
    void InvalidateAll();
}

public sealed class SiteCache(
    ILanguageService languages,
    ITextService texts,
    ISettingsService settings,
    ILayoutService layout,
    IContentService content) : ISiteCache
{
    public void InvalidateAll()
    {
        languages.Invalidate();
        settings.Invalidate();
        texts.Invalidate();
        layout.Invalidate();
        content.Invalidate();
    }
}
