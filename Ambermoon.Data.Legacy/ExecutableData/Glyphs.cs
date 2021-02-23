using Ambermoon.Data.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    // German 1.05: 0x51e8
    public class Glyphs
    {
        public const int Count = 94;

        readonly List<Graphic> entries = new List<Graphic>(Count);

        public IReadOnlyList<Graphic> Entries => entries.AsReadOnly();

        internal Glyphs(IDataReader dataReader)
        {
            for (int i = 0; i < Count; ++i)
            {
                // Each glyph is stored as 5 bytes.
                // Each byte provides pixel data in
                // their 5 most-significant bits.
                // But we read 6 to add some space.
                // So a glyph is 6x5 pixels in size.
                // We add an empty bottom line too.

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
                    byte line = dataReader.ReadByte();

                    for (int x = 0; x < 6; ++x)
                    {
                        graphic.Data[x + y * 6] = (byte)((line & 0x80) >> 7);
                        line <<= 1;
                    }
                }

                entries.Add(graphic);
            }
        }
    }
}
