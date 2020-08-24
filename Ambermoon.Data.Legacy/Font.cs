using System;

namespace Ambermoon.Data.Legacy
{
    internal class Font : IFont
    {
        readonly ExecutableData.Glyphs glyphs;

        public Font(ExecutableData.Glyphs glyphs)
        {
            this.glyphs = glyphs;
        }

        public Graphic GetGlyphGraphic(uint glyphIndex)
        {
            if (glyphIndex > 93)
                throw new IndexOutOfRangeException($"Glyph index {glyphIndex} is out of range. Should be in the range of 0 to 93.");

            return glyphs.Entries[(int)glyphIndex];
        }
    }
}
