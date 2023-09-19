namespace Ambermoon.Data.Serialization
{
    public interface ILabdataReader
    {
        void ReadLabdata(Labdata labdata, IDataReader dataReader, IGameData gameData);
        void ReadLabdataWithoutGraphics(Labdata labdata, IDataReader dataReader);
    }
}
