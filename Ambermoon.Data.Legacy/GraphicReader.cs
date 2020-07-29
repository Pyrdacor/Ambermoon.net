using System;

namespace Ambermoon.Data.Legacy
{
    public class GraphicReader : IGraphicReader
    {
        static int ToNextWordBoundary(int size)
        {
            return size % 2 == 0 ? size : size + 1;
        }

        void ReadPaletteGraphic(Graphic graphic, IDataReader dataReader, int planes, GraphicInfo graphicInfo, int? pixelsPerPlane = null)
        {
            graphic.Width = graphicInfo.Width;
            graphic.Height = graphicInfo.Height;

            int ppp = pixelsPerPlane ?? graphicInfo.Width;
            int calcWidth = ToNextWordBoundary(ppp);
            int planeSize = (calcWidth + 7) / 8;
            int sizeToRead = (graphic.Width * planes * graphic.Height + 7) / 8;
            var data = dataReader.ReadBytes(sizeToRead);
            int bitIndex = 0;
            int byteIndex = 0;
            int offset = 0;

            for (int y = 0; y < graphic.Height; ++y)
            {
                for (int n = 0; n < graphic.Width / ppp; ++n)
                {
                    for (int x = 0; x < ppp; ++x)
                    {
                        byte paletteIndex = 0;

                        for (int p = 0; p < planes; ++p)
                        {
                            if ((data[offset + p * planeSize + byteIndex] & (1 << (7 - bitIndex))) != 0)
                                paletteIndex |= (byte)(1 << p);
                        }

                        paletteIndex += graphicInfo.PaletteOffset;

                        if (graphicInfo.Alpha && paletteIndex == graphicInfo.PaletteOffset)
                            graphic.Data[n * ppp + x + y * graphic.Width] = 0;
                        else
                            graphic.Data[n * ppp + x + y * graphic.Width] = paletteIndex;

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

        public void ReadGraphic(Graphic graphic, IDataReader dataReader, GraphicInfo? graphicInfo)
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
                    graphic.Data = new byte[graphic.Width * graphic.Height * 4];
                    for (int i = 0; i < graphic.Width * graphic.Height; ++i)
                    {
                        ulong color = dataReader.ReadDword();
                        graphic.Data[i * 4 + 0] = (byte)((color >> 24) & 0xff);
                        graphic.Data[i * 4 + 1] = (byte)((color >> 16) & 0xff);
                        graphic.Data[i * 4 + 2] = (byte)((color >> 8) & 0xff);
                        graphic.Data[i * 4 + 3] = (byte)(color & 0xff);
                    }
                    break;
                default:
                    throw new Exception("Invalid legacy graphic format.");
            }
        }
    }
}
