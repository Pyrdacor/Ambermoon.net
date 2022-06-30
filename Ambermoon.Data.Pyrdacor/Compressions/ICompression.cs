using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.Compressions
{
    public interface ICompression
    {
        ushort Identifier { get; }
        IDataReader Decompress(IDataReader dataReader);
        IDataWriter Compress(IDataWriter dataWriter);

        public static ICompression NoCompression { get; } = new NullCompression();
        public static ICompression Deflate { get; } = new Deflate();
        public static ICompression RLE0 { get; } = new RLE0();

        public static ushort GetIdentifier<T>() where T : ICompression, new()
        {
            return new T().Identifier;
        }
    }
}
