using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
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
                Height = 16,
                Alpha = true,
                GraphicFormat = GraphicFormat.Palette3Bit,
                PaletteOffset = 24
            };

            Graphic ReadGraphic(IDataReader dataReader, byte maskColor = 0)
            {
                var graphic = new Graphic();

                graphicReader.ReadGraphic(graphic, dataReader, graphicInfo, maskColor);

                return graphic;
            }

            Graphic ReadOpaqueGraphic(IDataReader dataReader)
            {
                var graphic = new Graphic();

                graphicReader.ReadGraphic(graphic, dataReader, graphicInfo);
                graphic.ReplaceColor(0, 32);

                return graphic;
            }

            // Now 32 bytes follow where each of the 256 bits indicates (1) black or (0) transparent.
            var graphicData = new byte[256];
            for (int y = 0; y < 16; ++y)
            {
                var bits = dataReader.ReadWord();

                for (int x = 0; x < 16; ++x)
                {
                    if ((bits & 0x8000) != 0)
                        graphicData[y * 16 + x] = 28;
                    bits <<= 1;
                }
            }
            entries.Add(UIGraphic.DisabledOverlay16x16, new Graphic
            {
                Data = graphicData,
                Width = 16,
                Height = 16,
                IndexedGraphic = true
            });
            // Then real 3-bit graphics follow.
            // window frames
            entries.Add(UIGraphic.FrameUpperLeft, ReadGraphic(dataReader));
            entries.Add(UIGraphic.FrameLeft, ReadGraphic(dataReader));
            entries.Add(UIGraphic.FrameLowerLeft, ReadGraphic(dataReader));
            entries.Add(UIGraphic.FrameTop, ReadGraphic(dataReader));
            entries.Add(UIGraphic.FrameBottom, ReadGraphic(dataReader));
            entries.Add(UIGraphic.FrameUpperRight, ReadGraphic(dataReader));
            entries.Add(UIGraphic.FrameRight, ReadGraphic(dataReader));
            entries.Add(UIGraphic.FrameLowerRight, ReadGraphic(dataReader));

            graphicInfo.GraphicFormat = GraphicFormat.Palette5Bit;
            graphicInfo.PaletteOffset = 0;
            for (int i = (int)UIGraphic.StatusDead; i <= (int)UIGraphic.StatusRangeAttack; ++i)
            {
                entries.Add((UIGraphic)i, ReadGraphic(dataReader));
            }

            graphicInfo.Width = 32;
            graphicInfo.Height = 29;
            graphicInfo.GraphicFormat = GraphicFormat.Palette5Bit;
            graphicInfo.PaletteOffset = 0;
            entries.Add(UIGraphic.Eagle, ReadGraphic(dataReader));
            graphicInfo.Height = 26;
            entries.Add(UIGraphic.DamageSplash, ReadGraphic(dataReader));
            graphicInfo.Height = 23;
            graphicInfo.GraphicFormat = GraphicFormat.Palette3Bit;
            graphicInfo.PaletteOffset = 24;
            entries.Add(UIGraphic.Ouch, ReadGraphic(dataReader));
            graphicInfo.Width = 16;
            graphicInfo.Height = 9;
            graphicInfo.GraphicFormat = GraphicFormat.Palette5Bit;
            graphicInfo.PaletteOffset = 0;
            var frames = new Graphic(64, 9, 0);
            for (uint i = 0; i < 4; ++i)
                frames.AddOverlay(i * 16u, 0u, ReadGraphic(dataReader), false);
            entries.Add(UIGraphic.StarBlinkAnimation, frames);
            graphicInfo.GraphicFormat = GraphicFormat.Palette3Bit;
            graphicInfo.PaletteOffset = 24;
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
            for (int i = (int)UIGraphic.Candle; i <= (int)UIGraphic.Map; ++i)
                entries.Add((UIGraphic)i, ReadOpaqueGraphic(dataReader));

            graphicInfo.Width = 32;
            graphicInfo.Height = 15;
            entries.Add(UIGraphic.Windchain, ReadOpaqueGraphic(dataReader));
            graphicInfo.Height = 32;
            entries.Add(UIGraphic.MonsterEyeInactive, ReadOpaqueGraphic(dataReader));
            entries.Add(UIGraphic.MonsterEyeActive, ReadOpaqueGraphic(dataReader));
            entries.Add(UIGraphic.Night, ReadOpaqueGraphic(dataReader));
            entries.Add(UIGraphic.Dusk, ReadOpaqueGraphic(dataReader));
            entries.Add(UIGraphic.Day, ReadOpaqueGraphic(dataReader));
            entries.Add(UIGraphic.Dawn, ReadOpaqueGraphic(dataReader));

            graphicInfo.Height = 17;
            graphicInfo.Alpha = true;
            entries.Add(UIGraphic.ButtonFrame, ReadGraphic(dataReader));
            entries.Add(UIGraphic.ButtonFramePressed, ReadGraphic(dataReader));
            // Note: There is a 1-bit mask here where a 0 bit means transparent (keep color) and 1 means overlay.
            // As we use this for buttons we will set the color as the button back color (28).
            // The disable overlay is 32x11 in size.
            var disableOverlay = new Graphic(32, 11, 0);
            for (int y = 0; y < 11; ++y)
            {
                var bits = dataReader.ReadDword();

                for (int x = 0; x < 32; ++x)
                {
                    if ((bits & 0x80000000) != 0)
                        disableOverlay.Data[y * 32 + x] = 28;
                    bits <<= 1;
                }
            }
            entries.Add(UIGraphic.ButtonDisabledOverlay, disableOverlay);
            graphicInfo.Width = 32;
            graphicInfo.Height = 32;
            entries.Add(UIGraphic.Compass, ReadOpaqueGraphic(dataReader));
            graphicInfo.Width = 16;
            graphicInfo.Height = 9;
            entries.Add(UIGraphic.Attack, ReadGraphic(dataReader));
            entries.Add(UIGraphic.Defense, ReadGraphic(dataReader));
            graphicInfo.Width = 32;
            graphicInfo.Height = 34;
            entries.Add(UIGraphic.Skull, ReadGraphic(dataReader, 25));
            entries.Add(UIGraphic.EmptyCharacterSlot, ReadOpaqueGraphic(dataReader));
            graphicInfo.Width = 16;
            graphicInfo.Height = 16;
            graphicInfo.GraphicFormat = GraphicFormat.Palette3Bit;
            graphicInfo.PaletteOffset = 0;
            var compoundGraphic = new Graphic(176, 16, 0);
            for (uint i = 0; i < 11; ++i)
                compoundGraphic.AddOverlay(i * 16u, 0u, ReadGraphic(dataReader), false);
            entries.Add(UIGraphic.ItemConsume, compoundGraphic);
            graphicInfo.Width = 32;
            graphicInfo.Height = 29;
            graphicInfo.GraphicFormat = GraphicFormat.Palette5Bit;
            graphicInfo.PaletteOffset = 0;
            entries.Add(UIGraphic.Talisman, ReadGraphic(dataReader));
            var unused = new Graphic(16, 13, 0);
            for (int y = 0; y < 13; ++y)
            {
                var bits = dataReader.ReadWord();

                for (int x = 0; x < 16; ++x)
                {
                    if ((bits & 0x8000) != 0)
                        unused.Data[y * 16 + x] = 28;
                    bits <<= 1;
                }
            }
            entries.Add(UIGraphic.Unused, unused);
            var brokenOverlay = new Graphic(16, 16, 0);
            for (int y = 0; y < 16; ++y)
            {
                var bits = dataReader.ReadWord();

                for (int x = 0; x < 16; ++x)
                {
                    if ((bits & 0x8000) != 0)
                        brokenOverlay.Data[y * 16 + x] = 26;
                    bits <<= 1;
                }
            }
            entries.Add(UIGraphic.BrokenItemOverlay, brokenOverlay); // TODO: use it
        }
    }
}
