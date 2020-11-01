namespace Ambermoon.Data.Serialization
{
    public interface IAutomapReader
    {
        void ReadAutomap(Automap automap, IDataReader dataReader);
    }
}
