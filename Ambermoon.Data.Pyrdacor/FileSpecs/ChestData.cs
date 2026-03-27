using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

internal class ChestData : IFileSpec<ChestData>, IFileSpec
{
    private static readonly ChestReader chestReader = new();
    private static readonly ChestWriter chestWriter = new();

    public static string Magic => "CHE";
    public static byte SupportedVersion => 0;
    public static ushort PreferredCompression => ICompression.GetIdentifier<DeflateCompression>();
    Chest? chest = null;

    public Chest Chest => chest!;

    public ChestData()
    {

    }

    public ChestData(Chest chest)
    {
        this.chest = chest;
    }

    public void Read(IDataReader dataReader, uint _, GameData __, byte ___)
    {
        chest = Chest.Load(chestReader, dataReader);
    }

    public void Write(IDataWriter dataWriter)
    {
        if (chest == null)
            throw new AmbermoonException(ExceptionScope.Application, "Chest data was null when trying to write it.");

        chestWriter.WriteChest(chest, dataWriter);
    }
}
