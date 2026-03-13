using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using PycDecompressor = PYC.Compression.Decompressor;
using PycCompressor = PYC.Compression.Compressor;

namespace Ambermoon.Data.Pyrdacor.Compressions;

internal class PycCompression : ICompression<PycCompression>, ICompression
{
    public static ushort Identifier => 0x5CBB;

    public IDataReader Decompress(IDataReader dataReader)
    {
        return DataReader.FromData(PycDecompressor.Decompress(dataReader.ReadToEnd()));
    }

    public IDataWriter Compress(IDataWriter dataWriter)
    {
        return new DataWriter(PycCompressor.Compress(dataWriter.ToArray()));
    }
}
