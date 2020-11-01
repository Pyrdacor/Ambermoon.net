namespace Ambermoon.Data.Serialization
{
    public interface IChestWriter
    {
        void WriteChest(Chest chest, IDataWriter dataWriter);
    }
}
