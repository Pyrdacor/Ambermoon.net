namespace Ambermoon.Data.Pyrdacor.Objects
{
    internal class IngameFont : IFont
    {
        readonly Lazy<Font> font;
        readonly Lazy<Font> digitFont;

        public IngameFont(Func<Font> fontProvider, Func<Font> digitFontProvider)
        {
            font = new Lazy<Font>(fontProvider);
            digitFont = new Lazy<Font>(digitFontProvider);
        }

        public Graphic GetGlyphGraphic(uint glyphIndex) => font.Value.GetGlyphGraphic(glyphIndex);

        public Graphic GetDigitGlyphGraphic(uint glyphIndex) => digitFont.Value.GetGlyphGraphic(glyphIndex);
    }

    internal class IngameFontProvider : IFontProvider
    {
        private readonly IngameFont ingameFont;

        public IngameFontProvider(IngameFont ingameFont)
        {
            this.ingameFont = ingameFont;
        }

        public IFont GetFont() => ingameFont;
    }
}
