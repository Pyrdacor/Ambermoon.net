using System;
using System.Collections.Generic;
using System.Linq;
using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Legacy.Serialization
{
    public static class MapWriter
    {
        public static void WriteMapHeader(Map map, IDataWriter dataWriter)
        {
            dataWriter.Write((ushort)map.Flags);
            dataWriter.Write((byte)map.Type);
            dataWriter.Write((byte)map.MusicIndex);
            dataWriter.Write((byte)map.Width);
            dataWriter.Write((byte)map.Height);
            dataWriter.Write((byte)map.TilesetOrLabdataIndex);
            dataWriter.Write((byte)map.NPCGfxIndex);
            dataWriter.Write((byte)map.LabyrinthBackgroundIndex);
            dataWriter.Write((byte)map.PaletteIndex);
            dataWriter.Write((byte)map.World);
            dataWriter.Write(0);
        }

        public static void WriteMap(Map map, IDataWriter dataWriter)
        {
            WriteMapHeader(map, dataWriter);

            // Up to 32 character references (10 bytes each -> total 320 bytes)
            for (int i = 0; i < 32; ++i)
            {
                if (map.CharacterReferences[i] == null)
                {
                    for (int j = 0; j < 10; ++j)
                        dataWriter.Write(0);
                }
                else
                {
                    dataWriter.Write((byte)map.CharacterReferences[i].Index);
                    dataWriter.Write((byte)map.CharacterReferences[i].CollisionClass);
                    var typeAndFlags = (byte)((byte)map.CharacterReferences[i].Type | ((byte)map.CharacterReferences[i].CharacterFlags << 2));
                    dataWriter.Write(typeAndFlags);
                    dataWriter.Write((byte)map.CharacterReferences[i].EventIndex);
                    dataWriter.Write((ushort)map.CharacterReferences[i].GraphicIndex);
                    dataWriter.Write((uint)map.CharacterReferences[i].TileFlags);
                }
            }

            if (map.Type == MapType.Map2D)
            {
                for (int y = 0; y < map.Height; ++y)
                {
                    for (int x = 0; x < map.Width; ++x)
                    {
                        var tile = map.InitialTiles[x, y];
                        dataWriter.Write((byte)tile.BackTileIndex);
                        dataWriter.Write((byte)tile.MapEventId);
                        dataWriter.Write((ushort)tile.FrontTileIndex);
                    }
                }
            }
            else
            {
                for (int y = 0; y < map.Height; ++y)
                {
                    for (int x = 0; x < map.Width; ++x)
                    {
                        var block = map.InitialBlocks[x, y];
                        var blockDataIndex = block.MapBorder ? 255
                            : block.ObjectIndex != 0 ? block.ObjectIndex
                            : block.WallIndex != 0 ? 100u + block.WallIndex
                            : 0;
                        dataWriter.Write((byte)blockDataIndex);
                        dataWriter.Write((byte)block.MapEventId);
                    }
                }
            }

            EventWriter.WriteEvents(dataWriter, map.Events, map.EventList);

            int characterDataOffset = dataWriter.Position;
            var characterPositionDataHeaderLocations = new Dictionary<int, byte[]>();

            // For each character reference the positions or movement paths are stored here.
            // For random movement there are 2 bytes (x and y). Otherwise there are 288 positions
            // each has 2 bytes (x and y). Each position is for one 5 minute chunk of the day.
            // There are 24 hours * 60 minutes = 1440 minutes per day. Divided by 5 you get 288.
            // A position of 0,0 is possible. It means "not visible on the map".
            foreach (var characterReference in map.CharacterReferences)
            {
                if (characterReference == null || characterReference.Index == 0)
                    continue;

                if (characterReference.Type == CharacterType.Monster ||
                    characterReference.CharacterFlags.HasFlag(Map.CharacterReference.Flags.RandomMovement) ||
                    characterReference.CharacterFlags.HasFlag(Map.CharacterReference.Flags.Stationary))
                {
                    // For monsters, random movement or stationary only the start position is given.
                    dataWriter.Write((byte)characterReference.Positions[0].X);
                    dataWriter.Write((byte)characterReference.Positions[0].Y);
                }
                else if (characterReference.CharacterFlags.HasFlag(Map.CharacterReference.Flags.SpecialMovement))
                {
                    switch (characterReference.SpecialMoveType)
                    {
                        case Map.CharacterReference.SpecialMovementType.CyclicPath:
                        case Map.CharacterReference.SpecialMovementType.AlternatingPath:
                        {
                            ushort numPositions = (ushort)characterReference.Positions.Count;
                            var positionData = new byte[2 + numPositions * 2];

                            positionData[0] = (byte)(numPositions >> 8);
                            positionData[1] = (byte)(numPositions & 0xff);

                            for (int i = 0; i < numPositions; i++)
                            {
                                positionData[2 + i * 2] = (byte)characterReference.Positions[i].X;
                                positionData[3 + i * 2] = (byte)characterReference.Positions[i].Y;
                            }

                            characterPositionDataHeaderLocations.Add(dataWriter.Position, positionData);
                            dataWriter.Write((ushort)((int)characterReference.SpecialMoveType << 12));

                            break;
                        }
                        case Map.CharacterReference.SpecialMovementType.ReuseCyclicPath:
                        {
                            ushort header = (ushort)(characterReference.SpecialMoveCharacterIndex ?? throw new NullReferenceException(nameof(characterReference.SpecialMoveCharacterIndex)));
                            header |= (ushort)((characterReference.SpecialMoveOffset ?? throw new NullReferenceException(nameof(characterReference.SpecialMoveOffset))) << 5);

                            dataWriter.Write(header);

                            break;
                        }
                        case Map.CharacterReference.SpecialMovementType.CircleCenterPosition:
                        {
                            // TODO
                            throw new NotImplementedException();
                        }
                    }

                    foreach (var location in characterPositionDataHeaderLocations.OrderBy(l => l.Key))
                    {
                        int type = dataWriter.GetBytes(location.Key, 1)[0];
                        dataWriter.Replace(location.Key, (ushort)((type << 8) | dataWriter.Position));
                        dataWriter.Write(location.Value);
                    }
                }
                else
                {
                    for (int i = 0; i < 288; ++i)
                    {
                        dataWriter.Write((byte)characterReference.Positions[i].X);
                        dataWriter.Write((byte)characterReference.Positions[i].Y);
                    }
                }
            }

            dataWriter.Write((ushort)map.GotoPoints.Count);

            foreach (var gotoPoint in map.GotoPoints)
            {
                dataWriter.Write((byte)gotoPoint.X);
                dataWriter.Write((byte)gotoPoint.Y);
                dataWriter.Write((byte)gotoPoint.Direction);
                dataWriter.Write((byte)gotoPoint.Index);

                string name = gotoPoint.Name;

                if (name.Length > 15)
                    name = name.Substring(0, 15);
                dataWriter.WriteWithoutLength(name);
                for (int i = name.Length; i < 16; ++i)
                    dataWriter.Write(0);
            }

            if (map.Type == MapType.Map3D)
            {
                if (map.EventAutomapTypes == null || map.EventAutomapTypes.Count != map.EventList.Count)
                    throw new AmbermoonException(ExceptionScope.Data, "For 3D maps the EventAutomapTypes collection size must match the EventList collection size.");
                foreach (var automapType in map.EventAutomapTypes)
                {
                    if (automapType == AutomapType.Invalid)
                        dataWriter.Write(0);
                    else
                        dataWriter.Write((byte)automapType);
                }
            }
        }
    }
}
