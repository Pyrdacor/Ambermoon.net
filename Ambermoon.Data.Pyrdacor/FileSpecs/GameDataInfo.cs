using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Pyrdacor.Extensions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

internal class GameDataInfo : IFileSpec<GameDataInfo>, IFileSpec
{
    bool advanced = false;
    string version = "";
    GameLanguage language = GameLanguage.English;

    public static string Magic => "INF";
    public static byte SupportedVersion => 0;
    public static ushort PreferredCompression => ICompression.GetIdentifier<NullCompression>();

    public bool Advanced
    {
        get => advanced;
        internal init => advanced = value;
    }

    public string Version
    {
        get => version;
        internal init => version = value;
    }

    public GameLanguage Language
    {
        get => language;
        internal init => language = value;
    }

    public void Read(IDataReader dataReader, uint _, GameData __, byte ___)
    {
        advanced = dataReader.ReadBool();
        language = dataReader.ReadEnum8<GameLanguage>();
        version = dataReader.ReadString();
    }

    public void Write(IDataWriter dataWriter)
    {
        dataWriter.Write(Advanced);
        dataWriter.WriteEnum8(Language);
        dataWriter.Write(Version);
    }
}
