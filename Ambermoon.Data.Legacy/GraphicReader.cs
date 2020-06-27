using System;

namespace Ambermoon.Data.Legacy
{
    public class GraphicReader : IGraphicReader
    {
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
                    {
                        var palette = graphicInfo.Value.Palette;

                        if (palette == null)
                            throw new ArgumentNullException("Legacy palette graphics need a palette.");

                        var data = dataReader.ReadBytes(width * height * 5 / 8); // as width is always a multiple of 8 this will work
                        int planeSize = width / 8; // as width is always a multiple of 8 this will work
                        int bitIndex = 0;
                        int byteIndex = 0;
                        int offset = 0;

                        for (int y = 0; y < height; ++y)
                        {
                            for (int x = 0; x < width; ++x)
                            {
                                int paletteIndex = 0;

                                for (int p = 0; p < 5; ++p)
                                {
                                    if ((data[offset + p * planeSize + byteIndex] & (1 << (7 - bitIndex))) != 0)
                                        paletteIndex |= (1 << p);
                                }

                                palette.Fill(graphic.Data, (x + y * width) * 4, paletteIndex);

                                if (++bitIndex == 8)
                                {
                                    bitIndex = 0;
                                    ++byteIndex;
                                }
                            }

                            offset += 5 * planeSize;
                            byteIndex = 0;
                            bitIndex = 0;
                        }
                    }
                    break;
                case GraphicFormat.Texture4Bit:
                    {
                        byte[] palette = new byte[]
                        {
                            0, 0, 0, 255,
                            0, 0, 170, 255,
                            170, 0, 0, 255,
                            170, 0, 170, 255,
                            0, 170, 0, 255,
                            0, 170, 170, 0, 255,
                            170, 170, 0, 255,
                            170, 170, 170, 255,
                            85, 85, 85, 255,
                            0, 0, 255, 255,
                            255, 0, 0, 255,
                            255, 0, 255, 255,
                            0, 255, 0, 255,
                            0, 255, 255, 255,
                            255, 255, 0, 255,
                            255, 255, 255, 255
                        };
                        var bitReader = new BitReader(dataReader.ReadBytes((width * height * 4 + 7) / 8));

                        for (int i = 0; i < width * height; ++i)
                        {
                            int index = bitReader.ReadBits(4);
                            graphic.Data[i * 4 + 0] = palette[index * 4 + 0];
                            graphic.Data[i * 4 + 1] = palette[index * 4 + 1];
                            graphic.Data[i * 4 + 2] = palette[index * 4 + 2];
                            graphic.Data[i * 4 + 3] = 255;
                        }
                    }
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
                default:
                    throw new Exception("Invalid legacy graphic format.");
            }
        }
    }
}
