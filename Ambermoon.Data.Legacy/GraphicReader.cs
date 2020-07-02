using System;

namespace Ambermoon.Data.Legacy
{
    public class GraphicReader : IGraphicReader
    {
        void ReadPaletteGraphic(Graphic graphic, IDataReader dataReader, int planes, GraphicInfo graphicInfo)
        {
            if (graphicInfo.Palette == null)
                throw new ArgumentNullException("Legacy palette graphics need a palette.");

            var data = dataReader.ReadBytes(graphic.Width * graphic.Height * planes / 8); // as width is always a multiple of 8 this will work
            int planeSize = graphic.Width / 8; // as width is always a multiple of 8 this will work
            int bitIndex = 0;
            int byteIndex = 0;
            int offset = 0;

            for (int y = 0; y < graphic.Height; ++y)
            {
                for (int x = 0; x < graphic.Width; ++x)
                {
                    int paletteIndex = 0;

                    for (int p = 0; p < planes; ++p)
                    {
                        if ((data[offset + p * planeSize + byteIndex] & (1 << (7 - bitIndex))) != 0)
                            paletteIndex |= (1 << p);
                    }

                    if (!graphicInfo.Alpha || paletteIndex != 0)
                        paletteIndex += graphicInfo.PaletteOffset;

                    graphicInfo.Palette.Fill(graphic.Data, (x + y * graphic.Width) * 4, paletteIndex);

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

            int width = graphicInfo.Value.Width;
            int height = graphicInfo.Value.Height;
            graphic.Data = new byte[width * height * 4];

            switch (graphicInfo.Value.GraphicFormat)
            {
                case GraphicFormat.Palette5Bit:
                    ReadPaletteGraphic(graphic, dataReader, 5, graphicInfo.Value);
                    break;
                case GraphicFormat.Palette4Bit:
                    ReadPaletteGraphic(graphic, dataReader, 4, graphicInfo.Value);
                    break;
                case GraphicFormat.Palette3Bit:
                    ReadPaletteGraphic(graphic, dataReader, 3, graphicInfo.Value);
                    break;
                case GraphicFormat.XRGB16:
                    for (int i = 0; i < width * height; ++i)
                    {
                        ushort color = dataReader.ReadWord();
                        graphic.Data[i * 4 + 0] = (byte)(((color >> 8) & 0x0f) << 4);
                        graphic.Data[i * 4 + 1] = (byte)(((color >> 4) & 0x0f) << 4);
                        graphic.Data[i * 4 + 2] = (byte)((color & 0x0f) << 4);
                        graphic.Data[i * 4 + 3] = 255;
                    }
                    break;
                case GraphicFormat.RGBA32:
                    for (int i = 0; i < width * height; ++i)
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
