using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Pyrdacor.Extensions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

internal class MapData : IFileSpec<MapData>, IFileSpec
{
    public static bool UseMapDataSharing { get; set; } = false;

    // The shared map data feature is a bit tricky because you would have to check all previous maps for
    // identical map data to be able to use it. For now, we keep a static list of potential maps where
    // we know they are identical in the original game data to the first map and can be used for testing.
    // We still will do the comparison as mods might change this.
    static readonly HashSet<uint> potentialSharedDataMapIndices =
    [
        2, 3, 4, 5, 6, 7, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
        21, 22, 23, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 47,
        48, 49, 50, 51, 52, 53, 55, 56, 63, 64, 65, 66, 67, 68, 76,
        77, 78, 79, 80, 81, 82, 83, 84, 95, 96, 97, 98, 99, 100, 111,
        112, 113, 114, 115, 127, 128, 129, 130, 143, 144, 145, 146,
        147, 148, 159, 160, 161, 162, 163, 173, 174, 175, 176, 177,
        178, 189, 190, 191, 192, 193, 194, 205, 206, 207, 208, 209,
        210, 221, 222, 223, 224, 225, 226, 235, 236, 237, 238, 239,
        240, 241, 242, 243, 244, 245, 251, 252, 253, 254, 255, 256
    ];

    public static string Magic => "MAP";
    public static byte SupportedVersion => 0;
    public static ushort PreferredCompression => ICompression.GetIdentifier<Deflate>();
    Map? map = null;
    ushort? templateMapIndex = null;
    bool resolved = false;
    readonly Dictionary<ushort, byte> mapEventOnTileIndex = [];

    public MapData()
    {

    }

    public MapData(Map map)
    {
        this.map = map;
        resolved = true;
    }

    public Map GetMap(IMapManager mapManager)
    {
        if (map == null)
            throw new NullReferenceException("Map was null");

        if (resolved || templateMapIndex == null)
            return map;

        resolved = true;

        var templateMap = mapManager.GetMap(templateMapIndex.Value);

        if (templateMap.Width != map.Width || templateMap.Height != map.Height)
            throw new AmbermoonException(ExceptionScope.Data, "Width or height mismatch with template map.");

        if (map.Type == MapType.Map2D)
        {
            map.Tiles = (Map.Tile[,])templateMap.InitialTiles.Clone();
            map.InitialTiles = (Map.Tile[,])templateMap.InitialTiles.Clone();

            AssignEvents(map.Tiles, map.InitialTiles);
        }
        else
        {
            map.Blocks = (Map.Block[,])templateMap.InitialBlocks.Clone();
            map.InitialBlocks = (Map.Block[,])templateMap.InitialBlocks.Clone();

            AssignEvents(map.Blocks, map.InitialBlocks);
        }

        void AssignEvents(Map.IEventOnMap[,] tiles, Map.IEventOnMap[,] initialTiles)
        {
            foreach (var (tileIndex, mapEventId) in mapEventOnTileIndex)
            {
                int x = tileIndex % map.Width;
                int y = tileIndex / map.Width;

                tiles[x, y].MapEventId = mapEventId;
                initialTiles[x, y].MapEventId = mapEventId;
            }
        }

        mapEventOnTileIndex.Clear();

        return map;
    }

