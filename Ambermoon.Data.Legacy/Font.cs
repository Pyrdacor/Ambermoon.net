using System;

namespace Ambermoon.Data.Legacy
{
    internal class Font : IFont
    {
        readonly ExecutableData.Glyphs glyphs;
        readonly ExecutableData.DigitGlyphs digitGlyphs;

        public int GlyphCount => glyphs.Entries.Count;

        public int GlyphHeight => 7;

        public Font(ExecutableData.Glyphs glyphs, ExecutableData.DigitGlyphs digitGlyphs)
        {
            this.glyphs = glyphs;
            this.digitGlyphs = digitGlyphs;
        }

        public Graphic GetGlyphGraphic(uint glyphIndex)
        {
            if (glyphIndex > 93)
                throw new IndexOutOfRangeException($"Glyph index {glyphIndex} is out of range. Should be in the range of 0 to 93.");

            return glyphs.Entries[(int)glyphIndex];
        }

        public Graphic GetDigitGlyphGraphic(uint glyphIndex)
        {
            if (glyphIndex > 9)
                throw new IndexOutOfRangeException($"Digit glyph index {glyphIndex} is out of range. Should be in the range of 0 to 9.");

            return digitGlyphs.Entries[(int)glyphIndex];
        }
    }
}
