using Ambermoon.Data.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data
{
    public enum GraphicFormat
    {
        Palette5Bit,
        Palette4Bit,
        Palette3Bit,
        Texture4Bit,
        XRGB16,
        RGBA32,
        AttachedSprite
    }

    public struct GraphicInfo
    {
        public GraphicFormat GraphicFormat;
        public int Width;
        public int Height;
        public bool Alpha;
        public byte PaletteOffset;
        public byte ColorKey;

        public int BitsPerPixel => GraphicFormat switch
        {
            GraphicFormat.Palette5Bit => 5,
            GraphicFormat.Palette4Bit => 4,
            GraphicFormat.Palette3Bit => 3,
            GraphicFormat.Texture4Bit => 4,
            GraphicFormat.XRGB16 => 16,
            GraphicFormat.RGBA32 => 32,
            GraphicFormat.AttachedSprite => 4,
            _ => throw new ArgumentOutOfRangeException("Invalid graphic format")
        };

        public int DataSize => (Width * Height * BitsPerPixel + 7) / 8;
    }

    [Serializable]
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

        public Graphic Clone()
        {
            var dataCopy = new byte[Data.Length];
            Buffer.BlockCopy(Data, 0, dataCopy, 0, dataCopy.Length);
            return new Graphic
            {
                Width = Width,
                Height = Height,
                Data = dataCopy,
                IndexedGraphic = IndexedGraphic
            };
        }

        public Graphic CreateScaled(float factor)
        {
            if (Util.FloatEqual(factor, 1.0f))
                return this;

            if (Util.FloatEqual(factor, 0.0f) || Width == 0 || Height == 0)
                return new Graphic(0, 0, 0);

            int newWidth = Util.Floor(factor * Width);
            int newHeight = Util.Floor(factor * Height);

            var graphic = new Graphic(newWidth, newHeight, 0) { IndexedGraphic = IndexedGraphic };

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

        public Graphic CreateScaled(int width, int height)
        {
            if (width == Width && height == Height)
                return this;

            if (Width == 0 || Height == 0 || width == 0 || height == 0)
                return new Graphic(0, 0, 0);

            var graphic = new Graphic(width, height, 0) { IndexedGraphic = IndexedGraphic };

            float xFactor = (float)width / Width;
            float yFactor = (float)height / Height;

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    int sourceX = Math.Min(Util.Round(x / xFactor), Width - 1);
                    int sourceY = Math.Min(Util.Round(y / yFactor), Height - 1);
                    graphic.Data[x + y * width] = Data[sourceX + sourceY * Width];
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

        public void AddOverlay(uint x, uint y, Graphic overlay, bool blend = true)
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

                    if (!blend || index != 0)
                        Data[x + c + (y + r) * Width] = index;

                    // In original the blend mode seems to be like this:
                    //   mask = ~max(overlayA | overlayR | overlayG | overlayB)
                    //   wallColor & colorARGB(mask, mask, mask, mask) | overlayColor
                    // If graphics always are fully opaque or fully transparent (r,g,b,a = 0,0,0,0) this means blending
                    // algorithm is like we do already with palette index 0.
                }
            }
        }

        public static Graphic Concat(params Graphic[] graphics)
        {
            var concatGraphic = new Graphic(graphics.Sum(g => g.Width), graphics.Max(g => g.Height), 0);
            uint x = 0;

            foreach (var graphic in graphics)
            {
                concatGraphic.AddOverlay(x, 0, graphic, false);
                x += (uint)graphic.Width;
            }

            return concatGraphic;
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
                Data = data,
                IndexedGraphic = true
            };
        }

        public static Graphic CreateGradient(int width, int height, int startY, int rowsPerIncrease, byte colorIndex, byte endColorIndex)
        {
            Graphic graphic = new Graphic(width, height, colorIndex) { IndexedGraphic = true };

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
                    throw new ArgumentNullException(nameof(palette));

                byte[] data = new byte[Width * Height * 4];

                for (int i = 0; i < Width * Height; ++i)
                {
                    byte index = Data[i];

                    if (index != alphaIndex)
                    {
                        for (int c = 0; c < 4; ++c)
                            data[i * 4 + c] = palette.Data[index * 4 + c];
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

        public GraphicBuilder AddColoredArea(Rect area, Color color) => AddColoredArea(area, (byte)color);

        public Graphic Build()
        {
            var graphic = new Graphic(width, height, 0) { IndexedGraphic = true };

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
