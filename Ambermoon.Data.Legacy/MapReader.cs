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
        void ReadMapTexts(Map map, IDataReader textDataReader)
        {
            map.Texts.Clear();

            if (textDataReader != null)
            {
                int numMapTexts = textDataReader.ReadWord();
                if (numMapTexts == 0)
                {
                    textDataReader.ReadWord(); // unknown
                    numMapTexts = textDataReader.ReadWord();

                    string currentText = "";

                    for (int i = 0; i < numMapTexts; ++i)
                    {
                        while (true)
                        {
                            var ch = textDataReader.ReadChar();

                            if (ch == "\0" || (textDataReader.Position == textDataReader.Size && i == numMapTexts - 1))
                            {
                                map.Texts.Add(currentText.Trim());
                                currentText = "";
                                break;
                            }

                            currentText += ch;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < numMapTexts; ++i)
                        textDataReader.ReadWord(); // unknown numMapTexts words

                    string currentText = "";

                    for (int i = 0; i < numMapTexts; ++i)
                    {
                        while (true)
                        {
                            var ch = textDataReader.ReadChar();

                            if (ch == "\0" || (textDataReader.Position == textDataReader.Size && i == numMapTexts - 1))
                            {
                                map.Texts.Add(currentText.Trim());
                                currentText = "";
                                break;
                            }

                            currentText += ch;
                        }
                    }
                }
            }
        }

        public void ReadMap(Map map, IDataReader dataReader, IDataReader textDataReader)
        {
            // Load map texts
            ReadMapTexts(map, textDataReader);

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

            map.Tiles = new Map.Tile[map.Width, map.Height];

            if (map.Type == MapType.Map2D)
            {
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
                            Unused = (tileData[2] & 0xf8u) >> 3,
                            // TODO: TileType
                        };
                    }
                }
            }
            else
            {
                for (int y = 0; y < map.Height; ++y)
                {
                    for (int x = 0; x < map.Width; ++x)
                    {
                        var tileData = dataReader.ReadBytes(2);
                        map.Tiles[x, y] = new Map.Tile
                        {
                            BackTileIndex = ((uint)(tileData[1] & 0xe0) << 3) | tileData[0],
                            MapEventId = tileData[1] & 0x1fu
                            // TODO: blocking etc
                        };
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
                    var mapEvent = ParseEvent(dataReader);
                    mapEvent.Index = i + 1;
                    mapEvents.Add(Tuple.Create(mapEvent, (int)dataReader.ReadWord()));
                }

                foreach (var mapEvent in mapEvents)
                {
                    mapEvent.Item1.Next = mapEvent.Item2 == 0xffff ? null : mapEvents[mapEvent.Item2].Item1;
                }

                foreach (var mapEventOffset in mapEventOffsets)
                    map.Events.Add(mapEvents[(int)mapEventOffset].Item1);

                if (false/*map.Index == 267 || map.Index == 258 || map.Index == 262*/)
                {
                    int foo = 1;
                    foreach (var ev in map.Events)
                    {
                        Console.WriteLine($"{foo++}: {ev.Type} -> {ev}");
                        var x = ev.Next;
                        while (x != null)
                        {
                            Console.WriteLine($"\t{x.Type} -> {x}");
                            x = x.Next;
                        }
                    }
                }
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
                        var direction = (CharacterDirection)dataReader.ReadByte();
                        var unknown1 = dataReader.ReadBytes(2);
                        uint mapIndex = dataReader.ReadWord();
                        var unknown2 = dataReader.ReadBytes(2);
                        mapEvent = new MapChangeEvent
                        {
                            MapIndex = mapIndex,
                            X = x,
                            Y = y,
                            Direction = direction,
                            Unknown1 = unknown1,
                            Unknown2 = unknown2
                        };
                        break;
                    }
                case MapEventType.Chest:
                    {
                        // 1. byte are the lock flags
                        // 2. byte is unknown (always 0 except for one chest with 20 blue discs which has 0x32 and lock flags of 0x00)
                        // 3. byte is unknown (0xff for unlocked chests)
                        // 4. byte is the chest index (0-based)
                        // 5. byte (0 = chest, 1 = pile/removable loot or item) or "remove if empty"
                        // word at position 6 is the key index if a key must unlock it
                        // the last word is unknown (seems to be 0xffff for unlocked, and some id otherwise)
                        // maybe open it will trigger change/something else?
                        var lockType = (ChestMapEvent.LockFlags)dataReader.ReadByte();
                        var unknown1 = dataReader.ReadWord(); // Unknown
                        uint chestIndex = dataReader.ReadByte();
                        bool removeWhenEmpty = dataReader.ReadByte() != 0;
                        uint keyIndex = dataReader.ReadWord();
                        var unknown2 = dataReader.ReadWord(); // Unknown
                        mapEvent = new ChestMapEvent
                        {
                            Unknown1 = unknown1,
                            Unknown2 = unknown2,
                            Lock = lockType,
                            ChestIndex = chestIndex,
                            RemoveWhenEmpty = removeWhenEmpty,
                            KeyIndex = keyIndex
                        };
                        break;
                    }
                case MapEventType.TextEvent:
                    {
                        // 3 unknown bytes
                        // 4. word is the map text index
                        // 4 unknown bytes
                        var unknown1 = dataReader.ReadBytes(4);
                        var textIndex = dataReader.ReadByte();
                        var unknown2 = dataReader.ReadBytes(4);
                        mapEvent = new TextEvent
                        {
                            TextIndex = textIndex,
                            Unknown1 = unknown1,
                            Unknown2 = unknown2
                        };
                        break;
                    }
                case MapEventType.Spinner:
                    {
                        var direction = (CharacterDirection)dataReader.ReadByte();
                        var unknown1 = dataReader.ReadBytes(8);
                        mapEvent = new SpinnerEvent
                        {
                            Direction = direction,
                            Unknown1 = unknown1,
                        };
                        break;
                    }
                case MapEventType.Damage:
                    {
                        var unknown1 = dataReader.ReadBytes(9);
                        mapEvent = new DamageEvent
                        {
                            Unknown1 = unknown1,
                        };
                        break;
                    }
                case MapEventType.Riddlemouth:
                    {
                        var introTextIndex = dataReader.ReadByte();
                        var solutionTextIndex = dataReader.ReadByte();
                        var unknown1 = dataReader.ReadBytes(7);
                        mapEvent = new RiddlemouthEvent
                        {
                            IntroTextIndex = introTextIndex,
                            SolutionTextIndex = solutionTextIndex,
                            Unknown1 = unknown1
                        };
                        break;
                    }
                case MapEventType.ChangePlayerAttribute:
                    {
                        var unknown1 = dataReader.ReadBytes(6);
                        var attribute = (Attribute)dataReader.ReadByte();
                        var unknown2 = dataReader.ReadByte();
                        var value = dataReader.ReadByte();

                        mapEvent = new ChangePlayerAttributeEvent
                        {
                            Attribute = attribute,
                            Value = value,
                            Unknown1 = unknown1,
                            Unknown2 = unknown2
                        };
                        break;
                    }
                case MapEventType.ChangeTile:
                    {
                        var x = dataReader.ReadByte();
                        var y = dataReader.ReadByte();
                        var unknown1 = dataReader.ReadBytes(3);
                        var frontTileIndex = dataReader.ReadWord();
                        var unknown2 = dataReader.ReadBytes(2);
                        mapEvent = new ChangeTileEvent
                        {
                            X = x,
                            Y = y,
                            FrontTileIndex = frontTileIndex,
                            Unknown1 = unknown1,
                            Unknown2 = unknown2
                        };
                        break;
                    }
                case MapEventType.StartBattle:
                    {
                        var unknown1 = dataReader.ReadBytes(6);
                        var monsterGroupIndex = dataReader.ReadByte();
                        var unknown2 = dataReader.ReadBytes(2);
                        mapEvent = new StartBattleEvent
                        {
                            MonsterGroupIndex = monsterGroupIndex,
                            Unknown1 = unknown1,
                            Unknown2 = unknown2
                        };
                        break;
                    }
                case MapEventType.Condition:
                    {
                        var conditionType = (ConditionEvent.ConditionType)dataReader.ReadByte(); // TODO: this needs more research
                        var value = dataReader.ReadByte();
                        var unknown1 = dataReader.ReadBytes(4);
                        var objectIndex = dataReader.ReadByte();
                        var unknown2 = dataReader.ReadBytes(2);
                        mapEvent = new ConditionEvent
                        {
                            TypeOfCondition = conditionType,
                            ObjectIndex = objectIndex,
                            Value = value,
                            Unknown1 = unknown1,
                            Unknown2 = unknown2
                        };
                        break;
                    }
                case MapEventType.Action:
                    {
                        var actionType = (ActionEvent.ActionType)dataReader.ReadByte(); // TODO: this needs more research
                        var value = dataReader.ReadByte();
                        var unknown1 = dataReader.ReadBytes(4);
                        var objectIndex = dataReader.ReadByte();
                        var unknown2 = dataReader.ReadBytes(2);
                        mapEvent = new ActionEvent
                        {
                            TypeOfAction = actionType,
                            ObjectIndex = objectIndex,
                            Value = value,
                            Unknown1 = unknown1,
                            Unknown2 = unknown2
                        };
                        break;
                    }
                case MapEventType.Question:
                    {
                        var textIndex = dataReader.ReadByte();
                        var unknown1 = dataReader.ReadBytes(8);
                        mapEvent = new QuestionEvent
                        {
                            TextIndex = textIndex,
                            Unknown1 = unknown1
                        };
                        break;
                    }
                case MapEventType.ChangeMusic:
                    {
                        var musicIndex = dataReader.ReadWord();
                        var volume = dataReader.ReadByte();
                        var unknown1 = dataReader.ReadBytes(6);
                        mapEvent = new ChangeMusicEvent
                        {
                            MusicIndex = musicIndex,
                            Volume = volume,
                            Unknown1 = unknown1
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
