using Ambermoon.Data.Enumerations;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    public class UIGraphics
    {
        readonly Dictionary<UIGraphic, Graphic> entries = new Dictionary<UIGraphic, Graphic>();

        public IReadOnlyDictionary<UIGraphic, Graphic> Entries => entries;

        internal UIGraphics(IDataReader dataReader)
        {
            var graphicReader = new GraphicReader();
            var graphicInfo = new GraphicInfo
            {
                Width = 16,
                Height = 6,
                Alpha = true,
                GraphicFormat = GraphicFormat.Palette3Bit,
                PaletteOffset = 24
            };

            Graphic ReadGraphic(IDataReader dataReader)
            {
                var graphic = new Graphic();

                graphicReader.ReadGraphic(graphic, dataReader, graphicInfo);

                return graphic;
            }

            dataReader.Position = 156;
            entries.Add(UIGraphic.DisabledOverlay16x6, ReadGraphic(dataReader));
            graphicInfo.Height = 16;
            // window frames
            entries.Add(UIGraphic.FrameUpperLeft, ReadGraphic(dataReader));
            entries.Add(UIGraphic.FrameLeft, ReadGraphic(dataReader));
            entries.Add(UIGraphic.FrameLowerLeft, ReadGraphic(dataReader));
            entries.Add(UIGraphic.FrameTop, ReadGraphic(dataReader));
            entries.Add(UIGraphic.FrameBottom, ReadGraphic(dataReader));
            entries.Add(UIGraphic.FrameUpperRight, ReadGraphic(dataReader));
            entries.Add(UIGraphic.FrameRight, ReadGraphic(dataReader));
            entries.Add(UIGraphic.FrameLowerRight, ReadGraphic(dataReader));

            // Palette 50 (same as items)
            dataReader.Position = 960;
            graphicInfo.GraphicFormat = GraphicFormat.Palette5Bit;
            graphicInfo.PaletteOffset = 0;
            for (int i = (int)UIGraphic.StatusDead; i <= (int)UIGraphic.StatusRangeAttack; ++i)
                entries.Add((UIGraphic)i, ReadGraphic(dataReader));

            graphicInfo.Width = 32;
            graphicInfo.Height = 29;
            graphicInfo.GraphicFormat = GraphicFormat.Palette5Bit;
            graphicInfo.PaletteOffset = 0;
            entries.Add(UIGraphic.Eagle, ReadGraphic(dataReader)); // Palette of the map (e.g. 1)
            graphicInfo.Height = 26;
            entries.Add(UIGraphic.Explosion, ReadGraphic(dataReader)); // Palette 50 (items)
            graphicInfo.Height = 23;
            graphicInfo.GraphicFormat = GraphicFormat.Palette3Bit;
            graphicInfo.PaletteOffset = 24;
            entries.Add(UIGraphic.Ouch, ReadGraphic(dataReader));

            graphicInfo.Width = 16;
            graphicInfo.Height = 60;
            entries.Add(UIGraphic.StarBlinkAnimation, ReadGraphic(dataReader));
            graphicInfo.Height = 40;
            entries.Add(UIGraphic.PlusBlinkAnimation, ReadGraphic(dataReader));
            graphicInfo.Height = 36;
            entries.Add(UIGraphic.LeftPortraitBorder, ReadGraphic(dataReader));
            entries.Add(UIGraphic.CharacterValueBarFrames, ReadGraphic(dataReader));
            entries.Add(UIGraphic.RightPortraitBorder, ReadGraphic(dataReader));
            graphicInfo.Height = 1;
            entries.Add(UIGraphic.SmallBorder1, ReadGraphic(dataReader));
            entries.Add(UIGraphic.SmallBorder2, ReadGraphic(dataReader));
            graphicInfo.Height = 16;
            for (int i = (int)UIGraphic.Candle; i <= (int)UIGraphic.Scroll; ++i)
                entries.Add((UIGraphic)i, ReadGraphic(dataReader));

            graphicInfo.Width = 32;
            graphicInfo.Height = 15;
            entries.Add(UIGraphic.Windchain, ReadGraphic(dataReader));
            graphicInfo.Height = 32;
            entries.Add(UIGraphic.MonsterEyeInactive, ReadGraphic(dataReader));
            entries.Add(UIGraphic.MonsterEyeActive, ReadGraphic(dataReader));
            entries.Add(UIGraphic.Night, ReadGraphic(dataReader));
            entries.Add(UIGraphic.Dusk, ReadGraphic(dataReader));
            entries.Add(UIGraphic.Day, ReadGraphic(dataReader));
            entries.Add(UIGraphic.Dawn, ReadGraphic(dataReader));

            dataReader.Position = 0x285C; // start at base button shape

            graphicInfo.Height = 17;
            entries.Add(UIGraphic.ButtonFrame, ReadGraphic(dataReader));
            entries.Add(UIGraphic.ButtonFramePressed, ReadGraphic(dataReader));
            graphicInfo.Height = 4;
            entries.Add(UIGraphic.DisabledOverlay32x4, ReadGraphic(dataReader));
            graphicInfo.Height = 32;
            entries.Add(UIGraphic.Compass, ReadGraphic(dataReader));
            graphicInfo.Height = 9;
            entries.Add(UIGraphic.Unknown, ReadGraphic(dataReader));
            graphicInfo.Height = 34;
            entries.Add(UIGraphic.Skull, ReadGraphic(dataReader));
            entries.Add(UIGraphic.EmptyCharacterSlot, ReadGraphic(dataReader));
        }
    }
}
