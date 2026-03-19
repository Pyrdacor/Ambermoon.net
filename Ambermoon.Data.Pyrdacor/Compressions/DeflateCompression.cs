using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using System.IO.Compression;

namespace Ambermoon.Data.Pyrdacor.Compressions;

internal class DeflateCompression : ICompression<DeflateCompression>, ICompression
{
    public static ushort Identifier => 0xDEF1;

    public IDataReader Decompress(IDataReader dataReader)
    {
        using Stream compressedFileStream = new MemoryStream(dataReader.ReadToEnd());
        using var targetStream = new MemoryStream();
        using var decompressor = new DeflateStream(compressedFileStream, CompressionMode.Decompress);
        decompressor.CopyTo(targetStream);
        targetStream.Position = 0;
        return new DataReader(targetStream);
    }

    public byte[] Compress(byte[] data)
    {
        using Stream sourceStream = new MemoryStream(data);
        using var compressedStream = new MemoryStream();

        using (var compressor = new DeflateStream(compressedStream, CompressionLevel.Optimal))
        {
            sourceStream.CopyTo(compressor);
        }

        return compressedStream.ToArray();
    }
}
