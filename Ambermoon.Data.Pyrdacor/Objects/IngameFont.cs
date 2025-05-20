namespace Ambermoon.Data.Pyrdacor.Objects;

internal class IngameFont(Func<Font> fontProvider, Func<Font> digitFontProvider) : IFont
{
    readonly Lazy<Font> font = new(fontProvider);
    readonly Lazy<Font> digitFont = new(digitFontProvider);

    public int GlyphCount => font.Value.GlyphCount;
    public int GlyphHeight => font.Value.GlyphHeight;

    public Graphic GetGlyphGraphic(uint glyphIndex) => font.Value.GetGlyphGraphic(glyphIndex);

    public Graphic GetDigitGlyphGraphic(uint glyphIndex) => digitFont.Value.GetGlyphGraphic(glyphIndex);
}

internal class IngameFontProvider(IngameFont ingameFont) : IFontProvider
{
    public IFont GetFont() => ingameFont;
}
