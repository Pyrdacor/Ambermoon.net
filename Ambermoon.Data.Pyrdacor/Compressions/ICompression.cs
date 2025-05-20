using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.Compressions;

public interface ICompression<T> where T : ICompression
{
    ushort GetIdentifier() => T.Identifier;
}

public interface ICompression
{
    static virtual ushort Identifier => 0xFFFF;
    IDataReader Decompress(IDataReader dataReader);
    IDataWriter Compress(IDataWriter dataWriter);

    public static KeyValuePair<ushort, ICompression> NoCompression { get; } = Create<NullCompression>();
    public static KeyValuePair<ushort, ICompression> Deflate { get; } = Create<Deflate>();
    public static KeyValuePair<ushort, ICompression> RLE0 { get; } = Create<RLE0>();
    public static KeyValuePair<ushort, ICompression> PYC { get; } = Create<PycCompression>();

    public static ushort GetIdentifier<T>() where T : ICompression
    {
        return T.Identifier;
    }

    private static KeyValuePair<ushort, ICompression> Create<T>() where T : ICompression, new()
    {
        return KeyValuePair.Create<ushort, ICompression>(T.Identifier, new T());
    }
}
