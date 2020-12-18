namespace Ambermoon.Data
{
    public interface IFont
    {
        Graphic GetGlyphGraphic(uint glyphIndex);
        Graphic GetDigitGlyphGraphic(uint glyphIndex);
    }
}
