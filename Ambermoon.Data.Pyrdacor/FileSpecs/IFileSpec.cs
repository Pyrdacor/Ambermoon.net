using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

public interface IFileSpec<T> where T : IFileSpec
{
    string GetMagic() => T.Magic;
    byte GetSupportedVersion() => T.SupportedVersion;
    ushort GetPreferredCompression() => T.PreferredCompression;
}

public interface IFileSpec
{
    static virtual string Magic => "UNK";
    static virtual byte SupportedVersion => 0;
    static virtual ushort PreferredCompression => ICompression.GetIdentifier<NullCompression>();
    void Read(IDataReader dataReader, uint index, GameData gameData);
    void Write(IDataWriter dataWriter);

    public static string GetMagic<T>() where T : IFileSpec
    {
        return T.Magic;
    }

    public static byte GetSupportedVersion<T>() where T : IFileSpec
    {
        return T.SupportedVersion;
    }

    public static ICompression GetPreferredCompression<T>() where T : IFileSpec
    {
        return PADF.Compressions.TryGetValue(T.PreferredCompression, out var compression) ? compression : ICompression.NoCompression.Value;
    }
}
