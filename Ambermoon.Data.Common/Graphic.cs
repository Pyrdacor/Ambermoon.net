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
        RGBA32
    }

    public struct GraphicInfo
    {
        public GraphicFormat GraphicFormat;
        public int Width;
        public int Height;
        public Palette Palette;
        public bool Alpha;
        public int PaletteOffset;

        public int BitsPerPixel => GraphicFormat switch
        {
            GraphicFormat.Palette5Bit => 5,
            GraphicFormat.Palette4Bit => 4,
            GraphicFormat.Palette3Bit => 3,
            GraphicFormat.XRGB16 => 16,
            GraphicFormat.RGBA32 => 32,
            _ => throw new ArgumentOutOfRangeException("Invalid graphic format")
        };

        public int DataSize => (Width * Height * BitsPerPixel + 7) / 8;
    }

    public class Graphic
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public byte[] Data { get; set; }

        // Note: All tiles must have same dimensions
        public static Graphic CreateTilesetGraphic(int tilesPerRow, IEnumerable<Graphic> graphics)
        {
            return CreateTilesetGraphic(tilesPerRow, graphics.ToArray());
        }

        public static Graphic CreateCompoundGraphic(IEnumerable<Graphic> graphics)
        {
            return CreateCompoundGraphic(graphics.ToArray());
        }

        public static Graphic CreateTilesetGraphic(int tilesPerRow, params Graphic[] graphics)
        {
            if (graphics.Length == 0)
                return new Graphic();

            if (tilesPerRow > graphics.Length)
                tilesPerRow = graphics.Length;

            int tileRows = (graphics.Length + tilesPerRow - 1) / tilesPerRow;
            int tileWidth = graphics[0].Width;
            int tileHeight = graphics[0].Height;
            var tilesetGraphic = new Graphic
            {
                Width = tilesPerRow * tileWidth,
                Height = tileRows * tileHeight
            };

            tilesetGraphic.Data = new byte[tilesetGraphic.Width * tilesetGraphic.Height * 4];
            int graphicXOffset = 0;
            int graphicYOffset = 0;

            for (int i = 0; i < graphics.Length; ++i)
            {
                for (int y = 0; y < tileHeight; ++y)
                {
                    Buffer.BlockCopy(graphics[i].Data, y * tileWidth * 4,
                        tilesetGraphic.Data, (graphicXOffset + (graphicYOffset + y) * tilesetGraphic.Width) * 4, tileWidth * 4);
                }

                graphicXOffset += tileWidth;

                if ((i + 1) % tilesPerRow == 0)
                {
                    graphicXOffset = 0;
                    graphicYOffset += tileHeight;
                }
            }

            return tilesetGraphic;
        }

        public static Graphic CreateCompoundGraphic(params Graphic[] graphics)
        {
            if (graphics.Length == 0)
                return new Graphic();

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

                for (int y = 0; y < graphics[i].Height; ++y)
                {
                    Buffer.BlockCopy(graphics[i].Data, y * graphicWidth * 4, compoundGraphic.Data, (graphicXOffset + y * compoundGraphic.Width) * 4, graphicWidth * 4);
                }

                graphicXOffset += graphicWidth;
            }

            return compoundGraphic;
        }
    }
}
