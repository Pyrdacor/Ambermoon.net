namespace Ambermoon.Data.Serialization
{
    public interface IAutomapWriter
    {
        void WriteAutomap(Automap automap, IDataWriter dataWriter);
    }
}
