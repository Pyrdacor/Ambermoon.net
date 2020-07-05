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
            public uint BackTileIndex { get; set; }
            public uint FrontTileIndex { get; set; }
            public uint MapEventId { get; set; }
            public uint Unknown { get; set; }
            public TileType Type { get; set; }
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
        public List<List<MapEvent>> Events { get; } = new List<List<MapEvent>>();
        public bool IsLyramionWorldMap => IsWorldMap && World == World.Lyramion;
        public bool IsForestMoonWorldMap => IsWorldMap && World == World.ForestMoon;
        public bool IsMoragWorldMap => IsWorldMap && World == World.Morag;
        public bool IsWorldMap => Flags.HasFlag(MapFlags.WorldSurface);
        public uint MoveWorldMapIndex(uint baseIndex, uint mapsPerRow, uint numMapRows, uint currentIndex, int changeX, int changeY)
        {
            uint relativeIndex = currentIndex - baseIndex;
            uint row = relativeIndex / mapsPerRow;

            if (changeX != 0)
            {
                uint rowBaseIndex = row * mapsPerRow;
                int indexInRow = ((int)relativeIndex % (int)mapsPerRow) + changeX;

                while (indexInRow < 0)
                    indexInRow += (int)mapsPerRow;
                while (indexInRow >= mapsPerRow)
                    indexInRow -= (int)mapsPerRow;

                relativeIndex = rowBaseIndex + (uint)indexInRow;
            }

            if (changeY != 0)
            {
                int newIndex = (int)relativeIndex + changeY * (int)mapsPerRow;
                int totalMaps = (int)numMapRows * (int)mapsPerRow;

                while (newIndex < 0)
                    newIndex += totalMaps;
                while (newIndex >= totalMaps)
                    newIndex -= totalMaps;

                relativeIndex = (uint)newIndex;
            }

            return baseIndex + relativeIndex;
        }
        public uint? LeftMapIndex
        {
            get
            {
                if (IsLyramionWorldMap)
                    return MoveWorldMapIndex(1u, 16u, 16u, Index, -1, 0);

                if (IsForestMoonWorldMap)
                    return MoveWorldMapIndex(300u, 6u, 6u, Index, -1, 0);

                if (IsMoragWorldMap)
                    return MoveWorldMapIndex(513u, 4u, 4u, Index, -1, 0);

                return null;
            }
        }
        public uint? RightMapIndex
        {
            get
            {
                if (IsLyramionWorldMap)
                    return MoveWorldMapIndex(1u, 16u, 16u, Index, 1, 0);

                if (IsForestMoonWorldMap)
                    return MoveWorldMapIndex(300u, 6u, 6u, Index, 1, 0);

                if (IsMoragWorldMap)
                    return MoveWorldMapIndex(513u, 4u, 4u, Index, 1, 0);

                return null;
            }
        }
        public uint? UpMapIndex
        {
            get
            {
                if (IsLyramionWorldMap)
                    return MoveWorldMapIndex(1u, 16u, 16u, Index, 0, -1);

                if (IsForestMoonWorldMap)
                    return MoveWorldMapIndex(300u, 6u, 6u, Index, 0, -1);

                if (IsMoragWorldMap)
                    return MoveWorldMapIndex(513u, 4u, 4u, Index, 0, -1);

                return null;
            }
        }
        public uint? UpLeftMapIndex
        {
            get
            {
                if (IsLyramionWorldMap)
                    return MoveWorldMapIndex(1u, 16u, 16u, Index, -1, -1);

                if (IsForestMoonWorldMap)
                    return MoveWorldMapIndex(300u, 6u, 6u, Index, -1, -1);

                if (IsMoragWorldMap)
                    return MoveWorldMapIndex(513u, 4u, 4u, Index, -1, -1);

                return null;
            }
        }
        public uint? UpRightMapIndex
        {
            get
            {
                if (IsLyramionWorldMap)
                    return MoveWorldMapIndex(1u, 16u, 16u, Index, 1, -1);

                if (IsForestMoonWorldMap)
                    return MoveWorldMapIndex(300u, 6u, 6u, Index, 1, -1);

                if (IsMoragWorldMap)
                    return MoveWorldMapIndex(513u, 4u, 4u, Index, 1, -1);

                return null;
            }
        }
        public uint? DownMapIndex
        {
            get
            {
                if (IsLyramionWorldMap)
                    return MoveWorldMapIndex(1u, 16u, 16u, Index, 0, 1);

                if (IsForestMoonWorldMap)
                    return MoveWorldMapIndex(300u, 6u, 6u, Index, 0, 1);

                if (IsMoragWorldMap)
                    return MoveWorldMapIndex(513u, 4u, 4u, Index, 0, 1);

                return null;
            }
        }
        public uint? DownLeftMapIndex
        {
            get
            {
                if (IsLyramionWorldMap)
                    return MoveWorldMapIndex(1u, 16u, 16u, Index, -1, 1);

                if (IsForestMoonWorldMap)
                    return MoveWorldMapIndex(300u, 6u, 6u, Index, -1, 1);

                if (IsMoragWorldMap)
                    return MoveWorldMapIndex(513u, 4u, 4u, Index, -1, 1);

                return null;
            }
        }
        public uint? DownRightMapIndex
        {
            get
            {
                if (IsLyramionWorldMap)
                    return MoveWorldMapIndex(1u, 16u, 16u, Index, 1, 1);

                if (IsForestMoonWorldMap)
                    return MoveWorldMapIndex(300u, 6u, 6u, Index, 1, 1);

                if (IsMoragWorldMap)
                    return MoveWorldMapIndex(513u, 4u, 4u, Index, 1, 1);

                return null;
            }
        }
        public uint TicksPerAnimationFrame { get; set; } = 10; // This matches the frame speed in real game quiet good. TODO: changeable later? same for every map?

        private Map()
        {

        }

        void ExecuteEvents(IRenderPlayer player, uint x, uint y, IMapManager mapManager, uint ticks, List<MapEvent> mapEvents)
        {
            foreach (var mapEvent in mapEvents)
            {
                if (mapEvent.Type == MapEventType.MapChange)
                {
                    // TODO: conditions?
                    if (mapEvent is MapChangeEvent mapChangeEvent)
                    {
                        // The position (x, y) is 1-based in the data so we subtract 1.
                        // Morover the players position is 1 tile below its drawing position so subtract another 1 from y.
                        player.MoveTo(mapManager.GetMap(mapChangeEvent.MapIndex), mapChangeEvent.X - 1, mapChangeEvent.Y - 2, ticks, true, true);
                    }
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
                    ExecuteEvents(player, x, y, mapManager, ticks, mapEvents);
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

        public static Map Load(uint index, IMapReader mapReader, IDataReader dataReader)
        {
            var map = new Map { Index = index };

            mapReader.ReadMap(map, dataReader);

            return map;
        }
    }
}
