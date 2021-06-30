using Ambermoon.Data;
using System;

namespace Ambermoon
{
    public enum EventTrigger
    {
        Always,
        Move,
        Hand,
        Eye,
        Mouth,
        Levitating,
        /// <summary>
        /// If a specific item is needed for triggering use Item0 + ItemIndex
        /// </summary>
        Item0
    }

    internal static class MapExtensions
    {
        static uint? LastMapEventIndexMap = null;
        static uint? LastMapEventIndex = null;

        public static void Reset()
        {
            LastMapEventIndexMap = null;
            LastMapEventIndex = null;
        }

        public static uint PositionToTileIndex(this Map map, uint x, uint y) => x + y * (uint)map.Width;

        public static bool TriggerEvents(this Map map, Game game, EventTrigger trigger,
            uint x, uint y, uint ticks, Savegame savegame)
        {
            return TriggerEvents(map, game, trigger, x, y, ticks, savegame, out _);
        }

        public static Event GetEvent(this Map map, uint x, uint y, Savegame savegame)
        {
            var mapEventId = map.Type == MapType.Map2D ? map.Tiles[x, y].MapEventId : map.Blocks[x, y].MapEventId;
            bool hasMapEvent = mapEventId != 0 && savegame.IsEventActive(map.Index, mapEventId - 1);

            if (!hasMapEvent)
                return null;

            return map.EventList[(int)mapEventId - 1];
        }

        public static bool TriggerEvents(this Map map, Game game, EventTrigger trigger,
            uint x, uint y, uint ticks, Savegame savegame, out bool hasMapEvent, Func<Event, bool> filter = null)
        {
            var mapEventId = map.Type == MapType.Map2D ? map.Tiles[x, y].MapEventId : map.Blocks[x, y].MapEventId;

            hasMapEvent = mapEventId != 0 && savegame.IsEventActive(map.Index, mapEventId - 1);

            if (!hasMapEvent)
                return false;

            var @event = map.EventList[(int)mapEventId - 1];

            if (filter != null && !filter(@event))
                return false;

            if (trigger == EventTrigger.Move && LastMapEventIndexMap == map.Index && LastMapEventIndex == mapEventId)
            {
                var ev = @event;

                while (ev?.Type == EventType.Condition ||
                       ev?.Type == EventType.Dice100Roll)
                    ev = ev.Next;

                // avoid triggering the same event twice, but only for some events
                if (ev != null &&
                    ev.Type != EventType.Teleport &&
                    ev.Type != EventType.Chest &&
                    ev.Type != EventType.Door &&
                    ev.Type != EventType.EnterPlace &&
                    ev.Type != EventType.RemoveBuffs &&
                    ev.Type != EventType.Riddlemouth &&
                    (map.Type == MapType.Map3D || ev.Type != EventType.Trap))
                {
                    return true;
                }
            }

            LastMapEventIndexMap = map.Index;
            LastMapEventIndex = mapEventId;

            return map.TriggerEventChain(game, trigger, x, y, ticks, @event);
        }

        public static void ClearLastEvent(this Map map)
        {
            LastMapEventIndexMap = map.Index;
            LastMapEventIndex = 0;
        }
    }
}
