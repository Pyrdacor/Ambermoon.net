using System;
using System.Collections.Generic;

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

        public Graphic CreateScaled(float factor)
        {
            if (Util.FloatEqual(factor, 1.0f))
                return this;

            if (Util.FloatEqual(factor, 0.0f) || Width == 0 || Height == 0)
                return new Graphic(0, 0, 0);

            int newWidth = Util.Floor(factor * Width);
            int newHeight = Util.Floor(factor * Height);

            var graphic = new Graphic(newWidth, newHeight, 0);

            for (int y = 0; y < newHeight; ++y)
            {
                for (int x = 0; x < newWidth; ++x)
                {
                    int sourceX = Math.Min(Util.Round(x / factor), Width - 1);
                    int sourceY = Math.Min(Util.Round(y / factor), Height - 1);
                    graphic.Data[x + y * newWidth] = Data[sourceX + sourceY * Width];
                }
            }

            return graphic;
        }

        public void ReplaceColor(byte oldColorIndex, byte newColorIndex)
        {
            for (int i = 0; i < Data.Length; ++i)
            {
                if (Data[i] == oldColorIndex)
                    Data[i] = newColorIndex;
            }
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

        public static Graphic FromIndexedData(int width, int height, byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("Graphic data was null.");

            if (data.Length != width * height)
                throw new ArgumentOutOfRangeException("Invalid graphic data size.");

            return new Graphic
            {
                Width = width,
                Height = height,
                Data = data
            };
        }

        public static Graphic CreateGradient(int width, int height, int startY, int rowsPerIncrease, byte colorIndex, byte endColorIndex)
        {
            Graphic graphic = new Graphic(width, height, colorIndex);

            for (int y = startY; y < height; ++y)
            {
                if (colorIndex < endColorIndex && (y - startY) % rowsPerIncrease == 0)
                    ++colorIndex;

                Array.Fill(graphic.Data, colorIndex, y * width, width);
            }

            return graphic;
        }

        public byte[] ToPixelData(Graphic palette, byte alphaIndex = 0)
        {
            if (IndexedGraphic)
            {
                if (palette == null)
                    throw new ArgumentNullException("Palette for indexed graphic was null.");

                byte[] data = new byte[Width * Height * 4];

                for (int i = 0; i < Width * Height; ++i)
                {
                    byte index = Data[i];

                    if (index != alphaIndex)
                    {
                        for (int c = 0; c < 4; ++c)
                            data[i * 4 + c] = palette.Data[index * 4 + c];
                    }
                    else
                    {
                        data[i * 4 + 1] = 80;
                        data[i * 4 + 3] = 1;
                    }
                }

                return data;
            }
            else
            {
                return Data;
            }
        }
    }

    public class GraphicBuilder
    {
        readonly int width;
        readonly int height;
        readonly List<KeyValuePair<Rect, byte>> coloredAreas = new List<KeyValuePair<Rect, byte>>();

        private GraphicBuilder(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        public static GraphicBuilder Create(int width, int height)
        {
            return new GraphicBuilder(width, height);
        }

        public GraphicBuilder AddColoredArea(Rect area, byte colorIndex)
        {
            coloredAreas.Add(new KeyValuePair<Rect, byte>(area, colorIndex));
            return this;
        }

        public Graphic Build()
        {
            var graphic = new Graphic(width, height, 0);

            foreach (var coloredArea in coloredAreas)
            {
                int areaX = coloredArea.Key.Left;
                int areaY = coloredArea.Key.Top;
                int areaWidth = coloredArea.Key.Width;
                int areaHeight = coloredArea.Key.Height;

                for (int y = 0; y < areaHeight; ++y)
                {
                    for (int x = 0; x < areaWidth; ++x)
                    {
                        graphic.Data[areaX + x + (areaY + y) * width] = coloredArea.Value;
                    }
                }
            }

            return graphic;
        }
    }
}
