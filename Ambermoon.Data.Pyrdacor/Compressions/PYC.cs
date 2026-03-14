using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using PycDecompressor = PYC.Compression.Decompressor;
using PycCompressor = PYC.Compression.Compressor;
using Ambermoon.Data.Pyrdacor.Serialization;

namespace Ambermoon.Data.Pyrdacor.Compressions;

internal class PycCompression : ICompression<PycCompression>, ICompression
{
    public static ushort Identifier => 0x5CBB;

    public IDataReader Decompress(IDataReader dataReader)
    {
        return new DataReaderLE(PycDecompressor.Decompress(dataReader.ReadToEnd()));
    }

    public IDataWriter Compress(IDataWriter dataWriter)
    {
        return new DataWriterLE(PycCompressor.Compress(dataWriter.ToArray()));
    }
}
