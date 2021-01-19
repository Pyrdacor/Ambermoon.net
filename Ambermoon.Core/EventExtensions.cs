using Ambermoon.Data;
using System;
using System.Linq;

namespace Ambermoon
{
    internal static class EventExtensions
    {
        public static Event ExecuteEvent(this Event @event, Map map, Game game,
            EventTrigger trigger, uint x, uint y, uint ticks, ref bool lastEventStatus,
            out bool aborted, IConversationPartner conversationPartner = null)
        {
            // Note: Aborted means that an event is not even executed. It does not mean that a decision
            // box is answered with No for example. It is used when:
            // - A condition of a condition event is not met and there is no event that is triggered in that case.
            // - A text popup does not accept the given trigger.
            // This is important in 3D when there might be an event on the current block and on the next one.
            // For example buttons use 2 events (one for Eye interaction and one for Hand interaction).

            aborted = false;

            switch (@event.Type)
            {
                case EventType.MapChange:
                {
                    if (trigger != EventTrigger.Move &&
                        trigger != EventTrigger.Always)
                        return null;

                    if (!(@event is MapChangeEvent mapChangeEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid map change event.");

                    game.Teleport(mapChangeEvent);
                    break;
                }
                case EventType.Chest:
                {
                    if (!(@event is ChestEvent chestMapEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid chest event.");

                    game.ShowChest(chestMapEvent, map);
                    return null;
                }
                case EventType.PopupText:
                {
                    if (!(@event is PopupTextEvent popupTextEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid text popup event.");

                    switch (trigger)
                    {
                        case EventTrigger.Move:
                            if (!popupTextEvent.CanTriggerByMoving)
                            {
                                aborted = true;
                                return null;
                            }
                            break;
                        case EventTrigger.Eye:
                            if (!popupTextEvent.CanTriggerByCursor)
                            {
                                aborted = true;
                                return null;
                            }
                            break;
                        case EventTrigger.Always:
                            break;
                        default:
                            aborted = true;
                            return null;
                    }

                    bool eventStatus = lastEventStatus;

                    game.ShowTextPopup(map, popupTextEvent, _ =>
                    {
                        map.TriggerEventChain(game, EventTrigger.Always,
                            x, y, game.CurrentTicks, @event.Next, eventStatus);
                    });
                    return null; // next event is only executed after popup response
                }
                case EventType.Riddlemouth:
                {
                    if (trigger != EventTrigger.Always &&
                        trigger != EventTrigger.Eye &&
                        trigger != EventTrigger.Hand &&
                        trigger != EventTrigger.Mouth)
                        return null;

                    if (!(@event is RiddlemouthEvent riddleMouthEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid riddle mouth event.");

                    game.ShowRiddlemouth(map, riddleMouthEvent, () =>
                    {
                        map.TriggerEventChain(game, EventTrigger.Always,
                            x, y, game.CurrentTicks, @event.Next, true);
                    });
                    return null; // next event is only executed after popup response
                }
                case EventType.Decision:
                {
                    if (!(@event is DecisionEvent decisionEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid decision event.");

                    game.ShowDecisionPopup(map, decisionEvent, response =>
                    {
                        if (response == PopupTextEvent.Response.Yes)
                        {
                            map.TriggerEventChain(game, EventTrigger.Always,
                                x, y, game.CurrentTicks, @event.Next, true);
                        }
                        else // Close and No have the same meaning here
                        {
                            if (decisionEvent.NoEventIndex != 0xffff)
                            {
                                map.TriggerEventChain(game, EventTrigger.Always,
                                    x, y, game.CurrentTicks, map.Events[(int)decisionEvent.NoEventIndex], false);
                            }
                        }
                    });
                    return null; // next event is only executed after popup response
                }
                case EventType.ChangeTile:
                {
                    // TODO: add those to the savegame as well!
                    if (!(@event is ChangeTileEvent changeTileEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid chest event.");

                    game.UpdateMapTile(changeTileEvent);
                    break;
                }
                // TODO ...
                case EventType.Condition:
                {
                    if (!(@event is ConditionEvent conditionEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid condition event.");

                    var mapEventIfFalse = conditionEvent.ContinueIfFalseWithMapEventIndex == 0xffff
                        ? null : map.Events[(int)conditionEvent.ContinueIfFalseWithMapEventIndex]; // TODO: is this right or +/- 1?

                    switch (conditionEvent.TypeOfCondition)
                    {
                        case ConditionEvent.ConditionType.GlobalVariable:
                            if (game.CurrentSavegame.GetGlobalVariable(conditionEvent.ObjectIndex) != (conditionEvent.Value != 0))
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        case ConditionEvent.ConditionType.EventBit:
                            if (game.CurrentSavegame.GetEventBit(map.Index, conditionEvent.ObjectIndex & 0x3f) != (conditionEvent.Value != 0))
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        case ConditionEvent.ConditionType.CharacterBit:
                            if (game.CurrentSavegame.GetCharacterBit(map.Index, conditionEvent.ObjectIndex & 0x1f) != (conditionEvent.Value != 0))
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        case ConditionEvent.ConditionType.Hand:
                            if (trigger != EventTrigger.Hand)
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        case ConditionEvent.ConditionType.Eye:
                            if (trigger != EventTrigger.Eye)
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
                            if (trigger < EventTrigger.Item0)
                            {
                                // no item used
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }

                            uint itemIndex = (uint)(trigger - EventTrigger.Item0);

                            if (itemIndex != conditionEvent.ObjectIndex)
                            {
                                // wrong item used
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        }
                        case ConditionEvent.ConditionType.PartyMember:
                        {
                            if (game.PartyMembers.Any(m => m.Index == conditionEvent.ObjectIndex) != (conditionEvent.Value != 0))
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        }
                        case ConditionEvent.ConditionType.ItemOwned:
                        {
                            int totalCount = 0;

                            foreach (var partyMember in game.PartyMembers)
                            {
                                foreach (var slot in partyMember.Inventory.Slots)
                                {
                                    if (slot.ItemIndex == conditionEvent.ObjectIndex)
                                        totalCount += slot.Amount;
                                }
                                foreach (var slot in partyMember.Equipment.Slots)
                                {
                                    if (slot.Value.ItemIndex == conditionEvent.ObjectIndex)
                                        totalCount += slot.Value.Amount;
                                }
                            }

                            if (totalCount != conditionEvent.Value)
                            {
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
                case EventType.Action:
                {
                    if (!(@event is ActionEvent actionEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid action event.");

                    switch (actionEvent.TypeOfAction)
                    {
                        case ActionEvent.ActionType.SetGlobalVariable:
                            game.CurrentSavegame.SetGlobalVariable(actionEvent.ObjectIndex, actionEvent.Value != 0);
                            break;
                        case ActionEvent.ActionType.SetEventBit:
                            game.SetMapEventBit(map.Index, actionEvent.ObjectIndex & 0x3fu, actionEvent.Value != 0);
                            break;
                        case ActionEvent.ActionType.SetCharacterBit:
                            game.SetMapCharacterBit(map.Index, actionEvent.ObjectIndex & 0x1f, actionEvent.Value != 0);
                            break;
                            // TODO ...
                    }

                    break;
                }
                case EventType.Dice100Roll:
                {
                    if (!(@event is Dice100RollEvent diceEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid dice 100 event.");

                    var mapEventIfFalse = diceEvent.ContinueIfFalseWithMapEventIndex == 0xffff
                        ? null : map.Events[(int)diceEvent.ContinueIfFalseWithMapEventIndex]; // TODO: is this right or +/- 1?
                    lastEventStatus = game.RollDice100() < diceEvent.Chance;
                    return lastEventStatus ? diceEvent.Next : mapEventIfFalse;
                }
                case EventType.StartBattle:
                {
                    if (!(@event is StartBattleEvent battleEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid battle event.");

                    uint GetCombatBackground()
                    {
                        var tile = map.Tiles[x, y];
                        var tileset = game.MapManager.GetTilesetForMap(map);
                        var tilesetTile = tile.BackTileIndex == 0
                            ? tileset.Tiles[tile.FrontTileIndex - 1]
                            : tileset.Tiles[tile.BackTileIndex - 1];
                        return tilesetTile.CombatBackgroundIndex;
                    }

                    uint? combatBackgroundIndex = map.Type == MapType.Map2D ? GetCombatBackground() : (uint?)null;
                    game.StartBattle(battleEvent, battleEvent.Next, combatBackgroundIndex);
                    return null;
                }
                case EventType.Conversation:
                {
                    if (!(@event is ConversationEvent conversationEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid conversation event.");

                    switch (conversationEvent.Interaction)
                    {
                        case ConversationEvent.InteractionType.Keyword:
                            // TODO: this has to be handled by the conversation window
                            aborted = true;
                            return null;
                        case ConversationEvent.InteractionType.ShowItem:
                            // TODO: this has to be handled by the conversation window
                            aborted = true;
                            return null;
                        case ConversationEvent.InteractionType.GiveItem:
                            // TODO: this has to be handled by the conversation window
                            aborted = true;
                            return null;
                        case ConversationEvent.InteractionType.Talk:
                            if (trigger != EventTrigger.Mouth)
                            {
                                aborted = true;
                                return null;
                            }
                            break;
                        case ConversationEvent.InteractionType.Leave:
                            // TODO: this has to be handled by the conversation window
                            aborted = true;
                            return null;
                        default:
                            // TODO
                            Console.WriteLine($"Found unknown conversation interaction type: {conversationEvent.Interaction}");
                            aborted = true;
                            return null;
                    }
                    game.ShowConversation(conversationPartner, conversationEvent.Next);
                    return null;
                }
                case EventType.PrintText:
                {
                    if (!(@event is PrintTextEvent printTextEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid print text event.");

                    // Note: This is only used by conversations and is handled by game.ShowConversation.
                    // So we don't need to do anything here.
                    return printTextEvent.Next;
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

            return @event.Next;
        }

        public static bool TriggerEventChain(this Map map, Game game, EventTrigger trigger, uint x, uint y,
            uint ticks, Event firstMapEvent, bool lastEventStatus = false)
        {
            var mapEvent = firstMapEvent;

            while (mapEvent != null)
            {
                mapEvent = mapEvent.ExecuteEvent(map, game, trigger, x, y, ticks, ref lastEventStatus, out bool aborted);

                if (aborted)
                    return false;
            }

            return true;
        }
    }
}
