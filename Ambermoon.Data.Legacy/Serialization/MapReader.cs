using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data.Legacy.Serialization
{
    public class MapReader : IMapReader
    {
        public void ReadMap(Map map, IDataReader dataReader, IDataReader textDataReader, Dictionary<uint, Tileset> tilesets)
        {
            // Load map texts
            map.Texts = TextReader.ReadTexts(textDataReader);

            map.Flags = (MapFlags)dataReader.ReadWord();
            map.Type = (MapType)dataReader.ReadByte();

            if (map.Type != MapType.Map2D && map.Type != MapType.Map3D)
                throw new Exception("Invalid map data.");

            map.MusicIndex = dataReader.ReadByte();
            map.Width = dataReader.ReadByte();
            map.Height = dataReader.ReadByte();
            map.TilesetOrLabdataIndex = dataReader.ReadByte();

            map.NPCGfxIndex = dataReader.ReadByte();
            map.LabyrinthBackgroundIndex = dataReader.ReadByte();
            map.PaletteIndex = dataReader.ReadByte();
            map.World = (World)dataReader.ReadByte();

            if (dataReader.ReadByte() != 0) // end of map header
                throw new AmbermoonException(ExceptionScope.Data, "Invalid map data");

            // Up to 32 character references (10 bytes each -> total 320 bytes)
            for (int i = 0; i < 32; ++i)
            {
                var index = dataReader.ReadByte();
                var onlyMoveWhenSeePlayer = dataReader.ReadByte() != 0; // TODO: Not 100% sure about this
                var type = dataReader.ReadByte();
                var eventIndex = dataReader.ReadByte();
                var gfxIndex = dataReader.ReadWord();
                var tileFlags = dataReader.ReadDword();

                map.CharacterReferences[i] = index == 0 ? null : new Map.CharacterReference
                {
                    Index = index,
                    OnlyMoveWhenSeePlayer = onlyMoveWhenSeePlayer,
                    Type = (CharacterType)(type & 0x03),
                    CharacterFlags = (Map.CharacterReference.Flags)(type >> 2),
                    EventIndex = eventIndex,
                    GraphicIndex = gfxIndex,
                    TileFlags = (Tileset.TileFlags)tileFlags,
                    CombatBackgroundIndex = tileFlags >> 28
                };

                // Note: Map 258 has 3 characters but one seems to be not used anymore.
                // It is not directly following the other 2 and has no movement data
                // hence it is not marked as random moving. I guess this is a relict.
                // This character is marked as party member but there is none on this map.
                // To avoid problems we null all further character references if we found
                // the first empty one.
                if (map.CharacterReferences[i] == null)
                {
                    for (int j = i + 1; j < 32; ++j)
                    {
                        map.CharacterReferences[j] = null;
                        dataReader.Position += 10;
                    }

                    break;
                }
            }

            if (map.Type == MapType.Map2D)
            {
                map.InitialTiles = new Map.Tile[map.Width, map.Height];
                map.InitialBlocks = null;
                map.Tiles = new Map.Tile[map.Width, map.Height];
                map.Blocks = null;
                var tileset = tilesets[map.TilesetOrLabdataIndex];

                for (int y = 0; y < map.Height; ++y)
                {
                    for (int x = 0; x < map.Width; ++x)
                    {
                        var tileData = dataReader.ReadBytes(4);
                        map.InitialTiles[x, y] = new Map.Tile
                        {
                            BackTileIndex = tileData[0],
                            FrontTileIndex = (uint)(tileData[2] << 8) | tileData[3],
                            MapEventId = tileData[1]
                        };
                        map.Tiles[x, y] = new Map.Tile
                        {
                            BackTileIndex = tileData[0],
                            FrontTileIndex = (uint)(tileData[2] << 8) | tileData[3],
                            MapEventId = tileData[1]
                        };
                        map.InitialTiles[x, y].Type = map.Tiles[x, y].Type = Map.TileTypeFromTile(map.InitialTiles[x, y], tileset);
                    }
                }
            }
            else
            {
                map.InitialBlocks = new Map.Block[map.Width, map.Height];
                map.InitialTiles = null;
                map.Blocks = new Map.Block[map.Width, map.Height];
                map.Tiles = null;

                for (int y = 0; y < map.Height; ++y)
                {
                    for (int x = 0; x < map.Width; ++x)
                    {
                        var blockData = dataReader.ReadBytes(2);
                        map.InitialBlocks[x, y] = new Map.Block
                        {
                            ObjectIndex = blockData[0] <= 100 ? (uint)blockData[0] : 0,
                            WallIndex = blockData[0] >= 101 && blockData[0] < 255 ? (uint)blockData[0] - 100 : 0,
                            MapEventId = blockData[1],
                            MapBorder = blockData[0] == 255
                        };
                        map.Blocks[x, y] = new Map.Block
                        {
                            ObjectIndex = blockData[0] <= 100 ? (uint)blockData[0] : 0,
                            WallIndex = blockData[0] >= 101 && blockData[0] < 255 ? (uint)blockData[0] - 100 : 0,
                            MapEventId = blockData[1],
                            MapBorder = blockData[0] == 255
                        };
                    }
                }
            }

            EventReader.ReadEvents(dataReader, map.Events, map.EventList);

            // For each character reference the positions or movement paths are stored here.
            // For random movement there are 2 bytes (x and y). Otherwise there are 288 positions
            // each has 2 bytes (x and y). Each position is for one 5 minute chunk of the day.
            // There are 24 hours * 60 minutes = 1440 minutes per day. Divided by 5 you get 288.
            // A position of 0,0 is possible. It means "not visible on the map".
            foreach (var characterReference in map.CharacterReferences)
            {
                if (characterReference == null)
                    continue;

                if (characterReference.Type == CharacterType.Monster ||
                    characterReference.CharacterFlags.HasFlag(Map.CharacterReference.Flags.RandomMovement))
                {
                    // For random movement only the start position is given.
                    characterReference.Positions.Add(new Position(dataReader.ReadByte(), dataReader.ReadByte()));
                }
                else
                {
                    for (int i = 0; i < 288; ++i)
                        characterReference.Positions.Add(new Position(dataReader.ReadByte(), dataReader.ReadByte()));
                }
            }

            uint gotoPointCount = dataReader.ReadWord();

            for (uint i = 0; i < gotoPointCount; ++i)
            {
                map.GotoPoints.Add(new Map.GotoPoint
                {
                    X = dataReader.ReadByte(),
                    Y = dataReader.ReadByte(),
                    Direction = (CharacterDirection)dataReader.ReadByte(),
                    Index = dataReader.ReadByte(),
                    Name = dataReader.ReadString(16).Trim('\0', ' ')
                });
            }

            if (map.Type == MapType.Map3D)
            {
                map.EventAutomapTypes = dataReader.ReadBytes(map.EventList.Count).Select(a => (AutomapType)a).ToList();
            }
        }
    }
}
