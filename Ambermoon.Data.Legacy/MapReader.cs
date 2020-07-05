using System;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy
{
    // TODO: Where is the information stored for:
    // - tile blocking states [would make sense to find it in tileset tile data]
    // - chair / bed [would make sense to find it in tileset tile data]
    // - interaction type (move onto, hand, eye, mouth, etc)
    public class MapReader : IMapReader
    {
        public void ReadMap(Map map, IDataReader dataReader)
        {
            map.Flags = (MapFlags)dataReader.ReadWord();
            map.Type = (MapType)dataReader.ReadByte();

            if (map.Type != MapType.Map2D && map.Type != MapType.Map3D)
                throw new Exception("Invalid map data.");

            map.MusicIndex = dataReader.ReadByte();
            map.Width = dataReader.ReadByte();
            map.Height = dataReader.ReadByte();
            map.TilesetIndex = dataReader.ReadByte();

            map.NPCGfxIndex = dataReader.ReadByte();
            map.LabyrinthBackIndex = dataReader.ReadByte();
            map.PaletteIndex = dataReader.ReadByte();
            map.World = (World)dataReader.ReadByte();

            if (dataReader.ReadByte() != 0) // end of map header
                throw new AmbermoonException(ExceptionScope.Data, "Invalid map data");

            dataReader.Position += 320; // Unknown 320 bytes

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
            }
            else
            {
                // TODO: 3D maps (looks like 1 word per tile -> first byte texture index, second maybe overlay texture index?)
                for (int y = 0; y < map.Height; ++y)
                {
                    for (int x = 0; x < map.Width; ++x)
                    {
                        dataReader.ReadWord();
                    }
                }
            }

            uint numMapEvents = dataReader.ReadWord();

            // There are numMapEvents 16 bit values.
            // Each gives the offset of the map event to use.
            // Each event data is 12 bytes in size.

            // After this the total number of map events is given.
            // Map events can be chained (linked list). Each chain
            // is identified by a map event id on some map tiles.

            // The last two bytes of each event data contain the
            // offset of the next event data or 0xFFFF if this is
            // the last map event of the chain/list.
            // Note that the linked list can have a non-linear order.

            // E.g. in map 8 the first map event (index 0) references
            // map event 2 and this references map event 1 which is the
            // end chunk of the first map event chain.
            uint[] mapEventOffsets = new uint[numMapEvents];

            for (uint i = 0; i < numMapEvents; ++i)
                mapEventOffsets[i] = dataReader.ReadWord();

            map.Events.Clear();

            if (numMapEvents > 0)
            {
                uint numTotalMapEvents = dataReader.ReadWord();
                var mapEvents = new List<Tuple<MapEvent, int>>();

                // read all map events and the next map event index
                for (uint i = 0; i < numTotalMapEvents; ++i)
                {
                    mapEvents.Add(Tuple.Create(ParseEvent(dataReader), (int)dataReader.ReadWord()));
                }

                foreach (var mapEvent in mapEvents)
                {
                    mapEvent.Item1.Next = mapEvent.Item2 == 0xffff ? null : mapEvents[mapEvent.Item2].Item1;
                }

                foreach (var mapEventOffset in mapEventOffsets)
                    map.Events.Add(mapEvents[(int)mapEventOffset].Item1);
            }

            // TODO

            //if (dataReader.ReadWord() != 0) // 00 00 -> end of map
                //throw new AmbermoonException(ExceptionScope.Data, "Invalid map format");

            // Remaining bytes unknown
        }

        static MapEvent ParseEvent(IDataReader dataReader)
        {
            MapEvent mapEvent;
            var type = (MapEventType)dataReader.ReadByte();

            switch (type)
            {
                case MapEventType.MapChange:
                    {
                        // 1. byte is the x coordinate
                        // 2. byte is the y coordinate
                        // Then 3 unknown bytes
                        // Then a word for the map index
                        // Then 2 unknown bytes (seem to be 00 FF)
                        uint x = dataReader.ReadByte();
                        uint y = dataReader.ReadByte();
                        dataReader.ReadBytes(3);
                        uint mapIndex = dataReader.ReadWord();
                        dataReader.ReadBytes(2);
                        mapEvent = new MapChangeEvent
                        {
                            MapIndex = mapIndex,
                            X = x,
                            Y = y
                        };
                        break;
                    }
                case MapEventType.Chest:
                    {
                        // 1. byte unknown (seen 0x64 when the chest was locked, otherwise 0x00)
                        // 2. byte unknown
                        // 3. byte is the lock status (0xFF is open, 0x02 is a special key)
                        // 4. byte is the chest index
                        // 5. byte (0 = chest, 1 = pile/removable loot or item)
                        // word at position 6 is the key index if a key must unlock it
                        // the last word is unknown (seems to be 0xffff for unlocked, and some id otherwise)
                        // maybe open it will trigger change/something else?
                        dataReader.ReadBytes(2); // Unknown
                        var lockType = (ChestMapEvent.LockType)dataReader.ReadByte();
                        uint chestIndex = dataReader.ReadByte();
                        bool removeWhenEmpty = dataReader.ReadByte() != 0;
                        uint keyIndex = dataReader.ReadWord();
                        dataReader.ReadWord(); // Unknown
                        mapEvent = new ChestMapEvent
                        {
                            Lock = lockType,
                            ChestIndex = chestIndex,
                            RemoveWhenEmpty = removeWhenEmpty,
                            KeyIndex = keyIndex
                        };
                        break;
                    }
                default:
                    {
                        // TODO
                        mapEvent = new DebugMapEvent
                        {
                            Data = dataReader.ReadBytes(9)
                        };
                        break;
                    }
            }

            mapEvent.Type = type;

            return mapEvent;
        }
    }
}
