using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.Compressions;

internal class PycCompression : ICompression<PycCompression>, ICompression
{
    public static ushort Identifier => 0xBB5C;

    public IDataReader Decompress(IDataReader dataReader)
    {
        return Decompressor.Decompress();
    }

    public IDataWriter Compress(IDataWriter dataWriter)
    {
        return dataWriter;
    }
}
