using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Legacy.Serialization
{
    public class TilesetReader : ITilesetReader
    {
        public void ReadTileset(Tileset tileset, IDataReader dataReader)
        {
            int numTiles = dataReader.ReadWord();
            tileset.Tiles = new Tileset.Tile[numTiles];

            for (int i = 0; i < numTiles; ++i)
            {
                var tileFlags = dataReader.ReadDword();

                tileset.Tiles[i] = new Tileset.Tile
                {
                    GraphicIndex = dataReader.ReadWord(),
                    NumAnimationFrames = dataReader.ReadByte(),
                    ColorIndex = dataReader.ReadByte(),
                    Flags = (Tileset.TileFlags)tileFlags
                };
            }
        }
    }
}
