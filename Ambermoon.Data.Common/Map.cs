using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Serialization;
using System;
using System.Collections.Generic;

namespace Ambermoon.Data
{
    [Flags]
    public enum MapFlags
    {
        None = 0,
        Indoor = 1 << 0,
        Outdoor = 1 << 1,
        Dungeon = 1 << 2,
        Automapper = 1 << 3, // If active the map has to be explored
        Unknown1 = 1 << 4,
        WorldSurface = 1 << 5,
        SecondaryUI3D = 1 << 6,
        NoSleepUntilDawn = 1 << 7, // If active sleep time is always 8 hours
        StationaryGraphics = 1 << 8,
        Unknown2 = 1 << 9,
        SecondaryUI2D = 1 << 10,
        Unknown3 = 1 << 11 // only 0 in map 269 which is the house of the baron of Spannenberg (also in map 148 but this is a bug)
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
            public uint Unused { get; set; } // always 0
            public TileType Type { get; set; }
            public bool AllowMovement(Tileset tileset, TravelType travelType, bool isPlayer = true)
            {
                if (!isPlayer && Type == TileType.Water)
                {
                    return false;
                }

                if (Type != TileType.Normal)
                {
                    return true;
                }

                bool allowBack = BackTileIndex == 0 || tileset.Tiles[BackTileIndex - 1].AllowMovement(travelType);
                bool allowFront = FrontTileIndex == 0 || tileset.Tiles[FrontTileIndex - 1].AllowMovement(travelType);

                if (FrontTileIndex != 0 && !tileset.Tiles[FrontTileIndex - 1].BringToFront)
                    allowBack = allowFront;

                return allowBack && allowFront;
            }
            public bool BlocksSight(Tileset tileset)
            {
                // TODO: Is there a tile flag for that?

                if (Type == TileType.Invisible || !AllowMovement(tileset, TravelType.Walk))
                    return true;

                return false;
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
        }

        public class CharacterReference
        {
            [Flags]
            public enum Flags
            {
                None = 0,
                RandomMovement = 0x01,
                UseTileset = 0x02,
                TextPopup = 0x04
            }

            public CharacterType Type { get; set; }
            public Flags CharacterFlags { get; set; }
            public bool OnlyMoveWhenSeePlayer { get; set; }
            public uint Index { get; set; } // of party member, npc, monster or map text
            /// <summary>
            /// Upper 4 bits of this contains the combat background index.
            /// </summary>
            public byte[] Unknown2 { get; set; }
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
        }

        public uint Index { get; private set; }
        public string Name => IsWorldMap ? $"{World}{Index:000}" : Texts[0]; // TODO: use correct language later?
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
        /// To more precise it could be used in any 2D map
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
        public List<Event> Events { get; } = new List<Event>();
        public List<Event> EventList { get; } = new List<Event>();
        public List<string> Texts { get; set; } = new List<string>();
        public CharacterReference[] CharacterReferences { get; } = new CharacterReference[32];
        public bool IsLyramionWorldMap => IsWorldMap && World == World.Lyramion;
        public bool IsForestMoonWorldMap => IsWorldMap && World == World.ForestMoon;
        public bool IsMoragWorldMap => IsWorldMap && World == World.Morag;
        public bool IsWorldMap => Flags.HasFlag(MapFlags.WorldSurface);
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

        private Map()
        {

        }

        public static Map Load(uint index, IMapReader mapReader, IDataReader dataReader, IDataReader textDataReader,
            Dictionary<uint, Tileset> tilesets)
        {
            var map = new Map { Index = index };

            mapReader.ReadMap(map, dataReader, textDataReader, tilesets);

            return map;
        }

        public static TileType TileTypeFromTile(Map.Tile tile, Tileset tileset)
        {
            var tilesetTile = tile.FrontTileIndex == 0 ? tileset.Tiles[tile.BackTileIndex - 1] : tileset.Tiles[tile.FrontTileIndex - 1];

            if (tilesetTile.Sleep)
                return TileType.Bed;
            if (tilesetTile.SitDirection != null)
                return TileType.ChairUp + (int)tilesetTile.SitDirection.Value;
            if (tilesetTile.Invisible)
                return TileType.Invisible;
            if (tile.AllowMovement(tileset, TravelType.Swim))
                return TileType.Water;
            return TileType.Normal;
        }

        public void UpdateTile(uint x, uint y, uint newBackTileIndex, uint newFrontTileIndex, Tileset tileset)
        {
            if (Type != MapType.Map2D)
                throw new AmbermoonException(ExceptionScope.Data, "Tiles can only be updated for 2D maps.");

            if (newBackTileIndex != 0)
            {
                Tiles[x, y].BackTileIndex = newBackTileIndex;
                Tiles[x, y].FrontTileIndex = newFrontTileIndex;
            }
            else if (newFrontTileIndex != 0 && Tiles[x, y].FrontTileIndex != 0 &&
                tileset.Tiles[Tiles[x, y].FrontTileIndex - 1].Background &&
                !tileset.Tiles[newFrontTileIndex - 1].Background)
            {
                Tiles[x, y].BackTileIndex = newFrontTileIndex;
                Tiles[x, y].FrontTileIndex = 0;
            }
            else
            {
                Tiles[x, y].FrontTileIndex = newFrontTileIndex;
            }
            Tiles[x, y].Type = TileTypeFromTile(Tiles[x, y], tileset);
        }
    }
}
