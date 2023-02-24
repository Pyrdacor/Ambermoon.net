using Ambermoon.Data.Serialization;
using System;

namespace Ambermoon.Data.Legacy.Serialization
{
    public class GraphicReader : IGraphicReader
    {
        static int ToNextBoundary(int size)
        {
            return size % 8 == 0 ? size : size + (8 - size % 8);
        }

        static void ReadPaletteGraphic(Graphic graphic, IDataReader dataReader, int planes, GraphicInfo graphicInfo, int? pixelsPerPlane = null)
        {
            graphic.Width = graphicInfo.Width;
            graphic.Height = graphicInfo.Height;

            int ppp = pixelsPerPlane ?? graphicInfo.Width;
            int calcWidth = ToNextBoundary(ppp);
            int planeSize = (calcWidth + 7) / 8;
            int scanLine = ToNextBoundary(graphic.Width);
            int sizeToRead = (scanLine * planes * graphic.Height + 7) / 8;
            var data = dataReader.ReadBytes(sizeToRead);
            int bitIndex = 0;
            int byteIndex = 0;
            int offset = 0;
            int planeCycles = ppp == graphicInfo.Width ? 1 : (graphic.Width + ppp - 1) / ppp;

            for (int y = 0; y < graphic.Height; ++y)
            {
                for (int n = 0; n < planeCycles; ++n)
                {
                    for (int x = 0; x < ppp; ++x)
                    {
                        int mx = n * ppp + x;

                        if (mx >= graphic.Width)
                            break;

                        byte paletteIndex = 0;

                        for (int p = 0; p < planes; ++p)
                        {
                            if ((data[offset + p * planeSize + byteIndex] & (1 << (7 - bitIndex))) != 0)
                                paletteIndex |= (byte)(1 << p);
                        }

                        paletteIndex += graphicInfo.PaletteOffset;

                        if (graphicInfo.Alpha && paletteIndex == graphicInfo.PaletteOffset)
                            graphic.Data[mx + y * graphic.Width] = graphicInfo.ColorKey;
                        else
                            graphic.Data[mx + y * graphic.Width] = paletteIndex;

                        if (++bitIndex == 8)
                        {
                            bitIndex = 0;
                            ++byteIndex;
                        }
                    }

                    offset += planes * planeSize;
                    byteIndex = 0;
                    bitIndex = 0;
                }
            }
        }

        public void ReadGraphic(Graphic graphic, IDataReader dataReader, GraphicInfo? graphicInfo, byte maskColor = 0)
        {
            // Legacy graphics need the graphicInfo.
            if (graphicInfo == null)
                throw new ArgumentNullException("Legacy graphics need information about the graphic to load.");

            graphic.Width = graphicInfo.Value.Width;
            graphic.Height = graphicInfo.Value.Height;

            switch (graphicInfo.Value.GraphicFormat)
            {
                case GraphicFormat.Palette5Bit:
                    graphic.IndexedGraphic = true;
                    graphic.Data = new byte[graphic.Width * graphic.Height];
                    ReadPaletteGraphic(graphic, dataReader, 5, graphicInfo.Value);
                    break;
                case GraphicFormat.Palette4Bit:
                    graphic.IndexedGraphic = true;
                    graphic.Data = new byte[graphic.Width * graphic.Height];
                    ReadPaletteGraphic(graphic, dataReader, 4, graphicInfo.Value);
                    break;
                case GraphicFormat.Palette3Bit:
                    graphic.IndexedGraphic = true;
                    graphic.Data = new byte[graphic.Width * graphic.Height];
                    ReadPaletteGraphic(graphic, dataReader, 3, graphicInfo.Value);
                    break;
                case GraphicFormat.Texture4Bit:
                    graphic.IndexedGraphic = true;
                    graphic.Data = new byte[graphic.Width * graphic.Height];
                    ReadPaletteGraphic(graphic, dataReader, 4, graphicInfo.Value, 8);
                    break;
                case GraphicFormat.XRGB16:
                    graphic.IndexedGraphic = false;
                    graphic.Data = new byte[graphic.Width * graphic.Height * 4];
                    for (int i = 0; i < graphic.Width * graphic.Height; ++i)
                    {
                        ushort color = dataReader.ReadWord();
                        graphic.Data[i * 4 + 0] = (byte)((color >> 8) & 0x0f);
                        graphic.Data[i * 4 + 1] = (byte)((color >> 4) & 0x0f);
                        graphic.Data[i * 4 + 2] = (byte)(color & 0x0f);
                        graphic.Data[i * 4 + 3] = 255;

                        graphic.Data[i * 4 + 0] |= (byte)(graphic.Data[i * 4 + 0] << 4);
                        graphic.Data[i * 4 + 1] |= (byte)(graphic.Data[i * 4 + 1] << 4);
                        graphic.Data[i * 4 + 2] |= (byte)(graphic.Data[i * 4 + 2] << 4);
                    }
                    break;
                case GraphicFormat.RGBA32:
                    graphic.IndexedGraphic = false;
                    graphic.Data = dataReader.ReadBytes(graphic.Width * graphic.Height * 4);
                    break;
                case GraphicFormat.AttachedSprite:
                {
                    // Attached sprites are two Amiga hardware sprites (2bpp each).
                    //
                    // word[2]           ControlWords
                    // word[2 * Height]  Data (2 words for each pixel row)
                    // long              Next pointer
                    //
                    // Each sprite has a width of 16 pixel. The first sprite gives the lower
                    // 2 bits and the second sprite the higher 2 bits of a 4 bit color index.
                    // The color index starts at 16 (transparent) and can otherwise have the
                    // values 17 to 31.
                    if (graphic.Width % 16 != 0)
                        throw new Exception("Attached sprites must have a width which is a multiple of 16.");
                    if (graphic.Width > 64)
                        throw new Exception("Attached sprites has a max width of 64 pixels.");
                    graphic.IndexedGraphic = true;
                    graphic.Data = new byte[graphic.Width * graphic.Height];
                    int numSprites = 2 * graphic.Width / 16;
                    for (int s = 0; s < numSprites; ++s)
                    {
                        // Skip the 2 control words
                        dataReader.Position += 4;

                        int line0Add = s % 2 == 0 ? 1 : 4;
                        int line1Add = s % 2 == 0 ? 2 : 8;

                        for (int y = 0; y < graphic.Height; ++y)
                        {
                            ushort line0 = dataReader.ReadWord();
                            ushort line1 = dataReader.ReadWord();
                            ushort mask = 0x8000;                          

                            for (int x = 0; x < 16; ++x)
                            {
                                int index = (s / 2) * 16 + x + y * graphic.Width;
                                int colorIndex = graphic.Data[index];

                                if ((line0 & mask) != 0)
                                    colorIndex |= line0Add;
                                if ((line1 & mask) != 0)
                                    colorIndex |= line1Add;

                                graphic.Data[index] = (byte)colorIndex;

                                mask >>= 1;
                            }
                        }

                        // Skip the next pointer
                        dataReader.Position += 4;
                    }

                    // Adjust indices
                    for (int i = 0; i < graphic.Data.Length; ++i)
                    {
                        if (graphic.Data[i] != 0)
                            graphic.Data[i] += 16;
                    }
                    break;
                }   
                default:
                    throw new Exception("Invalid legacy graphic format.");
            }

            if (maskColor != 0)
            {
                graphic.ReplaceColor(0, 32);
                graphic.ReplaceColor(maskColor, 0);
            }
        }
    }
}
