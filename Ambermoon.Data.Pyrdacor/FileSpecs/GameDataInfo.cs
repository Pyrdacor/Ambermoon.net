using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Pyrdacor.Extensions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

internal class GameDataInfo : IFileSpec<GameDataInfo>, IFileSpec
{
    public static string Magic => "INF";
    public static byte SupportedVersion => 0;
    public static ushort PreferredCompression => ICompression.GetIdentifier<NullCompression>();

    public bool Advanced { get; private set; }
    public string Version { get; private set; } = "";
    public GameLanguage Language { get; private set; }

    public void Read(IDataReader dataReader, uint _, GameData __, byte ___)
    {
        Advanced = dataReader.ReadBool();
        Language = dataReader.ReadEnum8<GameLanguage>();
        Version = dataReader.ReadString();
    }

    public void Write(IDataWriter dataWriter)
    {
        dataWriter.Write(Advanced);
        dataWriter.WriteEnum8(Language);
        dataWriter.Write(Version);
    }
}
