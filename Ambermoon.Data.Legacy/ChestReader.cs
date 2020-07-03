namespace Ambermoon.Data.Legacy
{
    public class ChestReader : IChestReader
    {
        public void ReadChest(Chest chest, IDataReader dataReader)
        {
            dataReader.ReadByte(); // Unknown
            chest.Type = (ChestType)dataReader.ReadByte(); // TODO: check if this is correct and which are possible values
            dataReader.ReadWord(); // Unknown

            for (int y = 0; y < 4; ++i)
            {
                for (int x = 0; x < 6; ++x)
                {
                    chest.Slots[x, y] = new ItemSlot
                    {
                        ItemIndex = dataReader.ReadWord(),
                        Amount = dataReader.ReadByte()
                    };

                    dataReader.ReadBytes(3); // Unknown
                }
            }
        }
    }
}
