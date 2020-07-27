namespace Ambermoon.Data
{
    public interface IItemReader
    {
        void ReadItem(Item item, IDataReader dataReader);
    }
}
