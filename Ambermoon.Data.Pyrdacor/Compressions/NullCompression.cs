using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.Compressions;

internal class NullCompression : ICompression<NullCompression>, ICompression
{
    public static ushort Identifier => 0x0000;

    public IDataReader Decompress(IDataReader dataReader)
    {
        return dataReader;
    }

    public byte[] Compress(byte[] data)
    {
        return data;
    }
}
