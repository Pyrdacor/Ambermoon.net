using Ambermoon.Data;
using Ambermoon.Render;

namespace Ambermoon
{
    public enum MapEventTrigger
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

    public static class MapExtensions
    {
        static uint? LastMapEventIndexMap = null; // TODO: do it better
        static uint? LastMapEventIndex = null; // TODO: do it better

        public static uint PositionToTileIndex(this Map map, uint x, uint y) => x + y * (uint)map.Width;

        static MapEvent ExecuteEvent(Map map, Game game, IRenderPlayer player, MapEventTrigger trigger, uint x, uint y,
            IMapManager mapManager, uint ticks, MapEvent mapEvent, ref bool lastEventStatus, out bool aborted)
        {
            // Note: Aborted means that an event is not even executed. It does not mean that a decision
            // box is answered with No for example. It is used when:
            // - A condition of a condition event is not met and there is no event that is triggered in that case.
            // - A text popup does not accept the given trigger.
            // This is important in 3D when there might be an event on the current block and on the next one.
            // For example buttons use 2 events (one for Eye interaction and one for Hand interaction).

            aborted = false;

            switch (mapEvent.Type)
            {
                case MapEventType.MapChange:
                {
                    if (trigger != MapEventTrigger.Move &&
                        trigger != MapEventTrigger.Always)
                        return null;

                    if (!(mapEvent is MapChangeEvent mapChangeEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid map change event.");

                    game.Teleport(mapChangeEvent);
                    break;
                }
                case MapEventType.Chest:
                {
                    if (!(mapEvent is ChestMapEvent chestMapEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid chest event.");

                    game.ShowChest(chestMapEvent);
                    break;
                }
                case MapEventType.PopupText:
                {
                    if (!(mapEvent is PopupTextEvent popupTextEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid text popup event.");

                    switch (trigger)
                    {
                        case MapEventTrigger.Move:
                            if (!popupTextEvent.CanTriggerByMoving)
                            {
                                aborted = true;
                                return null;
                            }
                            break;
                        case MapEventTrigger.Eye:
                        case MapEventTrigger.Hand:
                            if (!popupTextEvent.CanTriggerByCursor)
                            {
                                aborted = true;
                                return null;
                            }
                            break;
                        default:
                            aborted = true;
                            return null;
                    }

                    bool eventStatus = lastEventStatus;

                    game.ShowTextPopup(map, popupTextEvent, _ =>
                    {
                        TriggerEventChain(map, game, player, MapEventTrigger.Always, x, y, mapManager,
                            game.CurrentTicks, mapEvent.Next, eventStatus);
                    });
                    return null; // next event is only executed after popup response
                }
                case MapEventType.Riddlemouth:
                {
                    if (trigger != MapEventTrigger.Always &&
                        trigger != MapEventTrigger.Eye &&
                        trigger != MapEventTrigger.Hand &&
                        trigger != MapEventTrigger.Mouth)
                        return null;

                    if (!(mapEvent is RiddlemouthEvent riddleMouthEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid riddle mouth event.");

                    game.ShowRiddlemouth(map, riddleMouthEvent, () =>
                    {
                        TriggerEventChain(map, game, player, MapEventTrigger.Always, x, y, mapManager,
                            game.CurrentTicks, mapEvent.Next, true);
                    });
                    return null; // next event is only executed after popup response
                }
                case MapEventType.Decision:
                {
                    if (!(mapEvent is DecisionEvent decisionEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid decision event.");

                    game.ShowDecisionPopup(map, decisionEvent, response =>
                    {
                        if (response == PopupTextEvent.Response.Yes)
                        {
                            TriggerEventChain(map, game, player, MapEventTrigger.Always, x, y, mapManager,
                                game.CurrentTicks, mapEvent.Next, true);
                        }
                        else // Close and No have the same meaning here
                        {
                            if (decisionEvent.NoEventIndex != 0xffff)
                            {
                                TriggerEventChain(map, game, player, MapEventTrigger.Always, x, y, mapManager,
                                    game.CurrentTicks, map.Events[(int)decisionEvent.NoEventIndex], false);
                            }
                        }
                    });
                    return null; // next event is only executed after popup response
                }
                case MapEventType.ChangeTile:
                {
                    if (!(mapEvent is ChangeTileEvent changeTileEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid chest event.");

                    game.UpdateMapTile(changeTileEvent);
                    break;
                }
                // TODO ...
                case MapEventType.Condition:
                {
                    if (!(mapEvent is ConditionEvent conditionEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid condition event.");

                    var mapEventIfFalse = conditionEvent.ContinueIfFalseWithMapEventIndex == 0xffff
                        ? null : map.Events[(int)conditionEvent.ContinueIfFalseWithMapEventIndex]; // TODO: is this right or +/- 1?

                    switch (conditionEvent.TypeOfCondition)
                    {
                        case ConditionEvent.ConditionType.MapVariable:
                            if (game.GetMapVariables(map)[conditionEvent.ObjectIndex] != conditionEvent.Value)
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        case ConditionEvent.ConditionType.GlobalVariable:
                            if (game.GlobalVariables[conditionEvent.ObjectIndex] != conditionEvent.Value)
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        case ConditionEvent.ConditionType.Hand:
                            if (trigger != MapEventTrigger.Hand)
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        case ConditionEvent.ConditionType.Success:
                            if (!lastEventStatus)
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        case ConditionEvent.ConditionType.UseItem:
                        {
                            if (trigger < MapEventTrigger.Item0)
                            {
                                // no item used
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }

                            uint itemIndex = (uint)(trigger - MapEventTrigger.Item0);

                            if (itemIndex != conditionEvent.ObjectIndex)
                            {
                                // wrong item used
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        }
                        // TODO ...
                    }

                    break;
                }
                case MapEventType.Action:
                {
                    if (!(mapEvent is ActionEvent actionEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid action event.");

                    switch (actionEvent.TypeOfAction)
                    {
                        case ActionEvent.ActionType.SetMapVariable:
                            game.GetMapVariables(map)[actionEvent.ObjectIndex] = (int)actionEvent.Value;
                            break;
                        case ActionEvent.ActionType.SetGlobalVariable:
                            game.GlobalVariables[actionEvent.ObjectIndex] = (int)actionEvent.Value;
                            break;
                        // TODO ...
                    }

                    break;
                }
                case MapEventType.Dice100Roll:
                {
                    if (!(mapEvent is Dice100RollEvent diceEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid dice 100 event.");

                    var mapEventIfFalse = diceEvent.ContinueIfFalseWithMapEventIndex == 0xffff
                        ? null : map.Events[(int)diceEvent.ContinueIfFalseWithMapEventIndex]; // TODO: is this right or +/- 1?
                    lastEventStatus = game.RollDice100() < diceEvent.Chance;
                    return lastEventStatus ? diceEvent.Next : mapEventIfFalse;
                }
                // TODO ...
            }

            // TODO: battles, chest looting, etc should set this dependent on:
            // - battle won
            // - chest fully looted
            // ...
            // TODO: to do so we have to memorize the current event chain and continue
            // it after the player has done some actions (loot a chest, fight a battle, etc).
            // maybe we need a state machine here instead of just a linked list and a loop.
            lastEventStatus = true;

            return mapEvent.Next;
        }

        static bool TriggerEventChain(Map map, Game game, IRenderPlayer player, MapEventTrigger trigger, uint x, uint y,
            IMapManager mapManager, uint ticks, MapEvent firstMapEvent, bool lastEventStatus = false)
        {
            var mapEvent = firstMapEvent;

            while (mapEvent != null)
            {
                mapEvent = ExecuteEvent(map, game, player, trigger, x, y, mapManager, ticks, mapEvent, ref lastEventStatus, out bool aborted);

                if (aborted)
                    return false;
            }

            return true;
        }

        public static bool TriggerEvents(this Map map, Game game, IRenderPlayer player, MapEventTrigger trigger,
            uint x, uint y, IMapManager mapManager, uint ticks)
        {
            return TriggerEvents(map, game, player, trigger, x, y, mapManager, ticks, out bool _, false);
        }

        public static bool TriggerEvents(this Map map, Game game, IRenderPlayer player, MapEventTrigger trigger,
            uint x, uint y, IMapManager mapManager, uint ticks, out bool hasMapEvent, bool noIndexReset = false)
        {
            var mapEventId = map.Type == MapType.Map2D ? map.Tiles[x, y].MapEventId : map.Blocks[x, y].MapEventId;
            hasMapEvent = mapEventId != 0;

            if (trigger == MapEventTrigger.Move && LastMapEventIndexMap == map.Index && LastMapEventIndex == mapEventId)
                return false;

            if (!noIndexReset || mapEventId != 0)
            {
                LastMapEventIndexMap = map.Index;
                LastMapEventIndex = mapEventId;
            }

            if (mapEventId == 0)
                return false; // no map events at this position

            return TriggerEventChain(map, game, player, trigger, x, y, mapManager, ticks, map.EventLists[(int)mapEventId - 1]);
        }

        public static void ClearLastEvent(this Map map)
        {
            LastMapEventIndexMap = map.Index;
            LastMapEventIndex = 0;
        }
    }
}
