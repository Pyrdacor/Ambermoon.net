using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.Compressions;

public interface ICompression<T> : ICompression where T : ICompression
{
    ushort ICompression.GetIdentifier() => T.Identifier;
}

public interface ICompression
{
    static virtual ushort Identifier => 0xFFFF;
    IDataReader Decompress(IDataReader dataReader);
    byte[] Compress(byte[] data);
    abstract ushort GetIdentifier();

    public static KeyValuePair<ushort, ICompression> NoCompression { get; } = Create<NullCompression>();
    public static KeyValuePair<ushort, ICompression> Deflate { get; } = Create<DeflateCompression>();
    public static KeyValuePair<ushort, ICompression> RLE0 { get; } = Create<RLE0Compression>();
    public static KeyValuePair<ushort, ICompression> RLEX { get; } = Create<RLEXCompression>();
    public static KeyValuePair<ushort, ICompression> Delta { get; } = Create<DeltaCompression>();

    public static ushort GetIdentifier<T>() where T : ICompression
    {
        return T.Identifier;
    }

    private static KeyValuePair<ushort, ICompression> Create<T>() where T : ICompression, new()
    {
        return KeyValuePair.Create<ushort, ICompression>(T.Identifier, new T());
    }
}
