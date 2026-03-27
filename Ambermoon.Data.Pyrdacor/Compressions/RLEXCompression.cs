using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.Compressions;

internal class RLEXCompression : ICompression<RLEXCompression>, ICompression
{
    public static ushort Identifier => 0x00FF;

    public IDataReader Decompress(IDataReader dataReader)
    {
        var decompressedData = new List<byte>();

        while (dataReader.Position < dataReader.Size)
        {
            var b = dataReader.ReadByte();

            if (b == 0 || b == 0xff)
            {
                int count = 1 + dataReader.ReadByte();

                while (--count >= 0)
                    decompressedData.Add(b);
            }
            else
                decompressedData.Add(b);
        }

        return new DataReader([.. decompressedData]);
    }

    public byte[] Compress(byte[] data)
    {
        bool isZero = false;
        int repeatCount = 0;
        var compressedData = new List<byte>();

        void WriteRepeated()
        {
            if (repeatCount == 0)
                return;

            int chunks = repeatCount / 256;
            int lastChunkSize = repeatCount % 256;
            byte literal = isZero ? (byte)0 : (byte)0xff;

            for (int i = 0; i < chunks; ++i)
            {
                compressedData.Add(literal);
                compressedData.Add(255);
            }

            if (lastChunkSize != 0)
            {
                compressedData.Add(literal);
                compressedData.Add((byte)(lastChunkSize - 1));
            }

            repeatCount = 0;
        }

        for (int i = 0; i < data.Length; ++i)
        {
            if (data[i] == 0)
            {
                if (!isZero && repeatCount > 0)
                    WriteRepeated();

                isZero = true;
                ++repeatCount;
            }
            else if (data[i] == 0xff)
            {
                if (isZero && repeatCount > 0)
                    WriteRepeated();

                isZero = false;
                ++repeatCount;
            }
            else
            {
                WriteRepeated();
                compressedData.Add(data[i]);
            }
        }

        WriteRepeated();

        return [..compressedData];
    }
}
