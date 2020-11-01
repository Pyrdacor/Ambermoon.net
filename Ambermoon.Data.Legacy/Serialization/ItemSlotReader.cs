using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Legacy.Serialization
{
    internal static class ItemSlotReader
    {
        public static void ReadItemSlot(ItemSlot itemSlot, IDataReader dataReader)
        {
            itemSlot.Amount = dataReader.ReadByte();
            itemSlot.Unknown = dataReader.ReadBytes(2); // Unknown
            itemSlot.Flags = (ItemSlotFlags)dataReader.ReadByte();
            itemSlot.ItemIndex = dataReader.ReadWord();
        }
    }
}
