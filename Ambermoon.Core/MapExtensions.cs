using Ambermoon.Data;
using Ambermoon.Render;

namespace Ambermoon
{
    public enum MapEventTrigger
    {
        Move,
        Hand,
        Eye,
        Mouth,
        /// <summary>
        /// If a specific item is needed for triggering use Item0 + ItemIndex
        /// </summary>
        Item0
    }

    public static class MapExtensions
    {
        public static uint PositionToTileIndex(this Map map, uint x, uint y) => x + y * (uint)map.Width;

        static MapEvent ExecuteEvent(Map map, Game game, IRenderPlayer player, MapEventTrigger trigger, uint x, uint y,
            IMapManager mapManager, uint ticks, MapEvent mapEvent, ref bool lastEventStatus)
        {
            switch (mapEvent.Type)
            {
                case MapEventType.MapChange:
                {
                    if (!(mapEvent is MapChangeEvent mapChangeEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid map change event.");

                    var newMap = mapManager.GetMap(mapChangeEvent.MapIndex);

                    // The position (x, y) is 1-based in the data so we subtract 1.
                    // Moreover the players position is 1 tile below its drawing position
                    // in non-world 2D so subtract another 1 from y.
                    player.MoveTo(newMap, mapChangeEvent.X - 1,
                        mapChangeEvent.Y - (newMap.Type == MapType.Map2D && !newMap.IsWorldMap ? 2u : 1u),
                        ticks, true, mapChangeEvent.Direction);

                    break;
                }
                // TODO ...
                case MapEventType.Condition:
                {
                    if (!(mapEvent is ConditionEvent conditionEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid condition event.");

                    switch (conditionEvent.TypeOfCondition)
                    {
                        case ConditionEvent.ConditionType.MapVariable:
                            if (game.GetMapVariables(map)[conditionEvent.ObjectIndex] != conditionEvent.Value)
                                return null; // no further event execution
                            break;
                        case ConditionEvent.ConditionType.GlobalVariable:
                            if (game.GlobalVariables[conditionEvent.ObjectIndex] != conditionEvent.Value)
                                return null; // no further event execution
                            break;
                        case ConditionEvent.ConditionType.Hand:
                            if (trigger != MapEventTrigger.Hand)
                                return null; // no further event execution
                            break;
                        case ConditionEvent.ConditionType.Success:
                            if (!lastEventStatus)
                                return null; // no further event execution
                            break;
                        case ConditionEvent.ConditionType.UseItem:
                        {
                            var mapEventIfFalse = conditionEvent.ContinueIfFalseWithMapEventIndex == 0xffff
                                ? null : map.Events[(int)conditionEvent.ContinueIfFalseWithMapEventIndex]; // TODO: is this right or +/- 1?

                            if (trigger < MapEventTrigger.Item0)
                                return mapEventIfFalse; // no item used

                            uint itemIndex = (uint)(trigger - MapEventTrigger.Item0);

                            if (itemIndex != conditionEvent.ObjectIndex)
                                return mapEventIfFalse; // wrong item used

                            break;
                        }
                        // TODO ...
                    }

                    break;
                }
                // TODO ...
            }

            // TODO: battles, chest looting, etc should set this dependent on:
            // - battle won
            // - chest fully looted
            // ...
            lastEventStatus = true;

            return mapEvent.Next;
        }

        public static void TriggerEvents(this Map map, Game game, IRenderPlayer player,
            MapEventTrigger trigger, uint x, uint y, IMapManager mapManager, uint ticks)
        {
            var mapEventId = map.Type == MapType.Map2D ? map.Tiles[x, y].MapEventId : map.Blocks[x, y].MapEventId;

            if (mapEventId == 0)
                return; // no map events at this position

            bool lastEventStatus = false;
            var mapEvent = map.EventLists[(int)mapEventId - 1];

            while (mapEvent != null)
            {
                mapEvent = ExecuteEvent(map, game, player, trigger, x, y, mapManager, ticks, mapEvent, ref lastEventStatus);
            }
        }

    }
}
