using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Legacy.Serialization
{
    public class ChestReader : IChestReader
    {
        public void ReadChest(Chest chest, IDataReader dataReader)
        {
            for (int y = 0; y < 4; ++y)
            {
                for (int x = 0; x < 6; ++x)
                {
                    var itemSlot = new ItemSlot();
                    ItemSlotReader.ReadItemSlot(itemSlot, dataReader);
                    chest.Slots[x, y] = itemSlot;
                }
            }

            chest.Gold = dataReader.ReadWord();
            chest.Food = dataReader.ReadWord();
        }
    }
}
