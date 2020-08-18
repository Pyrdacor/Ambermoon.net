namespace Ambermoon.Data
{
    public interface ITilesetReader
    {
        void ReadTileset(Tileset tileset, IDataReader dataReader);
    }
}
