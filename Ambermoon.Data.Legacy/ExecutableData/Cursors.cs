using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    public struct Cursor
    {
        public short HotspotX { get; internal set; }
        public short HotspotY { get; internal set; }
        public Graphic Graphic { get; internal set; }
    }

    // German 1.05: 0x53be
    public class Cursors
    {
        public const int Count = 28;

        readonly List<Cursor> entries = new List<Cursor>(Count);

        public IReadOnlyList<Cursor> Entries => entries.AsReadOnly();

        internal Cursors(IDataReader dataReader)
        {
            var graphicReader = new GraphicReader();
            var graphicInfo = new GraphicInfo
            {
                Width = 16,
                Height = 16,
                Alpha = true,
                GraphicFormat = GraphicFormat.Palette3Bit,
                PaletteOffset = 24
            };

            Graphic ReadGraphic()
            {
                var graphic = new Graphic();

                graphicReader.ReadGraphic(graphic, dataReader, graphicInfo);

                return graphic;
            }

            for (int i = 0; i < Count; ++i)
            {
                entries.Add(new Cursor
                {
                    HotspotX = (short)dataReader.ReadWord(),
                    HotspotY = (short)dataReader.ReadWord(),
                    Graphic = ReadGraphic()
                });
            }
        }
    }
}
