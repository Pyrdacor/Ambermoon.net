namespace Ambermoon.Data
{
    public interface IFont
    {
        /// <summary>
        /// Height of the glyph in game pixels.
        /// 
        /// Note that this is not the actual graphic size
        /// but the size which is used inside the game to
        /// place text. For example a high resolution font
        /// which uses double the pixels, still should not
        /// double this value.
        /// </summary>
        int GlyphHeight { get; }
        int GlyphCount { get; }
        Graphic GetGlyphGraphic(uint glyphIndex);
        Graphic GetDigitGlyphGraphic(uint glyphIndex);
    }
}