    public void Read(IDataReader dataReader, uint _, GameData gameData, byte __)
    {
        map = new Map();

        MapReader.ReadMapHeader(map, dataReader);

        if (map.Flags.HasFlag(MapFlags.SharedMapData))
        {
            templateMapIndex = dataReader.ReadWord();
            resolved = false;
        }
        else
        {
            templateMapIndex = null;
            resolved = true;

            var tileset = gameData.GetTileset(map.TilesetOrLabdataIndex);

            if (map.Type == MapType.Map2D)
            {
                map.Tiles = new Map.Tile[map.Width, map.Height];
                map.InitialTiles = new Map.Tile[map.Width, map.Height];

                var backTiles = dataReader.ReadBytes(map.Width * map.Height);
                var frontTiles = dataReader.ReadBytes(map.Width * map.Height * 2);
                int index = 0;

                unsafe
                {
                    fixed (byte* bptr = frontTiles)
                    {
                        ushort* ptr = (ushort*)bptr;

                        for (int y = 0; y < map.Height; ++y)
                        {
                            for (int x = 0; x < map.Width; ++x)
                            {
                                uint frontTileIndex = *ptr++;

                                map.InitialTiles[x, y] = new Map.Tile
                                {
                                    BackTileIndex = backTiles[index],
                                    FrontTileIndex = frontTileIndex,
                                    MapEventId = 0
                                };
                                map.Tiles[x, y] = new Map.Tile
                                {
                                    BackTileIndex = backTiles[index],
                                    FrontTileIndex = frontTileIndex,
                                    MapEventId = 0
                                };
                                map.InitialTiles[x, y].Type = map.Tiles[x, y].Type = Map.TileTypeFromTile(map.InitialTiles[x, y], tileset);

                                ++index;
                            }
                        }
                    }
                }
            }
            else // 3D
            {
                map.Blocks = new Map.Block[map.Width, map.Height];
                map.InitialBlocks = new Map.Block[map.Width, map.Height];

                var indices = dataReader.ReadBytes(map.Width * map.Height);
                int index = 0;

                for (int y = 0; y < map.Height; ++y)
                {
                    for (int x = 0; x < map.Width; ++x)
                    {
                        uint blockDataIndex = indices[index++];

                        map.InitialBlocks[x, y] = new Map.Block
                        {
                            ObjectIndex = blockDataIndex <= 100 ? blockDataIndex : 0u,
                            WallIndex = blockDataIndex != 255 && blockDataIndex > 100 ? blockDataIndex - 100u : 0u,
                            MapBorder = blockDataIndex == 255,
                            MapEventId = 0
                        };
                        map.Blocks[x, y] = new Map.Block
                        {
                            ObjectIndex = blockDataIndex <= 100 ? blockDataIndex : 0u,
                            WallIndex = blockDataIndex != 255 && blockDataIndex > 100 ? blockDataIndex - 100u : 0u,
                            MapBorder = blockDataIndex == 255,
                            MapEventId = 0
                        };
                    }
                }
            }
        }

        EventReader.ReadEvents(dataReader, map.Events, map.EventList);

        int usedEventCount = dataReader.ReadWord();

        if (usedEventCount != 0)
        {
            if (map.EventList.Count == 0)
                throw new AmbermoonException(ExceptionScope.Data, "The map has no events but references some.");

            Action<ushort, byte> eventAssigner;

            if (map.Flags.HasFlag(MapFlags.SharedMapData))
            {
                eventAssigner = mapEventOnTileIndex.Add;
            }
            else
            {
                var initialEventsOnMap = map.Type == MapType.Map2D
                    ? map.InitialTiles.Cast<Map.IEventOnMap>().ToArray()
                    : map.InitialBlocks.Cast<Map.IEventOnMap>().ToArray();
                var eventsOnMap = map.Type == MapType.Map2D
                    ? map.Tiles.Cast<Map.IEventOnMap>().ToArray()
                    : map.Blocks.Cast<Map.IEventOnMap>().ToArray();

                eventAssigner = (tileIndex, mapEventId) =>
                {
                    initialEventsOnMap[tileIndex].MapEventId = mapEventId;
                    eventsOnMap[tileIndex].MapEventId = mapEventId;
                };
            }

            for (int i = 0; i < usedEventCount; ++i)
            {
                ushort tileIndex = dataReader.ReadWord();

                if (tileIndex >= map.Width * map.Height)
                    throw new AmbermoonException(ExceptionScope.Data, "Invalid tile index for map event.");

                byte mapEventId = dataReader.ReadByte();

                if (mapEventId >= map.EventList.Count)
                    throw new AmbermoonException(ExceptionScope.Data, "Invalid event index for map event.");

                eventAssigner(tileIndex, mapEventId);
            }
        }

        int characterCount = dataReader.ReadByte();

        if (characterCount > 32)
            throw new AmbermoonException(ExceptionScope.Data, "Too many characters on map.");

        for (int i = 0; i < characterCount; ++i)
        {
            var index = dataReader.ReadByte();
            var collisionClass = dataReader.ReadByte();
            var typeAndFlags = dataReader.ReadByte();
            var eventIndex = dataReader.ReadByte();
            var gfxIndex = dataReader.ReadWord();
            var tileFlags = dataReader.ReadDword();

            var character = map.CharacterReferences[i] = index == 0 ? null : new Map.CharacterReference
            {
                Index = index,
                Type = (CharacterType)(typeAndFlags & 0x03),
                CharacterFlags = (Map.CharacterReference.Flags)(typeAndFlags >> 2),
                CollisionClass = collisionClass,
                EventIndex = eventIndex,
                GraphicIndex = gfxIndex,
                TileFlags = (Tileset.TileFlags)tileFlags,
                CombatBackgroundIndex = tileFlags >> 28
            };

            if (character != null)
            {
                int positionCount = 288;

                if (character.CharacterFlags.HasFlag(Map.CharacterReference.Flags.RandomMovement))
                    positionCount = 1;
                else if (character.Type != CharacterType.Monster && character.CharacterFlags.HasFlag(Map.CharacterReference.Flags.Stationary))
                    positionCount = 1;

                for (int p = 0; p < positionCount; ++p)
                    character.Positions.Add(new Position(dataReader.ReadByte(), dataReader.ReadByte()));
            }
        }

        if (map.Type == MapType.Map3D)
        {
            int gotoPointCount = dataReader.ReadByte();

            for (int i = 0; i < gotoPointCount; ++i)
            {
                uint x = dataReader.ReadByte();
                uint y = dataReader.ReadByte();
                var direction = (CharacterDirection)dataReader.ReadByte();
                byte index = dataReader.ReadByte();

                map.GotoPoints.Add(new Map.GotoPoint
                {
                    X = x,
                    Y = y,
                    Direction = direction,
                    Index = index,
                    Name = gameData.GetGotoPointName(index)
                });
            }

            if (map.EventList.Count != 0)
                map.EventAutomapTypes.AddRange(dataReader.ReadBytes(map.EventList.Count).Select(t => (Enumerations.AutomapType)t));
        }
    }

