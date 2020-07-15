using Ambermoon.Render;
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
            Free,
            Chair,
            Bed,
            Obstacle, // can be passed by witch broom and eagle (this is also used for obstacles in water!)
            Water, // can swim in it, flying disc can fly over
            Ocean, // can not swim in it, flying disc can not fly over
            Mountain, // only pass with eagle
            Unpassable // even by eagle
            // TODO: is this enough?
        }

        public class Tile
        {
            /// <summary>
            /// Back layer in 2D maps, base texture in 3D maps
            /// </summary>
            public uint BackTileIndex { get; set; }
            /// <summary>
            /// Front layer in 2D maps, overlay texture in 3D maps
            /// </summary>
            public uint FrontTileIndex { get; set; }
            public uint MapEventId { get; set; }
            public uint Unused { get; set; } // always 0
            public TileType Type { get; set; }
        }

        public class CharacterReference
        {
            public int Type { get; set; } // 0 = None, 4 = party member, 5 = npc, 6 = monster
            public byte Unknown1 { get; set; }
            public uint Index { get; set; } // of party member, npc and monster
            public byte[] Unknown2 { get; set; }
        }

        public uint Index { get; private set; }
        public MapFlags Flags { get; set; }
        public MapType Type { get; set; }
        public uint MusicIndex { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public uint TilesetIndex { get; set; }
        public uint NPCGfxIndex { get; set; }
        public uint LabyrinthBackIndex { get; set; }
        public uint PaletteIndex { get; set; }
        public World World { get; set; }
        public Tile[,] Tiles { get; set; }
        public List<MapEvent> Events { get; } = new List<MapEvent>();
        public List<MapEvent> EventLists { get; } = new List<MapEvent>();
        public List<string> Texts { get; } = new List<string>();
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

        void ExecuteEvent(IRenderPlayer player, uint x, uint y, IMapManager mapManager, uint ticks, MapEvent mapEvent)
        {
            if (mapEvent.Type == MapEventType.MapChange)
            {
                // TODO: conditions?
                if (mapEvent is MapChangeEvent mapChangeEvent)
                {
                    // The position (x, y) is 1-based in the data so we subtract 1.
                    // Morover the players position is 1 tile below its drawing position so subtract another 1 from y.
                    player.MoveTo(mapManager.GetMap(mapChangeEvent.MapIndex), mapChangeEvent.X - 1, mapChangeEvent.Y - 2, ticks, true, mapChangeEvent.Direction);
                }
            }
        }

        public void TriggerEvents(IRenderPlayer player, MapEventTrigger trigger, uint x, uint y, IMapManager mapManager, uint ticks)
        {
            var mapEventId = Tiles[x, y].MapEventId;

            if (mapEventId == 0)
                return; // no map events at this position

            var mapEvents = Events[(int)mapEventId - 1];

            switch (trigger)
            {
                case MapEventTrigger.Move:
                    ExecuteEvent(player, x, y, mapManager, ticks, mapEvents);
                    // TODO
                    break;
                case MapEventTrigger.Hand:
                    // TODO
                    break;
                case MapEventTrigger.Eye:
                    // TODO
                    break;
                case MapEventTrigger.Mouth:
                    // TODO
                    break;
            }
        }

        public static Map Load(uint index, IMapReader mapReader, IDataReader dataReader, IDataReader textDataReader)
        {
            var map = new Map { Index = index };

            mapReader.ReadMap(map, dataReader, textDataReader);

            return map;
        }
    }
}
