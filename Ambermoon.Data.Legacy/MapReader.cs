using System;
using System.Collections.Generic;

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
                            MapEventId = tileData[1] & 0x1fu,
                            Unknown = (tileData[2] & 0xf8u) >> 3,
                            // TODO: TileType
                        };
                    }
                }

                uint numMapEvents = dataReader.ReadWord();

                // Assumption: There are numMapEvents + 1 values.
                // Each is a offset. The offset is relative to the position behind these offsets
                // and the offset is multiplied by 12.
                // A map event has 12 bytes. Per map event id (tile event) there can be multiple
                // events (I guess depending on some conditions).
                // So there are offset - lastOffset map events associated with an event id.
                uint[] offsets = new uint[numMapEvents + 1];

                for (uint i = 0; i < numMapEvents + 1; ++i)
                    offsets[i] = dataReader.ReadWord();

                map.Events.Clear();

                if (numMapEvents > 0)
                {
                    for (uint i = 0; i < numMapEvents; ++i)
                    {
                        uint numSubEvents = offsets[i + 1] - offsets[i];
                        List<MapEvent> mapEvents = new List<MapEvent>((int)numSubEvents);

                        for (uint s = 0; s < numSubEvents; ++s)
                        {
                            // Each event seems to have 12 bytes of data starting with the event type byte
                            mapEvents.Add(ParseEvent(dataReader));
                        }

                        map.Events.Add(mapEvents);
                    }
                }

                // TODO

                //if (dataReader.ReadWord() != 0) // 00 00 -> end of map
                    //throw new AmbermoonException(ExceptionScope.Data, "Invalid map format");
            }
            else
            {
                // TODO: 3D maps (looks like 1 word per tile -> first byte texture index, second maybe overlay texture index?)
            }

            // Remaining bytes unknown
        }

        static MapEvent ParseEvent(IDataReader dataReader)
        {
            MapEvent mapEvent;
            var type = (MapEventType)dataReader.ReadByte();

            switch (type)
            {
                case MapEventType.MapChange:
                    // 1. byte is the x coordinate
                    // 2. byte is the y coordinate
                    // Then 3 unknown bytes
                    // Then a word for the map index
                    // Then 4 unknown bytes (seem to be 00 FF FF FF)
                    uint x = dataReader.ReadByte();
                    uint y = dataReader.ReadByte();
                    dataReader.ReadBytes(3);
                    uint mapIndex = dataReader.ReadWord();
                    dataReader.ReadBytes(3);
                    mapEvent = new MapChangeEvent
                    {
                        MapIndex = mapIndex,
                        X = x,
                        Y = y
                    };
                    break;
                default:
                    // TODO
                    mapEvent = new MapEvent();
                    dataReader.ReadBytes(11);
                    break;
            }

            mapEvent.Type = type;

            return mapEvent;
        }
    }
}
