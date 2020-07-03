namespace Ambermoon.Data.Legacy
{
    public class MerchantReader : IMerchantReader
    {
        public void ReadMerchant(Merchant merchant, IDataReader dataReader)
        {
            for (int y = 0; y < 4; ++y)
            {
                for (int x = 0; x < 6; ++x)
                {
                    int amount = dataReader.ReadByte();
                    dataReader.ReadBytes(3); // Unknown
                    uint itemIndex = dataReader.ReadWord();

                    merchant.Slots[x, y] = new ItemSlot
                    {
                        ItemIndex = itemIndex,
                        Amount = amount                        
                    };
                }
            }
        }
    }
}
