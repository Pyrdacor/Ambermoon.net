namespace Ambermoon.Data.Serialization
{
    public interface IChestReader
    {
        void ReadChest(Chest chest, IDataReader dataReader);
    }
}
