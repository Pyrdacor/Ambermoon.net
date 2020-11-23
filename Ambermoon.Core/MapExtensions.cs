using Ambermoon.Data;
using Ambermoon.Render;

namespace Ambermoon
{
    public enum EventTrigger
    {
        Always,
        Move,
        Hand,
        Eye,
        Mouth,
        /// <summary>
        /// If a specific item is needed for triggering use Item0 + ItemIndex
        /// </summary>
        Item0
    }

    internal static class MapExtensions
    {
        static uint? LastMapEventIndexMap = null; // TODO: do it better
        static uint? LastMapEventIndex = null; // TODO: do it better

        public static uint PositionToTileIndex(this Map map, uint x, uint y) => x + y * (uint)map.Width;

        public static bool TriggerEvents(this Map map, Game game, EventTrigger trigger,
            uint x, uint y, uint ticks, Savegame savegame)
        {
            return TriggerEvents(map, game, trigger, x, y, ticks, savegame,
                out bool _, false);
        }

        public static Event GetEvent(this Map map, uint x, uint y, Savegame savegame)
        {
            var mapEventId = map.Type == MapType.Map2D ? map.Tiles[x, y].MapEventId : map.Blocks[x, y].MapEventId;
            bool hasMapEvent = mapEventId != 0 && !savegame.GetEventBit(map.Index, mapEventId - 1);

            if (!hasMapEvent)
                return null;

            return map.EventList[(int)mapEventId - 1];
        }

        public static bool TriggerEvents(this Map map, Game game, EventTrigger trigger,
            uint x, uint y, uint ticks, Savegame savegame,
            out bool hasMapEvent, bool noIndexReset = false)
        {
            var mapEventId = map.Type == MapType.Map2D ? map.Tiles[x, y].MapEventId : map.Blocks[x, y].MapEventId;

            hasMapEvent = mapEventId != 0 && !savegame.GetEventBit(map.Index, mapEventId - 1);

            if (!hasMapEvent)
                return false;

            if (trigger == EventTrigger.Move && LastMapEventIndexMap == map.Index && LastMapEventIndex == mapEventId)
                return false;

            if (!noIndexReset || mapEventId != 0)
            {
                LastMapEventIndexMap = map.Index;
                LastMapEventIndex = mapEventId;
            }

            if (mapEventId == 0)
                return false; // no map events at this position

            return map.TriggerEventChain(game, trigger, x, y, ticks, map.EventList[(int)mapEventId - 1]);
        }

        public static void ClearLastEvent(this Map map)
        {
            LastMapEventIndexMap = map.Index;
            LastMapEventIndex = 0;
        }
    }
}
