using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data
{
    [Flags]
    public enum MapFlags
    {
        None = 0,
        Indoor = 1 << 0, // Always at full light.
        Outdoor = 1 << 1, // Light level is given by the daytime.
        Dungeon = 1 << 2, // Only own light sources will grant light.
        Automapper = 1 << 3, // If set the map is available and the map has to be explored. It also allows map-related spells. All Morag temples omit this.
        CanRest = 1 << 4,
        Unknown1 = 1 << 5, // Unknown. All world maps use that in Ambermoon.
        Sky = 1 << 6, // All towns have this and the ruin tower. Only considered for 3D maps.
        NoSleepUntilDawn = 1 << 7, // If active sleep time is always 8 hours.
        StationaryGraphics = 1 << 8, // Allow stationary graphics (travel type images) and therefore transports. Is set for all world maps. This also controls if the music is taken from the map file or dependent on the travel type.
        Unknown2 = 1 << 9, // Unknown. Never used in Ambermoon.
        WorldSurface = 1 << 10, // If set the map doesn't use map text 0 as the title but uses the world name instead. Moreover based on world adjacent maps are shown with a size of 50x50.
        CanUseMagic = 1 << 11, // Only 0 in map 269 which is the house of the baron of Spannenberg (also in map 148 but this is a bug). It just disables the spell book if not set but you still can use scrolls or items.
        NoTravelMusic = 1 << 12, // Won't use travel music if StationaryGraphics is set
        NoMarkOrReturn = 1 << 13, // Forbids the use of "Word of marking" and "Word of returning"
        SmallPlayer = StationaryGraphics // Display player smaller. Only all world maps have this set. Only considered for 2D maps.
    }

    public class Map
    {
        public enum TileType
        {
            Normal,
            ChairUp,
            ChairRight,
            ChairDown,
            ChairLeft,
            Bed,
            Invisible,
            Water
        }

        public class Tile
        {
            /// <summary>
            /// Back layer in 2D maps
            /// </summary>
            public uint BackTileIndex { get; set; }
            /// <summary>
            /// Front layer in 2D maps
            /// </summary>
            public uint FrontTileIndex { get; set; }
            public uint MapEventId { get; set; }
            public TileType Type { get; set; }
            public bool AllowMovement(Tileset tileset, TravelType travelType, bool isPlayer = true, bool allowSwimWalkChange = false)
            {
                if (!isPlayer && Type == TileType.Water)
                {
                    return false;
                }

                if (travelType != TravelType.Swim && (Type > TileType.Normal && Type < TileType.Invisible))
                {
                    return true;
                }

                if (tileset.AllowMovement(BackTileIndex, FrontTileIndex, travelType))
                    return true;

                if (allowSwimWalkChange)
                {
                    if (travelType == TravelType.Swim && tileset.AllowMovement(BackTileIndex, FrontTileIndex, TravelType.Walk))
                        return true;

                    if (travelType == TravelType.Walk && tileset.AllowMovement(BackTileIndex, FrontTileIndex, TravelType.Swim))
                        return true;
                }

                return false;
            }
            public bool BlocksSight(Tileset tileset)
            {
                // TODO: Is there a tile flag for that?

                if (Type == TileType.Invisible || !AllowMovement(tileset, TravelType.Walk))
                    return true;

                return false;
            }

            public Tile Clone()
            {
                return new Tile
                {
                    BackTileIndex = BackTileIndex,
                    FrontTileIndex = FrontTileIndex,
                    MapEventId = MapEventId,
                    Type = Type
                };
            }
        }

        public class Block
        {
            public uint ObjectIndex { get; set; }
            public uint WallIndex { get; set; }
            public uint MapEventId { get; set; }
            /// <summary>
            /// This block is not drawn at all.
            /// </summary>
            public bool MapBorder { get; set; }

            public bool BlocksPlayer(Labdata labdata, bool wallsOnly = false)
            {
                if (MapBorder)
                    return true;

                if (WallIndex != 0)
                {
                    var wallFlags = labdata.Walls[((int)WallIndex - 1) % labdata.Walls.Count].Flags;
                    return wallFlags.HasFlag(Tileset.TileFlags.BlockAllMovement) || !wallFlags.HasFlag(Tileset.TileFlags.AllowMovementWalk);
                }

                if (!wallsOnly && ObjectIndex != 0)
                {
                    var obj = labdata.Objects[((int)ObjectIndex - 1) % labdata.Objects.Count];

                    foreach (var subObject in obj.SubObjects)
                    {
                        var objectFlags = subObject.Object.Flags;

                        if (objectFlags.HasFlag(Tileset.TileFlags.BlockAllMovement) || !objectFlags.HasFlag(Tileset.TileFlags.AllowMovementWalk))
                            return true;
                    }
                }

                return false;
            }

            public bool BlocksPlayerSight(Labdata labdata)
            {
                if (MapBorder)
                    return true;

                if (WallIndex != 0)
                {
                    var wallFlags = labdata.Walls[((int)WallIndex - 1) % labdata.Walls.Count].Flags;
                    return wallFlags.HasFlag(Tileset.TileFlags.BlockSight);
                }

                return false;
            }

            public Block Clone()
            {
                return new Block
                {
                    ObjectIndex = ObjectIndex,
                    WallIndex = WallIndex,
                    MapEventId = MapEventId,
                    MapBorder = MapBorder
                };
            }
        }

        public class CharacterReference
        {
            [Flags]
            public enum Flags
            {
                None = 0,
                RandomMovement = 0x01,
                UseTileset = 0x02,
                TextPopup = 0x04,
                Stationary = 0x20, // new in Ambermoon Advanced
                MoveOnlyWhenSeePlayer = 0x20 // new in Ambermoon Advanced
            }

            public CharacterType Type { get; set; }
            public Flags CharacterFlags { get; set; }
            public bool OnlyMoveWhenSeePlayer => Type == CharacterType.Monster && CharacterFlags.HasFlag(Flags.MoveOnlyWhenSeePlayer);
            public bool Stationary => Type != CharacterType.Monster && CharacterFlags.HasFlag(Flags.Stationary);
            /// <summary>
            /// Equals travel type.
            /// </summary>
            public int CollisionClass { get; set; }
            public uint Index { get; set; } // of party member, npc, monster or map text
            /// <summary>
            /// Upper 4 bits of this contains the combat background index.
            /// </summary>
            public Tileset.TileFlags TileFlags { get; set; }
            public uint EventIndex { get; set; }
            /// <summary>
            /// This is:
            /// - an object index inside the labdata for 3D maps
            /// - a tile index inside the tileset for 2D maps if flag UseTileset is set
            /// - an NPC graphic index for 2D maps if flag UseTileset is not set and it's an NPC
            /// </summary>
            public uint GraphicIndex { get; set; }
            public uint CombatBackgroundIndex { get; set; }
            public List<Position> Positions { get; } = new List<Position>(288);

            public CharacterReference Clone()
            {
                var clone =  new CharacterReference
                {
                    Type = Type,
                    CharacterFlags = CharacterFlags,
                    CollisionClass = CollisionClass,
                    Index = Index,
                    TileFlags = TileFlags,
                    EventIndex = EventIndex,
                    GraphicIndex = GraphicIndex,
                    CombatBackgroundIndex = CombatBackgroundIndex
                };

                clone.Positions.AddRange(Positions.Select(p => new Position(p)));

                return clone;
            }
        }

        public class GotoPoint
        {
            public uint X { get; set; }
            public uint Y { get; set; }
            public CharacterDirection Direction { get; set; }
            public uint Index { get; set; }
            public string Name { get; set; }

            public GotoPoint Clone()
            {
                return new GotoPoint
                {
                    X = X,
                    Y = Y,
                    Direction = Direction,
                    Index = Index,
                    Name = Name
                };
            }
        }

        public Map Clone()
        {
            var clone = new Map
            {
                Index = Index,
                Flags = Flags,
                Type = Type,
                MusicIndex = MusicIndex,
                Width = Width,
                Height = Height,
                TilesetOrLabdataIndex = TilesetOrLabdataIndex,
                NPCGfxIndex = NPCGfxIndex,
                LabyrinthBackgroundIndex = LabyrinthBackgroundIndex,
                PaletteIndex = PaletteIndex,
                World = World,
                Tiles = Type == MapType.Map2D ? new Tile[Width, Height] : null,
                Blocks = Type == MapType.Map3D ? new Block[Width, Height] : null,
                InitialTiles = Type == MapType.Map2D ? new Tile[Width, Height] : null,
                InitialBlocks = Type == MapType.Map3D ? new Block[Width, Height] : null,
            };

            var events = Events.ToDictionary(e => e, e => e.Clone(false));

            clone.EventList.AddRange(EventList.Select(e => events[e]));
            clone.Events.AddRange(events.Values);
            clone.Texts.AddRange(Texts);

            for (int i = 0; i < clone.CharacterReferences.Length; ++i)
                clone.CharacterReferences[i] = CharacterReferences[i]?.Clone();

            clone.GotoPoints.AddRange(GotoPoints.Select(g => g?.Clone()));
            clone.EventAutomapTypes.AddRange(EventAutomapTypes);

            for (int y = 0; y < Height; ++y)
            {
                for (int x = 0; x < Width; ++x)
                {
                    if (Type == MapType.Map2D)
                    {
                        clone.Tiles[x, y] = Tiles[x, y].Clone();
                        clone.InitialTiles[x, y] = InitialTiles[x, y].Clone();
                    }
                    else
                    {
                        clone.Blocks[x, y] = Blocks[x, y].Clone();
                        clone.InitialBlocks[x, y] = InitialBlocks[x, y].Clone();
                    }
                }
            }

            return clone;
        }

        public uint Index { get; private set; }
        public string Name => IsWorldMap || Index < 256 ? $"{World}{Index:000}" : Texts[0];
        public MapFlags Flags { get; set; }
        public MapType Type { get; set; }
        public uint MusicIndex { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        /// <summary>
        /// Tileset index in 2D
        /// Labdata index in 3D
        /// </summary>
        public uint TilesetOrLabdataIndex { get; set; }
        /// <summary>
        /// This is only used in non-world-surface 2D maps.
        /// To be more precise it could be used in any 2D map
        /// but it only makes sense for maps which have 2D NPCs.
        /// There are 2 NPC graphic files inside the NPC_gfx.amb.
        /// This index specifies which to load (0 = none, 1 or 2).
        /// </summary>
        public uint NPCGfxIndex { get; set; }
        /// <summary>
        /// This is used for outdoor 3D maps (towns). It basically
        /// depends on the world (there is one for each world).
        /// 0: Not used, 1: Lyramion, 2: Forest Moon, 3: Morag
        /// 
        /// TODO: This is also used for all Lyramion and Morag
        /// 2D world maps where it is set to 1. For all other 2D
        /// maps (including forest moon world maps) it is 0.
        /// </summary>
        public uint LabyrinthBackgroundIndex { get; set; }
        public uint PaletteIndex { get; set; }
        public World World { get; set; }
        public Tile[,] Tiles { get; set; }
        public Block[,] Blocks { get; set; }
        public Tile[,] InitialTiles { get; set; }
        public Block[,] InitialBlocks { get; set; }
        public List<Event> Events { get; } = new List<Event>();
        public List<Event> EventList { get; } = new List<Event>();
        public List<string> Texts { get; set; } = new List<string>();
        public CharacterReference[] CharacterReferences { get; } = new CharacterReference[32];
        public List<GotoPoint> GotoPoints { get; set; } = new List<GotoPoint>();
        public List<AutomapType> EventAutomapTypes { get; set; } = new List<AutomapType>();
        public bool IsLyramionWorldMap => IsWorldMap && World == World.Lyramion;
        public bool IsForestMoonWorldMap => IsWorldMap && World == World.ForestMoon;
        public bool IsMoragWorldMap => IsWorldMap && World == World.Morag;
        // Note: We use this to determine that the player is drawn smaller.
        // But actually it should depend on flag SmallPlayer. It is only used
        // for all world maps in Ambermoon so it should be safe.
        public bool IsWorldMap => Flags.HasFlag(MapFlags.WorldSurface);
        public bool UseTravelTypes => Flags.HasFlag(MapFlags.StationaryGraphics);
        public bool UseTravelMusic => UseTravelTypes && !Flags.HasFlag(MapFlags.NoTravelMusic);
        public string GetText(int index, string fallbackText)
        {
            if (Texts == null || index < 0 || index >= Texts.Count)
                return fallbackText;

            return Texts[index];
        }
        public uint MoveWorldMapIndex(uint baseIndex, uint worldMapDimension, uint currentIndex, int changeX, int changeY)
        {
            uint relativeIndex = currentIndex - baseIndex;
            uint row = relativeIndex / worldMapDimension;

            if (changeX != 0)
            {
                uint rowBaseIndex = row * worldMapDimension;
                int indexInRow = ((int)relativeIndex % (int)worldMapDimension) + changeX;

                while (indexInRow < 0)
                    indexInRow += (int)worldMapDimension;
                while (indexInRow >= worldMapDimension)
                    indexInRow -= (int)worldMapDimension;

                relativeIndex = rowBaseIndex + (uint)indexInRow;
            }

            if (changeY != 0)
            {
                int newIndex = (int)relativeIndex + changeY * (int)worldMapDimension;
                int totalMaps = (int)worldMapDimension * (int)worldMapDimension;

                while (newIndex < 0)
                    newIndex += totalMaps;
                while (newIndex >= totalMaps)
                    newIndex -= totalMaps;

                relativeIndex = (uint)newIndex;
            }

            return baseIndex + relativeIndex;
        }
        public uint BaseWorldMapIndex
        {
            get
            {
                if (IsLyramionWorldMap)
                    return 1u;

                if (IsForestMoonWorldMap)
                    return 300u;

                if (IsMoragWorldMap)
                    return 513u;

                return 0u;
            }
        }
        public uint WorldMapDimension
        {
            get
            {
                if (IsLyramionWorldMap)
                    return 16u;

                if (IsForestMoonWorldMap)
                    return 6u;

                if (IsMoragWorldMap)
                    return 4u;

                return 0u;
            }
        }
        public uint? LeftMapIndex => !IsWorldMap ? (uint?)null : MoveWorldMapIndex(BaseWorldMapIndex, WorldMapDimension, Index, -1, 0);
        public uint? RightMapIndex => !IsWorldMap ? (uint?)null : MoveWorldMapIndex(BaseWorldMapIndex, WorldMapDimension, Index, 1, 0);
        public uint? UpMapIndex => !IsWorldMap ? (uint?)null : MoveWorldMapIndex(BaseWorldMapIndex, WorldMapDimension, Index, 0, -1);
        public uint? UpLeftMapIndex => !IsWorldMap ? (uint?)null : MoveWorldMapIndex(BaseWorldMapIndex, WorldMapDimension, Index, -1, -1);
        public uint? UpRightMapIndex => !IsWorldMap ? (uint?)null : MoveWorldMapIndex(BaseWorldMapIndex, WorldMapDimension, Index, 1, -1);
        public uint? DownMapIndex => !IsWorldMap ? (uint?)null : MoveWorldMapIndex(BaseWorldMapIndex, WorldMapDimension, Index, 0, 1);
        public uint? DownLeftMapIndex => !IsWorldMap ? (uint?)null : MoveWorldMapIndex(BaseWorldMapIndex, WorldMapDimension, Index, -1, 1);
        public uint? DownRightMapIndex => !IsWorldMap ? (uint?)null : MoveWorldMapIndex(BaseWorldMapIndex, WorldMapDimension, Index, 1, 1);
        public Position MapOffset
        {
            get
            {
                if (!IsWorldMap)
                    return new Position(0, 0);

                var relativeIndex = Index - BaseWorldMapIndex;
                var dimension = WorldMapDimension;

                int x = (int)(relativeIndex % dimension) * Width; // world maps should all have the same width
                int y = (int)(relativeIndex / dimension) * Height; // world maps should all have the same height

                return new Position(x, y);
            }
        }
        public uint TicksPerAnimationFrame { get; set; } = 10; // This matches the frame speed in real game quiet good. TODO: changeable later? same for every map?

        public Map()
        {

        }

        public static Map Load(uint index, IMapReader mapReader, IDataReader dataReader, IDataReader textDataReader,
            Dictionary<uint, Tileset> tilesets)
        {
            var map = new Map { Index = index };

            mapReader.ReadMap(map, dataReader, tilesets);
            mapReader.ReadMapTexts(map, textDataReader);

            return map;
        }

        public static Map LoadWithoutTexts(uint index, IMapReader mapReader, IDataReader dataReader, Dictionary<uint, Tileset> tilesets)
        {
            var map = new Map { Index = index };

            mapReader.ReadMap(map, dataReader, tilesets);

            return map;
        }

        public void Reset()
        {
            if (InitialTiles != null)
            {
                Tiles = new Tile[Width, Height];

                for (int y = 0; y < Height; ++y)
                {
                    for (int x = 0; x < Width; ++x)
                    {
                        var initialTile = InitialTiles[x, y];
                        Tiles[x, y] = new Tile
                        {
                            BackTileIndex = initialTile.BackTileIndex,
                            FrontTileIndex = initialTile.FrontTileIndex,
                            MapEventId = initialTile.MapEventId,
                            Type = initialTile.Type
                        };
                    }
                }
            }
            else if (InitialBlocks != null)
            {
                Blocks = new Block[Width, Height];

                for (int y = 0; y < Height; ++y)
                {
                    for (int x = 0; x < Width; ++x)
                    {
                        var initialBlock = InitialBlocks[x, y];
                        Blocks[x, y] = new Block
                        {
                            MapBorder = initialBlock.MapBorder,
                            ObjectIndex = initialBlock.ObjectIndex,
                            WallIndex = initialBlock.WallIndex,
                            MapEventId = initialBlock.MapEventId
                        };
                    }
                }
            }
        }

        public static TileType TileTypeFromTile(Tile tile, Tileset tileset)
        {
            var tilesetTile = tile.FrontTileIndex == 0 ? tileset.Tiles[tile.BackTileIndex - 1] : tileset.Tiles[tile.FrontTileIndex - 1];

            if (tilesetTile.Sleep)
                return TileType.Bed;
            if (tilesetTile.SitDirection != null)
                return TileType.ChairUp + (int)tilesetTile.SitDirection.Value;
            if (tilesetTile.CharacterInvisible)
                return TileType.Invisible;
            if (tile.AllowMovement(tileset, TravelType.Swim))
                return TileType.Water;
            return TileType.Normal;
        }

        public void UpdateTile(uint x, uint y, uint newFrontTileIndex, Tileset tileset)
        {
            if (Type != MapType.Map2D)
                throw new AmbermoonException(ExceptionScope.Data, "Tiles can only be updated for 2D maps.");

            if (newFrontTileIndex == 0)
                Tiles[x, y].FrontTileIndex = 0;
            else
            {
                if (tileset.Tiles[newFrontTileIndex - 1].Flags.HasFlag(Tileset.TileFlags.Background))
                    Tiles[x, y].FrontTileIndex = newFrontTileIndex;
                else
                    Tiles[x, y].BackTileIndex = newFrontTileIndex;
            }

            Tiles[x, y].Type = TileTypeFromTile(Tiles[x, y], tileset);
        }

        public bool StopMovingTowards(Savegame savegame, int x, int y)
        {
            uint mapEventId = (Type == MapType.Map2D) ? Tiles[x, y].MapEventId : Blocks[x, y].MapEventId;

            if (mapEventId == 0 || !savegame.IsEventActive(Index, mapEventId - 1))
                return false;

            var @event = EventList[(int)mapEventId - 1];

            return @event.Type switch
            {
                EventType.Chest => true,
                EventType.Door => savegame.IsDoorLocked((@event as DoorEvent).DoorIndex), // Only locked doors block
                EventType.EnterPlace => true,
                EventType.Riddlemouth => true,
                _ => false
            };
        }

        public bool CanCamp => Flags.HasFlag(MapFlags.CanRest);
        public bool CanUseSpells => Flags.HasFlag(MapFlags.CanUseMagic);
    }
}
