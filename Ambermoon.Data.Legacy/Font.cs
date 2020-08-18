using System;

namespace Ambermoon.Data.Legacy
{
    internal class Font : IFont
    {
        public Graphic GetGlyphGraphic(uint glyphIndex)
        {
            if (glyphIndex > 93)
                throw new IndexOutOfRangeException($"Glyph index {glyphIndex} is out of range. Should be in the range of 0 to 93.");

            var graphic = new Graphic
            {
                Width = 6,
                Height = 6,
                Data = new byte[6 * 6],
                IndexedGraphic = true
            };

            // We will use 2 color indices (index 0 -> transparent, index 1 -> text color).
            // When rendering the text index 1 should be replaced by the text color.
            // The text shadow should be rendered as black text with offset 1,1.
            for (int y = 0; y < 5; ++y)
            {
                for (int x = 0; x < 6; ++x)
                {
                    graphic.Data[x + y * 6] = Glyphs.GlyphData[glyphIndex * 30 + x + y * 6];
                }
            }

            return graphic;
        }
    }
}
