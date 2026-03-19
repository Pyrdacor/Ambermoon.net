using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.Compressions;

internal class DeltaCompression : ICompression<DeltaCompression>, ICompression
{
    public static ushort Identifier => 0xD1FF;

    public IDataReader Decompress(IDataReader dataReader)
    {
        var reader = new DeflateCompression().Decompress(dataReader);

        if (reader.Size == 0)
            return reader;

        var data = reader.ReadToEnd();
        int current = data[0];

        for (int i = 1; i < data.Length; ++i)
        {
            current += data[i];
            data[i] = (byte)current;
        }

        return new DataReader(data);
    }

    public byte[] Compress(byte[] data)
    {
        if (data.Length == 0)
            return data;

        int previous = data[0];

        for (int i = 1; i < data.Length; ++i)
        {
            int current = data[i];
            data[i] = (byte)(current - previous);
            previous = current;
        }

        return new DeflateCompression().Compress(data);
    }
}
