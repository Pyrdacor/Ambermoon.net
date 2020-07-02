using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;

namespace Ambermoon.Data
{
    public enum GraphicFormat
    {
        Palette5Bit,
        Palette4Bit,
        Palette3Bit,
        XRGB16,
        RGBA32,
    }

    public struct GraphicInfo
    {
        public GraphicFormat GraphicFormat;
        public int Width;
        public int Height;
        public Palette Palette;
        public bool Alpha;
        public int PaletteOffset;
    }

    public class Graphic
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public byte[] Data { get; set; }

        public static Graphic CreateCompoundGraphic(IEnumerable<Graphic> graphics)
        {
            return CreateCompoundGraphic(graphics.ToArray());
        }

        public static Graphic CreateCompoundGraphic(params Graphic[] graphics)
        {
            var compoundGraphic = new Graphic
            {
                Width = graphics.Select(g => g.Width).Sum(),
                Height = graphics.Select(g => g.Height).Max()
            };

            compoundGraphic.Data = new byte[compoundGraphic.Width * compoundGraphic.Height * 4];
            int graphicXOffset = 0;

            for (int i = 0; i < graphics.Length; ++i)
            {
                int graphicWidth = graphics[i].Width;

                for (int y = 0; y < graphics[i].Height; ++i)
                {
                    Buffer.BlockCopy(graphics[i].Data, y * graphicWidth * 4, compoundGraphic.Data, (graphicXOffset + y * compoundGraphic.Width) * 4, graphicWidth * 4);
                }

                graphicXOffset += graphicWidth;
            }

            return compoundGraphic;
        }
    }
}
