using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

internal class ItemData : IFileSpec<ItemData>, IFileSpec
{
    public static string Magic => "ITM";
    public static byte SupportedVersion => 0;
    public static ushort PreferredCompression => ICompression.GetIdentifier<Deflate>();
    Item? item = null;

    public Item Item => item!;

    public ItemData()
    {

    }

    public ItemData(Item item)
    {
        this.item = item;
    }

    public void Read(IDataReader dataReader, uint index, GameData _)
    {
        item = Item.Load(index, new ItemReader(), dataReader);
    }

    public void Write(IDataWriter dataWriter)
    {
        if (item == null)
            throw new AmbermoonException(ExceptionScope.Application, "Chest data was null when trying to write it.");

        ItemWriter.WriteItem(item, dataWriter);
    }
}
