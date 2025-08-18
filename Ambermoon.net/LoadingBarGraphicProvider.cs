using Ambermoon.Data;
using Ambermoon.Data.Legacy.Serialization;

namespace Ambermoon;

internal class LoadingBarGraphicProvider
{
    static Graphic LoadImage(byte[] imageData)
    {
        var dataReader = new DataReader(imageData);
        int width = dataReader.ReadWord();
        int height = dataReader.ReadWord();
        int numColors = dataReader.ReadByte();

        byte[] colors = new byte[numColors * 3];

        for (int i = 0; i < numColors; i++)
        {
            colors[i * 3 + 0] = dataReader.ReadByte();
            colors[i * 3 + 1] = dataReader.ReadByte();
            colors[i * 3 + 2] = dataReader.ReadByte();
        }

        int chunkSize = width * height;
        byte[] data = new byte[chunkSize * 4];

        for (int i = 0; i < chunkSize; ++i)
        {
            int index = dataReader.ReadByte();

            data[i * 4 + 0] = colors[index * 3 + 0];
            data[i * 4 + 1] = colors[index * 3 + 1];
            data[i * 4 + 2] = colors[index * 3 + 2];
            data[i * 4 + 3] = 0xff;
        }

        return new Graphic
        {
            Width = width,
            Height = height,
            Data = data,
            IndexedGraphic = false
        };
    }

    public static Graphic GetGraphic(int index)
    {
        return index switch
        {
            0 => LoadImage(Resources.LoadingBarLeft),
            1 => LoadImage(Resources.LoadingBarRight),
            2 => LoadImage(Resources.LoadingBarMid),
            _ => LoadImage(Resources.LoadingBarGreen)
        };
    }
}