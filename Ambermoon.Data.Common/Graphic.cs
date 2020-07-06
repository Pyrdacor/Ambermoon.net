using System;

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
        public bool Alpha;
        public byte PaletteOffset;

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
        public bool IndexedGraphic { get; set; }
    }
}
