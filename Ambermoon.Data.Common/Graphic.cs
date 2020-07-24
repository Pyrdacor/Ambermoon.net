using System;

namespace Ambermoon.Data
{
    public enum GraphicFormat
    {
        Palette5Bit,
        Palette4Bit,
        Palette3Bit,
        Texture4Bit,
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
            GraphicFormat.Texture4Bit => 4,
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

        public Graphic()
        {

        }

        public Graphic(int width, int height, byte colorIndex)
        {
            Width = width;
            Height = height;
            Data = new byte[Width * Height];
            IndexedGraphic = true;

            Array.Fill(Data, colorIndex);
        }

        public void AddOverlay(uint x, uint y, Graphic overlay)
        {
            if (!overlay.IndexedGraphic || !IndexedGraphic)
                throw new AmbermoonException(ExceptionScope.Application, "Non-indexed graphics can not be used with overlays.");

            if (x + overlay.Width > Width || y + overlay.Height > Height)
                throw new IndexOutOfRangeException("Overlay is outside the bounds.");

            for (uint r = 0; r < overlay.Height; ++r)
            {
                for (uint c = 0; c < overlay.Width; ++c)
                {
                    byte index = overlay.Data[c + r * overlay.Width];

                    if (index != 0)
                        Data[x + c + (y + r) * Width] = index;
                }
            }
        }
    }
}