    public void Write(IDataWriter dataWriter)
    {
        if (map == null)
            throw new AmbermoonException(ExceptionScope.Application, "Map data was null when trying to write it.");

        bool sharedMapData = UseMapDataSharing && potentialSharedDataMapIndices.Contains(map.Index);

        if (sharedMapData)
            map.Flags |= MapFlags.SharedMapData;

        MapWriter.WriteMapHeader(map, dataWriter);

        var usedEvents = new Dictionary<int, uint>();

        if (sharedMapData)
        {
            dataWriter.Write((ushort)1); // For now only map 1 shares the data

            if (map.Type == MapType.Map2D)
            {
                map.Tiles.ForEach((tile, index) =>
                {
                    if (tile.MapEventId != 0)
                        usedEvents.Add(index, tile.MapEventId);
                });
            }
            else // 3D
            {
                map.Blocks.ForEach((block, index) =>
                {
                    if (block.MapEventId != 0)
                        usedEvents.Add(index, block.MapEventId);
                });
            }
        }
        else
        {
            if (map.Type == MapType.Map2D)
            {
                var backTiles = new byte[map.Width * map.Height];
                var frontTiles = new byte[map.Width * map.Height * 2];
                int index = 0;

                unsafe
                {
                    fixed (byte* backFixedPtr = backTiles)
                    fixed (byte* frontFixedPtr = frontTiles)
                    {
                        byte* backPtr = backFixedPtr;
                        ushort* frontPtr = (ushort*)frontFixedPtr;

                        for (int y = 0; y < map.Height; ++y)
                        {
                            for (int x = 0; x < map.Width; ++x)
                            {
                                var tile = map.InitialTiles[x, y];

                                *backPtr++ = (byte)tile.BackTileIndex;
                                *frontPtr++ = (ushort)tile.FrontTileIndex;

                                if (tile.MapEventId != 0)
                                    usedEvents.Add(index, tile.MapEventId);

                                index++;
                            }
                        }
                    }
                }

                dataWriter.Write(backTiles);
                dataWriter.Write(frontTiles);
            }
            else // 3D
            {
                var blocks = new byte[map.Width * map.Height];
                int index = 0;

                unsafe
                {
                    fixed (byte* blockFixedPtr = blocks)
                    {
                        byte* blockPtr = blockFixedPtr;

                        for (int y = 0; y < map.Height; ++y)
                        {
                            for (int x = 0; x < map.Width; ++x)
                            {
                                var block = map.InitialBlocks[x, y];

                                *blockPtr++ = (byte)
                                (
                                    block.MapBorder
                                        ? 0xff
                                        : block.WallIndex != 0
                                            ? 100 + block.WallIndex
                                            : block.ObjectIndex
                                );

                                if (block.MapEventId != 0)
                                    usedEvents.Add(index, block.MapEventId);

                                index++;
                            }
                        }
                    }
                }

                dataWriter.Write(blocks);
            }
        }

        EventWriter.WriteEvents(dataWriter, map.Events, map.EventList);

        dataWriter.Write((ushort)usedEvents.Count);

        foreach (var (tileIndex, mapEventId) in usedEvents)
        {
            dataWriter.Write((ushort)tileIndex);
            dataWriter.Write((byte)mapEventId);
        }

        var mapCharacters = map.CharacterReferences.Where(cr => cr != null).ToArray();

        dataWriter.Write((byte)mapCharacters.Length);

        foreach (var character in mapCharacters)
        {
            dataWriter.Write((byte)character.Index);
            dataWriter.Write((byte)character.CollisionClass);
            dataWriter.Write((byte)(((byte)character.Type & 0x03) | ((byte)character.CharacterFlags << 2)));
            dataWriter.Write((byte)character.EventIndex);
            dataWriter.Write((ushort)character.GraphicIndex);
            dataWriter.Write((uint)character.TileFlags | (uint)((character.CombatBackgroundIndex & 0xf) << 28));

            character.Positions.ForEach(p =>
            {
                dataWriter.Write((byte)p.X);
                dataWriter.Write((byte)p.Y);
            });

            if (map.Type == MapType.Map3D)
            {
                dataWriter.Write((byte)map.GotoPoints.Count);

                foreach (var gotoPoint in map.GotoPoints)
                {
                    dataWriter.Write((byte)gotoPoint.X);
                    dataWriter.Write((byte)gotoPoint.Y);
                    dataWriter.WriteEnum8(gotoPoint.Direction);
                    dataWriter.Write((byte)gotoPoint.Index);
                }

                if (map.EventList.Count != 0)
                    map.EventAutomapTypes.ForEach(t => dataWriter.Write((byte)t));
            }
        }
    }
}
