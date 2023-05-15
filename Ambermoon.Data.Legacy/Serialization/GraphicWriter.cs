using Ambermoon.Data.Serialization;
using System;

namespace Ambermoon.Data.Legacy.Serialization
{
    public static class GraphicWriter
    {
        public static void WriteGraphic(Graphic graphic, IDataWriter dataWriter, GraphicInfo graphicInfo, byte maskColor = 0, Graphic palette = null)
        {
            if (graphicInfo.Width != graphic.Width || graphicInfo.Height != graphic.Height)
                throw new AmbermoonException(ExceptionScope.Data, "Graphic dimensions do not match the given graphic info dimensions.");

            if (graphic.Width == 0 && graphic.Height == 0)
                return; // Nothing to write

            // Copy the graphic as we might change the data
            graphic = graphic.Clone();

            if (maskColor != 0)
            {
                if (!graphic.IndexedGraphic)
                    throw new AmbermoonException(ExceptionScope.Data, "Non-indexed graphic can not use a mask color index.");

                graphic.ReplaceColor(0, maskColor);
                graphic.ReplaceColor(32, 0);
            }

            switch (graphicInfo.GraphicFormat)
            {
                case GraphicFormat.Palette3Bit:
                case GraphicFormat.Palette4Bit:
                case GraphicFormat.Palette5Bit:
                case GraphicFormat.Texture4Bit:
                    if (!graphic.IndexedGraphic)
                        throw new AmbermoonException(ExceptionScope.Data, "Non-indexed graphic can not be written as bit-planar images.");
                    WritePaletteGraphic(graphic, dataWriter, graphicInfo.BitsPerPixel, graphicInfo, graphicInfo.GraphicFormat == GraphicFormat.Texture4Bit ? 8 : null);
                    break;
                case GraphicFormat.XRGB16:
                    if (graphic.IndexedGraphic && palette == null)
                        throw new AmbermoonException(ExceptionScope.Data, "To store an indexed graphic as XRGB16 a palette is needed.");
                    WritePixelGraphic(dataWriter, graphic.Width, graphic.Height, graphic.IndexedGraphic ? ToPixelData16(graphic, palette, graphicInfo.Alpha) : graphic.Data, 2);
                    break;
                case GraphicFormat.RGBA32:
                    if (graphic.IndexedGraphic && palette == null)
                        throw new AmbermoonException(ExceptionScope.Data, "To store an indexed graphic as RGBA32 a palette is needed.");
                    WritePixelGraphic(dataWriter, graphic.Width, graphic.Height, graphic.IndexedGraphic ? ToPixelData32(graphic, palette, graphicInfo.Alpha) : graphic.Data, 4);
                    break;
                case GraphicFormat.AttachedSprite:
                    throw new NotImplementedException("Attached sprite saving is not implemented yet.");
                default:
                    throw new NotSupportedException("Invalid legacy graphic format.");

            }
        }

        private static void WritePixelGraphic(IDataWriter dataWriter, int width, int height, byte[] data, int bytesPerPixel)
        {
            if (data.Length != width * height * bytesPerPixel)
                throw new AmbermoonException(ExceptionScope.Data, "Image data sizes does not match the given dimensions and color format.");

            dataWriter.Write(data);
        }

        private static byte[] ToPixelData16(Graphic graphic, Graphic palette, bool alpha)
        {
            var data = new byte[graphic.Width * graphic.Height * 2];
            byte alphaIndex = (byte)(alpha ? 0 : 0xff);

            for (int y = 0; y < graphic.Height; ++y)
            {
                for (int x = 0; x < graphic.Width; ++x)
                {
                    int i = x + y * graphic.Width;
                    int index = graphic.Data[i];

                    if (index != alphaIndex)
                    {
                        var r = palette.Data[i * 4 + 0];
                        var g = palette.Data[i * 4 + 1];
                        var b = palette.Data[i * 4 + 2];
                        var a = palette.Data[i * 4 + 3];
                        byte ar = (byte)((a & 0xf0) | (r >> 4));
                        byte gb = (byte)((g & 0xf0) | (b >> 4));
                        data[i * 2 + 0] = ar;
                        data[i * 2 + 1] = gb;
                    }
                }
            }

            return data;
        }

        private static byte[] ToPixelData32(Graphic graphic, Graphic palette, bool alpha)
        {
            return graphic.ToPixelData(palette, (byte)(alpha ? 0 : 0xff));
        }

        private static int ToNextBoundary(int size)
        {
            return size % 8 == 0 ? size : size + (8 - size % 8);
        }

        private static void WritePaletteGraphic(Graphic graphic, IDataWriter dataWriter, int planes, GraphicInfo graphicInfo, int? pixelsPerPlane = null)
        {
            if (pixelsPerPlane == 0)
                throw new AmbermoonException(ExceptionScope.Data, "A value of 0 for pixelsPerPlane is not allowed.");

            int numChunks = pixelsPerPlane == null ? 1 : (graphic.Width + 7) / pixelsPerPlane.Value;
            int calcWidth = ToNextBoundary(pixelsPerPlane ?? graphicInfo.Width);

            for (int y = 0; y < graphic.Height; ++y)
            {
                for (int n = 0; n < numChunks; ++n)
                {
                    int xOffset = n * pixelsPerPlane ?? 0;

                    for (int p = 0; p < planes; ++p)
                    {
                        int pmask = 1 << p;
                        byte b = 0;

                        for (int x = 0; x < calcWidth; ++x)
                        {
                            int realX = xOffset + x;

                            if (realX >= graphic.Width)
                            {
                                dataWriter.Write(b);
                                break;
                            }

                            int index = graphic.Data[realX + y * graphic.Width];
                            int bit = 7 - x % 8;

                            if ((index & pmask) != 0)
                                b |= (byte)(1 << bit);

                            if (bit == 0)
                            {
                                dataWriter.Write(b);
                                b = 0;
                            }
                        }
                    }
                }
            }
        }
    }
}
