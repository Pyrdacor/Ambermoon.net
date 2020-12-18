using Ambermoon.Data.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    // German 1.05: 0x4315 to 0x4346 has the small digits (4 msb in each byte are the 4 pixels of each of the 5 lines of a digit)
    public class DigitGlyphs
    {
        public const int Count = 10;

        readonly List<Graphic> entries = new List<Graphic>(Count);

        public IReadOnlyList<Graphic> Entries => entries.AsReadOnly();

        internal DigitGlyphs(IDataReader dataReader)
        {
            for (int i = 0; i < Count; ++i)
            {
                // Each glyph is stored as 5 bytes.
                // Each byte provides pixel data in
                // their 4 most-significant bits.
                // But we read 5 to add some space.
                // So a digit glyph is 5x5 pixels in size.

                var graphic = new Graphic
                {
                    Width = 5,
                    Height = 5,
                    Data = new byte[5 * 5],
                    IndexedGraphic = true
                };

                // We will use 2 color indices (index 0 -> transparent, index 1 -> text color).
                // When rendering the text index 1 should be replaced by the text color.
                for (int y = 0; y < 5; ++y)
                {
                    byte line = dataReader.ReadByte();

                    for (int x = 0; x < 5; ++x)
                    {
                        graphic.Data[x + y * 5] = (byte)((line & 0x80) >> 7);
                        line <<= 1;
                    }
                }

                entries.Add(graphic);
            }
        }
    }
}
