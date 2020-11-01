namespace Ambermoon.Data.Serialization
{
    public interface IItemReader
    {
        void ReadItem(Item item, IDataReader dataReader);
    }
}
