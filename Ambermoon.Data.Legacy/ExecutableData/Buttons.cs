using Ambermoon.Data.Enumerations;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    // Note: The cursors (like eye, hand, etc) don't use the same images
    // as the buttons! This is also true for arrows etc.
    public class Buttons
    {
        readonly Dictionary<ButtonType, Graphic> entries = new Dictionary<ButtonType, Graphic>();

        public IReadOnlyDictionary<ButtonType, Graphic> Entries => entries;

        internal Buttons(IDataReader dataReaderFirstHunk, IDataReader dataReaderSecondHunk)
        {
            dataReaderFirstHunk.Position = 0x2860; // base button shape

            var graphicInfo = new GraphicInfo
            {
                Width = 32,
                Height = 17,
                Alpha = false,
                GraphicFormat = GraphicFormat.Palette3Bit,
                PaletteOffset = 24
            };
            var graphicReader = new GraphicReader();

            Graphic ReadGraphic(IDataReader dataReader)
            {
                var graphic = new Graphic();

                graphicReader.ReadGraphic(graphic, dataReader, graphicInfo);

                return graphic;
            }

            var baseShape = ReadGraphic(dataReaderFirstHunk);
            var pressedShape = ReadGraphic(dataReaderFirstHunk);

            baseShape.ReplaceColor(0, 28);
            pressedShape.ReplaceColor(0, 28);

            graphicInfo.Alpha = true;

            //Graphic CreateButtonGraphic()

            // TODO ...
        }
    }
}
