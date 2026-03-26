using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Pyrdacor.Extensions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

public class GameDataInfo : IFileSpec<GameDataInfo>, IFileSpec
{
    string name = ""; // usually "Ambermoon" but can be used for mods or other games using the same format
    bool advanced = false;
    string version = "";
    GameLanguage language = GameLanguage.English;
    DateTime releaseDate = DateTime.MinValue;

    public static string Magic => "INF";
    public static byte SupportedVersion => 0;
    public static ushort PreferredCompression => ICompression.GetIdentifier<NullCompression>();

    public string Name
    {
        get => name;
        internal init => name = value;
    }

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

    public DateTime ReleaseDate
    {
        get => releaseDate;
        internal init => releaseDate = value;
    }

    public void Read(IDataReader dataReader, uint _, GameData __, byte ___)
    {
        name = dataReader.ReadString();
        advanced = dataReader.ReadBool();
        language = dataReader.ReadEnum8<GameLanguage>();
        version = dataReader.ReadString();
        releaseDate = DateTime.ParseExact(dataReader.ReadString(8), "ddMMyyyy", null);
    }

    public void Write(IDataWriter dataWriter)
    {
        dataWriter.Write(Name);
        dataWriter.Write(Advanced);
        dataWriter.WriteEnum8(Language);
        dataWriter.Write(Version);
        dataWriter.WriteWithoutLength(ReleaseDate.ToString("ddMMyyyy"));
    }
}
