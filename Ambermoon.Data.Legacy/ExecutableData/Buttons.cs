using Ambermoon.Data.Enumerations;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    // Note: The cursors (like eye, hand, etc) don't use the same images
    // as the buttons! This is also true for arrows etc.
    // There are 78 buttons, 32x13 pixels each. They have transparent areas
    // left and right and they only contain the inner
    // graphic. The button frame is drawn separately.
    public class Buttons
    {
        readonly Dictionary<ButtonType, Graphic> entries = new Dictionary<ButtonType, Graphic>();

        public IReadOnlyDictionary<ButtonType, Graphic> Entries => entries;

        // Second data hunk, right behind UITexts.
        internal Buttons(IDataReader dataReader)
        {
            var graphicInfo = new GraphicInfo
            {
                Width = 32,
                Height = 13,
                Alpha = true,
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

            foreach (var buttonType in Enum.GetValues<ButtonType>())
                entries.Add(buttonType, ReadGraphic(dataReader));

            dataReader.AlignToWord();
        }
    }
}
