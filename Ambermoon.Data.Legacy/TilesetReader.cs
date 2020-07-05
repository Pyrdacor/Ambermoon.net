using System;

namespace Ambermoon.Data.Legacy
{
    public class TilesetReader : ITilesetReader
    {
        public void ReadTileset(Tileset tileset, IDataReader dataReader)
        {
            int numTiles = dataReader.ReadWord();
            tileset.Tiles = new Tileset.Tile[numTiles];

            for (int i = 0; i < numTiles; ++i)
            {
                tileset.Tiles[i] = new Tileset.Tile();
                tileset.Tiles[i].Unknown1 = dataReader.ReadDword(); // Unknown
                tileset.Tiles[i].GraphicIndex = dataReader.ReadWord();
                tileset.Tiles[i].NumAnimationFrames = dataReader.ReadByte();
                tileset.Tiles[i].Unknown2 = dataReader.ReadByte(); // Unknown
            }
        }
    }
}
