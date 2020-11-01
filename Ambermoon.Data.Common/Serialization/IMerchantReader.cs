namespace Ambermoon.Data.Serialization
{
    public interface IMerchantReader
    {
        void ReadMerchant(Merchant merchant, IDataReader dataReader);
    }
}
