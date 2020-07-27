namespace Ambermoon.Data.Legacy
{
    public class ChestReader : IChestReader
    {
        public void ReadChest(Chest chest, IDataReader dataReader)
        {
            for (int y = 0; y < 4; ++y)
            {
                for (int x = 0; x < 6; ++x)
                {
                    int amount = dataReader.ReadByte();
                    dataReader.ReadBytes(2); // Unknown
                    ItemSlotFlags flags = (ItemSlotFlags)dataReader.ReadByte();
                    uint itemIndex = dataReader.ReadWord();

                    chest.Slots[x, y] = new ItemSlot
                    {
                        ItemIndex = itemIndex,
                        Amount = amount,
                        Flags = flags
                    };
                }
            }

            chest.Gold = dataReader.ReadWord();
            chest.Food = dataReader.ReadWord();
        }
    }
}
