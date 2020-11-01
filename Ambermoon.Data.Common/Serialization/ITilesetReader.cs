namespace Ambermoon.Data.Serialization
{
    public interface ITilesetReader
    {
        void ReadTileset(Tileset tileset, IDataReader dataReader);
    }
}
