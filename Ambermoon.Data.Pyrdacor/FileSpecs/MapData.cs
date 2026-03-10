using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

internal class MapData : IFileSpec<MapData>, IFileSpec
{
    public static string Magic => "MAP";
    public static byte SupportedVersion => 0;
    public static ushort PreferredCompression => ICompression.GetIdentifier<Deflate>();
    Map? map = null;
    ushort? templateMapIndex = null;
    bool resolved = false;
    Dictionary<ushort, byte> mapEventOnTileIndex = [];

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

        MapWriter.WriteMapHeader(map, dataWriter);

        // TODO

        throw new NotImplementedException();
    }
}
