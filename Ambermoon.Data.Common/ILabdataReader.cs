namespace Ambermoon.Data
{
    public interface ILabdataReader
    {
        void ReadLabdata(Labdata labdata, IDataReader dataReader, IGameData gameData);
    }
}
