using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using System.IO.Compression;

namespace Ambermoon.Data.Pyrdacor.Compressions
{
    internal class Deflate : ICompression
    {
        public ushort Identifier => 0xDEF0;

        public IDataReader Decompress(IDataReader dataReader)
        {
            using Stream compressedFileStream = new MemoryStream(dataReader.ReadToEnd());
            using var targetStream = new MemoryStream();
            using var decompressor = new DeflateStream(compressedFileStream, CompressionMode.Decompress);
            decompressor.CopyTo(targetStream);
            targetStream.Position = 0;
            return new DataReader(targetStream);
        }

        public IDataWriter Compress(IDataWriter dataWriter)
        {
            using Stream sourceStream = new MemoryStream(dataWriter.ToArray());
            using var compressedStream = new MemoryStream();
            using var compressor = new DeflateStream(compressedStream, CompressionLevel.Optimal);
            sourceStream.Position = 0;
            sourceStream.CopyTo(compressor);
            return new DataWriter(compressedStream.ToArray());
        }
    }
}
