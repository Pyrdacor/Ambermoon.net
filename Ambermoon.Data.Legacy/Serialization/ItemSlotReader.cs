using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Legacy.Serialization
{
    internal static class ItemSlotReader
    {
        public static void ReadItemSlot(ItemSlot itemSlot, IDataReader dataReader)
        {
            itemSlot.Amount = dataReader.ReadByte();
            itemSlot.NumRemainingCharges = dataReader.ReadByte();
            itemSlot.RechargeTimes = dataReader.ReadByte();
            itemSlot.Flags = (ItemSlotFlags)dataReader.ReadByte();
            itemSlot.ItemIndex = dataReader.ReadWord();
        }
    }
}
