using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

using Savegame = Data.SavegameData;

internal class SavegameData : IFileSpec<SavegameData>, IFileSpec
{
    public static string Magic => "SAV";
    public static byte SupportedVersion => 0;
    public static ushort PreferredCompression => ICompression.GetIdentifier<RLE0Compression>();
    Savegame savegame = new();

    public Savegame Savegame => savegame;

    public SavegameData()
    {

    }

    public SavegameData(Savegame savegame)
    {
        this.savegame = savegame;
    }

    public void Read(IDataReader dataReader, uint _, GameData __, byte ___)
    {
        savegame = new();

        SavegameSerializer.ReadSaveData(savegame, dataReader);
    }

    public static void ReadInto(Savegame savegame, IDataReader dataReader, uint _, GameData __, byte ___)
    {
        SavegameSerializer.ReadSaveData(savegame, dataReader);
    }

    public void Write(IDataWriter dataWriter)
    {
        SavegameSerializer.WriteSaveData(savegame, dataWriter);
    }
}
