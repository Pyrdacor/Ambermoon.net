using System;

namespace Ambermoon.Data.Legacy
{
    public class GraphicReader : IGraphicReader
    {
        void ReadPaletteGraphic(Graphic graphic, IDataReader dataReader, int planes, GraphicInfo graphicInfo)
        {
            graphic.Width = graphicInfo.Width;
            graphic.Height = graphicInfo.Height;

            var data = dataReader.ReadBytes((graphic.Width * graphic.Height * planes + 7) / 8);
            int planeSize = (graphic.Width + 7) / 8;
            int bitIndex = 0;
            int byteIndex = 0;
            int offset = 0;

            for (int y = 0; y < graphic.Height; ++y)
            {
                for (int x = 0; x < graphic.Width; ++x)
                {
                    byte paletteIndex = 0;

                    for (int p = 0; p < planes; ++p)
                    {
                        if ((data[offset + p * planeSize + byteIndex] & (1 << (7 - bitIndex))) != 0)
                            paletteIndex |= (byte)(1 << p);
                    }

                    if (!graphicInfo.Alpha || paletteIndex != 0)
                        paletteIndex += graphicInfo.PaletteOffset;

                    graphic.Data[x + y * graphic.Width] = paletteIndex;

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
