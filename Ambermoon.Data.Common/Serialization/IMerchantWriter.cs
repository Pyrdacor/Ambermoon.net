namespace Ambermoon.Data.Serialization
{
    public interface IMerchantWriter
    {
        void WriteMerchant(Merchant merchant, IDataWriter dataWriter);
    }
}
