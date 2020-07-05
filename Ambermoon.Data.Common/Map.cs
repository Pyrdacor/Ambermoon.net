using Ambermoon.Render;
using System.Collections.Generic;

namespace Ambermoon.Data
{
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
        public MapType Type { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public uint TilesetIndex { get; set; }
        public Tile[,] Tiles { get; set; }
        public List<List<MapEvent>> Events { get; } = new List<List<MapEvent>>();
        public bool IsLyramionMap => Index >= 1 && Index <= 256;
        public bool IsForestMoonMap => Index >= 300 && Index <= 335;
        public bool IsMoragMap => Index >= 513 && Index <= 528;
        public bool IsWorldMap => IsLyramionMap || IsForestMoonMap || IsMoragMap;
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
                int newIndex = (int)relativeIndex + changeY;
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
                if (IsLyramionMap)
                    return MoveWorldMapIndex(1u, 16u, 16u, Index, -1, 0);

                if (IsForestMoonMap)
                    return MoveWorldMapIndex(300u, 6u, 6u, Index, -1, 0);

                if (IsMoragMap)
                    return MoveWorldMapIndex(513u, 4u, 4u, Index, -1, 0);

                return null;
            }
        }
        public uint? RightMapIndex
        {
            get
            {
                if (IsLyramionMap)
                    return MoveWorldMapIndex(1u, 16u, 16u, Index, 1, 0);

                if (IsForestMoonMap)
                    return MoveWorldMapIndex(300u, 6u, 6u, Index, 1, 0);

                if (IsMoragMap)
                    return MoveWorldMapIndex(513u, 4u, 4u, Index, 1, 0);

                return null;
            }
        }
        public uint? UpMapIndex
        {
            get
            {
                if (IsLyramionMap)
                    return MoveWorldMapIndex(1u, 16u, 16u, Index, 0, -1);

                if (IsForestMoonMap)
                    return MoveWorldMapIndex(300u, 6u, 6u, Index, 0, -1);

                if (IsMoragMap)
                    return MoveWorldMapIndex(513u, 4u, 4u, Index, 0, -1);

                return null;
            }
        }
        public uint? DownMapIndex
        {
            get
            {
                if (IsLyramionMap)
                    return MoveWorldMapIndex(1u, 16u, 16u, Index, 0, 1);

                if (IsForestMoonMap)
                    return MoveWorldMapIndex(300u, 6u, 6u, Index, 0, 1);

                if (IsMoragMap)
                    return MoveWorldMapIndex(513u, 4u, 4u, Index, 0, 1);

                return null;
            }
        }
        public uint? DownRightMapIndex
        {
            get
            {
                if (IsLyramionMap)
                    return MoveWorldMapIndex(1u, 16u, 16u, Index, 1, 1);

                if (IsForestMoonMap)
                    return MoveWorldMapIndex(300u, 6u, 6u, Index, 1, 1);

                if (IsMoragMap)
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
                        player.MoveTo(mapManager.GetMap(mapChangeEvent.MapIndex), mapChangeEvent.X - 1, mapChangeEvent.Y - 1, ticks, true);
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
