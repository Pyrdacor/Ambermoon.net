using System;

namespace Ambermoon.Data.Legacy
{
    public class MapReader : IMapReader
    {
        public void ReadMap(Map map, IDataReader dataReader)
        {
            dataReader.ReadWord(); // Unknown
            map.Type = (MapType)dataReader.ReadByte();

            if (map.Type != MapType.Map2D && map.Type != MapType.Map3D)
                throw new Exception("Invalid map data.");

            dataReader.ReadByte(); // Unknown
            map.Width = dataReader.ReadByte();
            map.Height = dataReader.ReadByte();
            map.TilesetIndex = dataReader.ReadByte();

            dataReader.ReadByte(); // Unknown
            dataReader.ReadDword(); // Unknown
            dataReader.Position += 320; // Event data (format unknown)

            if (map.Type == MapType.Map2D)
            {
                map.Tiles = new Map.Tile[map.Width, map.Height];

                for (int y = 0; y < map.Height; ++y)
                {
                    for (int x = 0; x < map.Width; ++x)
                    {
                        var tileData = dataReader.ReadBytes(4);
                        map.Tiles[x, y] = new Map.Tile
                        {
                            BackTileIndex = ((uint)(tileData[1] & 0xe0) << 3) | tileData[0],
                            FrontTileIndex = ((uint)(tileData[2] & 0x07) << 8) | tileData[3],
                            InteractionEventId = tileData[1] & 0x1fu,
                            Unknown = (tileData[2] & 0xf8u) >> 3,
                            // TODO: TileType
                        };
                    }
                }
            }
            else
            {
                // TODO: 3D maps (looks like 1 word per tile -> first byte texture index, second maybe overlay texture index?)
            }

            // Remaining bytes unknown
        }
    }
}
