/*
 * EventExtensions.cs - Makes triggering events easier
 *
 * Copyright (C) 2020-2021  Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of Ambermoon.net.
 *
 * Ambermoon.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Ambermoon.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Ambermoon.net. If not, see <http://www.gnu.org/licenses/>.
 */

using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon
{
    internal static class EventExtensions
    {
        public class EventProvider
        {
            public Event Event { get; private set; } = null;
            public void Provide(Event @event)
            {
                Event = @event;
                Provided?.Invoke(@event);
            }

            public event Action<Event> Provided;
        }

        public static Event ExecuteEvent(this Event @event, Map map, Game game,
            ref EventTrigger trigger, uint x, uint y, ref bool lastEventStatus,
            out bool aborted, out EventProvider eventProvider,
            IConversationPartner conversationPartner = null, uint? characterIndex = null)
        {
            eventProvider = null;

            // Note: Aborted means that an event is not even executed. It does not mean that a decision
            // box is answered with No for example. It is used when:
            // - A condition of a condition event is not met and there is no event that is triggered in that case.
            // - A text popup does not accept the given trigger.
            // This is important in 3D when there might be an event on the current block and on the next one.
            // For example buttons use 2 events (one for Eye interaction and one for Hand interaction).

            aborted = false;
            var events = conversationPartner == null ? map.Events : conversationPartner.Events;

            switch (@event.Type)
            {
                case EventType.Teleport:
                {
                    if (trigger != EventTrigger.Move &&
                        trigger != EventTrigger.Always)
                    {
                        aborted = true;
                        return null;
                    }

                    if (!(@event is TeleportEvent teleportEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid teleport event.");

                    game.Teleport(teleportEvent, x, y);

                    // Note: The teleporter from Mine 1 to 2 has a teleport event which has another
                    // one as its next event. We have to avoid further event execution by just return
                    // null here. I guess teleport events are not allowed to chain anything and this
                    // might be a data bug.
                    return null;
                }
                case EventType.Door:
                {
                    if (!(@event is DoorEvent doorEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid door event.");

                    if (!game.ShowDoor(doorEvent, false, false, map, x, y, true, trigger == EventTrigger.Move))
                    {
                        // already unlocked
                        // Note that the original sets last event status to false in this case!
                        lastEventStatus = false;
                        return doorEvent.Next;
                    }
                    return null;
                }
                case EventType.Chest:
                {
                    if (trigger == EventTrigger.Mouth)
                    {
                        aborted = true;
                        return null;
                    }

                    if (!(@event is ChestEvent chestEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid chest event.");

                    if (chestEvent.SearchSkillCheck &&
                        game.RandomInt(0, 99) >= game.CurrentPartyMember.Skills[Skill.Searching].TotalCurrentValue)
                    {
                        aborted = true;
                        return null;
                    }

                    aborted = !game.ShowChest(chestEvent, false, false, map, new Position((int)x, (int)y), true);
                    return null;
                }
                case EventType.PopupText:
                {
                    if (!(@event is PopupTextEvent popupTextEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid text popup event.");

                    // Only check trigger if this is the first event of the chain.
                    if (map.EventList.Contains(@event))
                    {
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

                        if (!popupTextEvent.TriggerIfBlind && !game.CanSee())
                        {
                            aborted = true;
                            return null;
                        }
                    }                    

                    bool eventStatus = lastEventStatus;
                    EventProvider provider = null;

                    if (conversationPartner != null)
                        provider = eventProvider = new EventProvider();

                    game.ShowTextPopup(map, popupTextEvent, _ =>
                    {
                        if (@event.Next != null)
                        {
                            if (conversationPartner == null)
                            {
                                map.TriggerEventChain(game, EventTrigger.Always, x, y, @event.Next, eventStatus);
                            }
                            else
                            {
                                provider?.Provide(popupTextEvent.Next);
                            }
                        }
                        else
                        {
                            game.ResetMapCharacterInteraction(map);
                            if (conversationPartner != null)
                                provider?.Provide(null);
                        }
                    });
                    return null; // next event is only executed after popup response
                }
                case EventType.Spinner:
                {
                    if (trigger == EventTrigger.Eye)
                    {
                        game.ShowMessagePopup(game.DataNameProvider.SeeRoundDiskInFloor);
                        aborted = true;
                        return null;
                    }

                    if (trigger != EventTrigger.Move &&
                        trigger != EventTrigger.Always)
                        return null;

                    if (!(@event is SpinnerEvent spinnerEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid spinner event.");

                    game.Spin(spinnerEvent.Direction, spinnerEvent.Next);
                    break;
                }
                case EventType.Trap:
                {
                    if (!(@event is TrapEvent trapEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid trap event.");

                    if (trigger == EventTrigger.Eye)
                    {
                        // Note: Eye will only detect the trap, not trigger it!
                        game.ShowMessagePopup(game.DataNameProvider.YouNoticeATrap);
                        aborted = true;
                        return null;
                    }
                    else if (trigger != EventTrigger.Move && trigger != EventTrigger.Always)
                    {
                        aborted = true;
                        return null;
                    }

                    game.TriggerTrap(trapEvent, lastEventStatus, x, y);
                    return null; // next event is only executed after trap effect
                }
                case EventType.ChangeBuffs:
                {
                    if (!(@event is ChangeBuffsEvent changeBuffsEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid change buffs event.");

                    if (changeBuffsEvent.AffectedBuff == null) // all
                    {
                        for (int i = 0; i < 6; ++i)
                        {
                            if (changeBuffsEvent.Add)
                                game.ActivateBuff((ActiveSpellType)i, changeBuffsEvent.Value, changeBuffsEvent.Duration);
                            else
                                game.CurrentSavegame.ActiveSpells[i] = null;                            
                        }

                        if (!changeBuffsEvent.Add)
                            game.UpdateLight();
                    }
                    else
                    {
                        int index = (int)changeBuffsEvent.AffectedBuff;

                        if (index < 6)
                        {
                            if (changeBuffsEvent.Add)
                            {
                                game.ActivateBuff((ActiveSpellType)index, changeBuffsEvent.Value, changeBuffsEvent.Duration);
                            }
                            else
                            {
                                game.CurrentSavegame.ActiveSpells[index] = null;

                                if (index == (int)ActiveSpellType.Light)
                                    game.UpdateLight();
                            }
                        }
                    }
                    break;
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
                        map.TriggerEventChain(game, EventTrigger.Always, x, y, @event.Next, true);
                    });
                    return null; // next event is only executed after popup response
                }
                case EventType.Reward:
                {
                    if (!(@event is RewardEvent rewardEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid reward event.");
                    EventProvider provider = null;
                    if (conversationPartner != null)
                        provider = eventProvider = new EventProvider();
                    bool eventStatus = lastEventStatus;
                    void Reward(PartyMember partyMember, Action followAction) => game.RewardPlayer(partyMember, rewardEvent, followAction);
                    void Done()
                    {
                        if (rewardEvent.Next != null)
                        {
                            if (conversationPartner == null)
                                TriggerEventChain(map, game, EventTrigger.Always, x, y, rewardEvent.Next, eventStatus);
                            else
                                provider?.Provide(rewardEvent.Next);
                        }
                        else if (conversationPartner != null)
                            provider?.Provide(null);
                    }
                    switch (rewardEvent.Target)
                    {
                        case RewardEvent.RewardTarget.ActivePlayer:
                            Reward(game.CurrentPartyMember, Done);
                            break;
                        case RewardEvent.RewardTarget.RandomPlayer:
                        {
                            var partyMembers = game.PartyMembers.Where(p => p != null && p.Alive && !p.Conditions.HasFlag(Condition.Petrified)).ToArray();
                            int index = game.RandomInt(0, partyMembers.Length - 1);
                            Reward(partyMembers[index], Done);
                            break;
                        }
                        case RewardEvent.RewardTarget.FirstAnimal:
                        {
                            var animal = game.PartyMembers.FirstOrDefault(p => p.Race == Race.Animal);

                            if (animal == null)
                            {
                                aborted = true;
                                return null;
                            }

                            Reward(animal, Done);
                            break;
                        }
                        case RewardEvent.RewardTarget.All:
                            if (rewardEvent.TypeOfReward == RewardEvent.RewardType.HitPoints &&
                                (rewardEvent.Operation == RewardEvent.RewardOperation.Decrease ||
                                rewardEvent.Operation == RewardEvent.RewardOperation.DecreasePercentage))
                            {
                                Func<PartyMember, uint> damageProvider = rewardEvent.Operation == RewardEvent.RewardOperation.Decrease
                                    ? (Func<PartyMember, uint>)(_ => rewardEvent.Value) : p => rewardEvent.Value * p.HitPoints.TotalMaxValue / 100;

                                // Note: Rewards damage silently.
                                game.DamageAllPartyMembers(damageProvider, p => p.Alive, null, _ => Done(), Condition.None, false);
                            }
                            else
                            {
                                game.ForeachPartyMember(Reward, p => p.Alive, Done);
                            }
                            break;
                        default:
                            return rewardEvent.Next;
                    }
                    return null;
                }
                case EventType.ChangeTile:
                {
                    if (!(@event is ChangeTileEvent changeTileEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid chest event.");

                    // Note: Savegame stores the front tile index for 2D and wall/object index for 3D.
                    // Note: If map index is 0 (same map) we have to replace it with the real map index
                    // for savegames. Otherwise it will be interpreted as "end of tile changes marker".
                    // Clone as we change the event and it might be used several times.
                    var changeTileEventClone = new ChangeTileEvent
                    {
                        Type = changeTileEvent.Type,
                        X = changeTileEvent.X,
                        Y = changeTileEvent.Y,
                        MapIndex = changeTileEvent.MapIndex,
                        FrontTileIndex = changeTileEvent.FrontTileIndex,
                        Index = changeTileEvent.Index,
                        Next = changeTileEvent.Next,
                        Unknown = changeTileEvent.Unknown
                    };
                    if (changeTileEventClone.MapIndex == 0)
                        changeTileEventClone.MapIndex = map.Index;
                    if (changeTileEventClone.X == 0)
                        changeTileEventClone.X = x + 1;
                    if (changeTileEventClone.Y == 0)
                        changeTileEventClone.Y = y + 1;

                    game.UpdateMapTile(changeTileEventClone, x, y);
                    break;
                }
                case EventType.StartBattle:
                {
                    if (trigger != EventTrigger.Move &&
                        trigger != EventTrigger.Always)
                    {
                        aborted = true;
                        return null;
                    }

                    if (!(@event is StartBattleEvent battleEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid battle event.");

                    game.StartBattle(battleEvent, battleEvent.Next, game.GetCombatBackgroundIndex(map, x, y));
                    return null;
                }
                case EventType.EnterPlace:
                {
                    if (trigger != EventTrigger.Move &&
                        trigger != EventTrigger.Always)
                    {
                        aborted = true;
                        return null;
                    }

                    if (!(@event is EnterPlaceEvent enterPlaceEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid place event.");

                    if (!game.EnterPlace(map, enterPlaceEvent))
                        aborted = true;
                    return null;
                }
                case EventType.Condition:
                {
                    if (!(@event is ConditionEvent conditionEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid condition event.");

                    var mapEventIfFalse = conditionEvent.ContinueIfFalseWithMapEventIndex == 0xffff
                        ? null : events[(int)conditionEvent.ContinueIfFalseWithMapEventIndex];

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
                            if (game.CurrentSavegame.GetEventBit(1 + (conditionEvent.ObjectIndex >> 6), conditionEvent.ObjectIndex & 0x3f) != (conditionEvent.Value != 0))
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        case ConditionEvent.ConditionType.DoorOpen:
                            if (game.CurrentSavegame.IsDoorLocked(conditionEvent.ObjectIndex) != (conditionEvent.Value == 0))
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        case ConditionEvent.ConditionType.ChestOpen:
                            if (game.CurrentSavegame.IsChestLocked(conditionEvent.ObjectIndex) != (conditionEvent.Value == 0))
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        case ConditionEvent.ConditionType.CharacterBit:
                            if (game.CurrentSavegame.GetCharacterBit(1 + (conditionEvent.ObjectIndex >> 5), conditionEvent.ObjectIndex & 0x1f)
                                != (conditionEvent.Value != 0))
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        case ConditionEvent.ConditionType.PartyMember:
                        {
                            if (game.PartyMembers.Any(m => (m.Index == conditionEvent.ObjectIndex &&
                                ((uint)m.Conditions & (uint)conditionEvent.DisallowedAilments) == 0)) != (conditionEvent.Value != 0))
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

                            if (conditionEvent.Value == 0 && totalCount != 0)
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            else if (conditionEvent.Value == 1 && (totalCount == 0 || totalCount < conditionEvent.Count))
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }

                            break;
                        }
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
                        case ConditionEvent.ConditionType.KnowsKeyword:
                            if (game.CurrentSavegame.IsDictionaryWordKnown(conditionEvent.ObjectIndex) != (conditionEvent.Value != 0))
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        case ConditionEvent.ConditionType.LastEventResult:
                            if (lastEventStatus != (conditionEvent.Value != 0))
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        case ConditionEvent.ConditionType.GameOptionSet:
                            if (game.CurrentSavegame.IsGameOptionActive((Data.Enumerations.Option)(1 << (int)conditionEvent.ObjectIndex)) != (conditionEvent.Value != 0))
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        case ConditionEvent.ConditionType.CanSee:
                        {
                            bool canSee = game.CanSee();
                            if (canSee != (conditionEvent.Value != 0))
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        }
                        case ConditionEvent.ConditionType.Direction:
                        {
                            if ((game.PlayerDirection == (CharacterDirection)conditionEvent.ObjectIndex) != (conditionEvent.Value != 0))
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        }
                        case ConditionEvent.ConditionType.HasCondition:
                            if (game.CurrentPartyMember.Conditions.HasFlag((Condition)(1 << (int)conditionEvent.ObjectIndex)) != (conditionEvent.Value != 0))
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        case ConditionEvent.ConditionType.Hand:
                            if ((trigger == EventTrigger.Hand) != (conditionEvent.Value != 0))
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        case ConditionEvent.ConditionType.SayWord:
                        {
                            if ((trigger == EventTrigger.Mouth) != (conditionEvent.Value != 0))
                            {
                                aborted = true;
                                return null;
                            }
                            if (trigger == EventTrigger.Mouth)
                            {
                                game.SayWord(map, x, y, events, conditionEvent);
                                return null;
                            }
                            break;
                        }
                        case ConditionEvent.ConditionType.EnterNumber:
                        {
                            game.EnterNumber(map, x, y, events, conditionEvent);
                            return null;
                        }
                        case ConditionEvent.ConditionType.Levitating:
                            if ((trigger == EventTrigger.Levitating) != (conditionEvent.Value != 0))
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        case ConditionEvent.ConditionType.HasGold:
                            if ((game.CurrentPartyMember.Gold >= conditionEvent.ObjectIndex) != (conditionEvent.Value != 0))
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        case ConditionEvent.ConditionType.HasFood:
                            if ((game.CurrentPartyMember.Food >= conditionEvent.ObjectIndex) != (conditionEvent.Value != 0))
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        case ConditionEvent.ConditionType.Eye:
                            if ((trigger == EventTrigger.Eye) != (conditionEvent.Value != 0))
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        // New Ambermoon Advanced conditions
                        case ConditionEvent.ConditionType.Mouth:
                            if ((trigger == EventTrigger.Mouth) != (conditionEvent.Value != 0))
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        case ConditionEvent.ConditionType.TransportAtLocation:
                        {
                            var transport = game.CurrentSavegame.TransportLocations.FirstOrDefault(l => l != null && l.MapIndex == map.Index && l.Position.X == x + 1 && l.Position.Y == y + 1);
                            bool result = true;
                            if (transport == null)
                            {
                                result = false;
                            }
                            else if (conditionEvent.ObjectIndex != 0 && conditionEvent.ObjectIndex != (uint)transport.TravelType)
                            {
                                result = false;
                            }
                            if (result != (conditionEvent.Value != 0))
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        }
                        case ConditionEvent.ConditionType.MultiCursor:
                        {
                            var flags = conditionEvent.ObjectIndex;
                            bool result = false;

                            if (trigger == EventTrigger.Hand && (flags & 0x1) != 0)
                                result = true;
                            else if (trigger == EventTrigger.Eye && (flags & 0x2) != 0)
                                result = true;
                            else if (trigger == EventTrigger.Mouth && (flags & 0x4) != 0)
                                result = true;
                            if (result != (conditionEvent.Value != 0))
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        }
                        case ConditionEvent.ConditionType.TravelType:
                            if (((uint)game.TravelType == conditionEvent.ObjectIndex) != (conditionEvent.Value != 0))
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        case ConditionEvent.ConditionType.LeadClass:
                            if (((uint)game.CurrentPartyMember.Class == conditionEvent.ObjectIndex) != (conditionEvent.Value != 0))
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        case ConditionEvent.ConditionType.SpellEmpowered:
                        {
                            uint elementIndex = conditionEvent.ObjectIndex;

                            if (elementIndex > 2)
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }

                            int mask = (1 << (4 + (int)elementIndex));

                            if ((((int)game.CurrentPartyMember.BattleFlags & mask) != 0) != (conditionEvent.Value != 0))
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        }
                        case ConditionEvent.ConditionType.IsNight:
                            if (game.IsNight() != (conditionEvent.Value != 0))
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        case ConditionEvent.ConditionType.Attribute:
                        {
                            var attribute = game.CurrentPartyMember.Attributes[(Data.Attribute)conditionEvent.ObjectIndex];
                            var totalValue = attribute.CurrentValue + attribute.BonusValue;
                            if ((totalValue >= conditionEvent.Count) != (conditionEvent.Value != 0))
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        }
                        case ConditionEvent.ConditionType.Skill:
                        {
                            var skill = game.CurrentPartyMember.Skills[(Skill)conditionEvent.ObjectIndex];
                            var totalValue = skill.CurrentValue + skill.BonusValue;
                            if ((totalValue >= conditionEvent.Count) != (conditionEvent.Value != 0))
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        }
                    }

                    // For some follow-up events we won't proceed by using Eye, Hand or Mouth.
                    if (conversationPartner == null && conditionEvent.Next != null &&
                        trigger != EventTrigger.Move && trigger != EventTrigger.Always &&
                        conditionEvent.TypeOfCondition != ConditionEvent.ConditionType.Hand &&
                        conditionEvent.TypeOfCondition != ConditionEvent.ConditionType.Eye &&
                        conditionEvent.TypeOfCondition != ConditionEvent.ConditionType.Mouth &&
                        conditionEvent.TypeOfCondition != ConditionEvent.ConditionType.UseItem &&
                        conditionEvent.TypeOfCondition != ConditionEvent.ConditionType.Levitating &&
                        conditionEvent.TypeOfCondition != ConditionEvent.ConditionType.EnterNumber &&
                        conditionEvent.TypeOfCondition != ConditionEvent.ConditionType.SayWord &&
                        conditionEvent.TypeOfCondition != ConditionEvent.ConditionType.LastEventResult)
                    {
                        var next = conditionEvent.Next;

                        while (next != null && (next.Type == EventType.Condition ||
                            next.Type == EventType.Action || next.Type == EventType.Reward ||
                            next.Type == EventType.ChangeMusic || next.Type == EventType.Dice100Roll))
                            next = next.Next;

                        if (next != null)
                        {
                            switch (next.Type)
                            {
                                case EventType.Teleport:
                                case EventType.StartBattle:
                                case EventType.EnterPlace:
                                case EventType.Riddlemouth:
                                    aborted = true;
                                    return null;
                            }
                        }
                    }

                    if (conditionEvent.Next == null || !(conditionEvent.Next is ConditionEvent followEvent) ||
                        (followEvent.TypeOfCondition != ConditionEvent.ConditionType.Eye &&
                         followEvent.TypeOfCondition != ConditionEvent.ConditionType.Hand &&
                         followEvent.TypeOfCondition != ConditionEvent.ConditionType.Mouth))
                        trigger = EventTrigger.Always; // following events should not dependent on the trigger anymore in that case

                    break;
                }
                case EventType.Action:
                {
                    if (!(@event is ActionEvent actionEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid action event.");

                    bool ClearSetToggle(Func<bool> currentValueRetriever)
                    {
                        switch (actionEvent.Value)
                        {
                            case 0: // Clear
                                return false;
                            case 1: // Set
                                return true;
                            case 2: // Toggle
                                return !currentValueRetriever();
                            default: // Leave as it is
                                return currentValueRetriever();
                        }
                    }

                    switch (actionEvent.TypeOfAction)
                    {
                        case ActionEvent.ActionType.SetGlobalVariable:
                            game.CurrentSavegame.SetGlobalVariable(actionEvent.ObjectIndex,
                                ClearSetToggle(() => game.CurrentSavegame.GetGlobalVariable(actionEvent.ObjectIndex)));
                            break;
                        case ActionEvent.ActionType.SetEventBit:
                        {
                            var mapIndex = 1 + (actionEvent.ObjectIndex >> 6);
                            var eventIndex = actionEvent.ObjectIndex & 0x3f;
                            game.SetMapEventBit(mapIndex, eventIndex,
                                ClearSetToggle(() => game.CurrentSavegame.GetEventBit(mapIndex, eventIndex)));
                            break;
                        }
                        case ActionEvent.ActionType.LockDoor:
                            if (ClearSetToggle(() => !game.CurrentSavegame.IsDoorLocked(actionEvent.ObjectIndex)))
                                game.CurrentSavegame.UnlockDoor(actionEvent.ObjectIndex);
                            else
                                game.CurrentSavegame.LockDoor(actionEvent.ObjectIndex);
                            break;
                        case ActionEvent.ActionType.LockChest:
                            if (ClearSetToggle(() => !game.CurrentSavegame.IsChestLocked(actionEvent.ObjectIndex)))
                                game.CurrentSavegame.UnlockChest(actionEvent.ObjectIndex);
                            else
                                game.CurrentSavegame.LockChest(actionEvent.ObjectIndex);
                            break;
                        case ActionEvent.ActionType.SetCharacterBit:
                        {
                            var mapIndex = 1 + (actionEvent.ObjectIndex >> 5);
                            var eventIndex = actionEvent.ObjectIndex & 0x1f;
                            game.SetMapCharacterBit(mapIndex, eventIndex,
                                ClearSetToggle(() => game.CurrentSavegame.GetCharacterBit(mapIndex, eventIndex)));
                            break;
                        }
                        case ActionEvent.ActionType.AddItem:
                        {
                            // Note: This is never used in Ambermoon with value 1. So it always removes items only.
                            // Giving items is either done through a chest event or the conversation create event.
                            var itemIndex = actionEvent.ObjectIndex;
                            if (itemIndex > 0)
                            {
                                if (actionEvent.Value == 1) // Add
                                {
                                    int numberToAdd = Math.Max(1, (int)actionEvent.Count);
                                    foreach (var partyMember in game.PartyMembers)
                                    {
                                        numberToAdd = game.DropItem(partyMember, itemIndex, numberToAdd);

                                        if (numberToAdd == 0)
                                            break;
                                    }
                                    // Ignore the rest as we couldn't do anything about it.
                                }
                                else if (actionEvent.Value == 0) // Remove
                                {
                                    int numberToRemove = (int)Math.Max(1, actionEvent.Count);
                                    // Prefer inventory
                                    foreach (var partyMember in game.PartyMembers)
                                    {
                                        foreach (var slot in partyMember.Inventory.Slots)
                                        {
                                            if (slot?.ItemIndex == itemIndex)
                                            {
                                                int slotCount = slot.Amount;
                                                slot.Remove(Math.Min(numberToRemove, slotCount));
                                                int numRemoved = slotCount - slot.Amount;
                                                game.InventoryItemRemoved(itemIndex, numRemoved, partyMember);
                                                numberToRemove -= numRemoved;

                                                if (numberToRemove == 0)
                                                    break;
                                            }
                                        }

                                        if (numberToRemove == 0)
                                            break;
                                    }
                                    foreach (var partyMember in game.PartyMembers)
                                    {
                                        foreach (var slot in partyMember.Equipment.Slots.Values)
                                        {
                                            if (slot?.ItemIndex == itemIndex)
                                            {
                                                int slotCount = slot.Amount;
                                                bool cursed = slot.Flags.HasFlag(ItemSlotFlags.Cursed);
                                                slot.Remove(Math.Min(numberToRemove, slotCount));
                                                int numRemoved = slotCount - slot.Amount;
                                                game.EquipmentRemoved(partyMember,itemIndex, numRemoved, cursed);
                                                numberToRemove -= numRemoved;

                                                if (numberToRemove == 0)
                                                    break;
                                            }
                                        }

                                        if (numberToRemove == 0)
                                            break;
                                    }
                                }
                            }
                            break;
                        }
                        case ActionEvent.ActionType.AddKeyword:
                            // Note: This may also remove a keyword but this is no real use case.
                            // We will only add keywords here and ignore the action value.
                            // The original code seems to do the same.
                            game.CurrentSavegame.AddDictionaryWord(actionEvent.ObjectIndex);
                            break;
                        case ActionEvent.ActionType.SetGameOption:
                        {
                            var option = (Data.Enumerations.Option)(1 << (int)actionEvent.ObjectIndex);
                            game.CurrentSavegame.SetGameOption(option, ClearSetToggle(() => game.CurrentSavegame.IsGameOptionActive(option)));
                            break;
                        }
                        case ActionEvent.ActionType.SetDirection:
                        {
                            game.SetPlayerDirection((CharacterDirection)(actionEvent.ObjectIndex % 5));
                            break;
                        }
                        case ActionEvent.ActionType.AddCondition:
                        {
                            var condition = (Condition)(1 << (int)actionEvent.ObjectIndex);
                            if (ClearSetToggle(() => game.CurrentPartyMember.Conditions.HasFlag(condition)))
                                game.AddCondition(condition);
                            else
                                game.RemoveCondition(condition, game.CurrentPartyMember);
                            break;
                        }
                        case ActionEvent.ActionType.AddGold:
                            // Note: The original code always removes the gold. But as this is not used
                            // at all I think controlling the behavior with the value bit is better.
                            if (actionEvent.Value == 1) // Add
                                game.DistributeGold(actionEvent.ObjectIndex, true);
                            else if (actionEvent.Value == 0) // Remove
                            {
                                var partyMembers = game.PartyMembers.ToArray();
                                int totalGoldToRemove = (int)Math.Min(actionEvent.ObjectIndex, partyMembers.Sum(p => p.Gold));
                                int goldToRemovePerPlayer = totalGoldToRemove / partyMembers.Length;
                                int singleGoldMemberCount = totalGoldToRemove % partyMembers.Length;

                                foreach (var partyMember in partyMembers)
                                {
                                    int goldToRemove = goldToRemovePerPlayer;

                                    if (singleGoldMemberCount != 0)
                                        ++goldToRemove;

                                    int removeAmount = Math.Min(goldToRemove, partyMember.Gold);

                                    if (removeAmount == goldToRemove)
                                        --singleGoldMemberCount;

                                    partyMember.Gold = (ushort)Math.Max(0, partyMember.Gold - removeAmount);
                                    partyMember.TotalWeight -= (uint)removeAmount * Character.GoldWeight;
                                    totalGoldToRemove -= removeAmount;
                                }

                                if (totalGoldToRemove != 0)
                                {
                                    foreach (var partyMember in partyMembers)
                                    {
                                        if (partyMember.Gold != 0)
                                        {
                                            int removeAmount = Math.Min(totalGoldToRemove, partyMember.Gold);
                                            partyMember.Gold = (ushort)Math.Max(0, partyMember.Gold - removeAmount);
                                            partyMember.TotalWeight -= (uint)removeAmount * Character.GoldWeight;
                                            totalGoldToRemove -= removeAmount;
                                        }
                                    }
                                }
                            }
                            break;
                        case ActionEvent.ActionType.AddFood:
                            // Note: The original code always removes the food. But as this is not used
                            // at all I think controlling the behavior with the value bit is better.
                            if (actionEvent.Value == 1) // Add
                                game.DistributeFood(actionEvent.ObjectIndex, true);
                            else if (actionEvent.Value == 0) // Remove
                            {
                                var partyMembers = game.PartyMembers.ToArray();
                                int totalFoodToRemove = (int)Math.Min(actionEvent.ObjectIndex, partyMembers.Sum(p => p.Food));
                                int foodToRemovePerPlayer = totalFoodToRemove / partyMembers.Length;
                                int singleFoodMemberCount = totalFoodToRemove % partyMembers.Length;

                                foreach (var partyMember in partyMembers)
                                {
                                    int foodToRemove = foodToRemovePerPlayer;

                                    if (singleFoodMemberCount != 0)
                                        ++foodToRemove;

                                    int removeAmount = Math.Min(foodToRemove, partyMember.Food);

                                    if (removeAmount == foodToRemove)
                                        --singleFoodMemberCount;

                                    partyMember.Food = (ushort)Math.Max(0, partyMember.Food - removeAmount);
                                    partyMember.TotalWeight -= (uint)removeAmount * Character.FoodWeight;
                                    totalFoodToRemove -= removeAmount;
                                }

                                if (totalFoodToRemove != 0)
                                {
                                    foreach (var partyMember in partyMembers)
                                    {
                                        if (partyMember.Food != 0)
                                        {
                                            int removeAmount = Math.Min(totalFoodToRemove, partyMember.Food);
                                            partyMember.Food = (ushort)Math.Max(0, partyMember.Food - removeAmount);
                                            partyMember.TotalWeight -= (uint)removeAmount * Character.FoodWeight;
                                            totalFoodToRemove -= removeAmount;
                                        }
                                    }
                                }
                            }
                            break;
                    }

                    break;
                }
                case EventType.Dice100Roll:
                {
                    if (!(@event is Dice100RollEvent diceEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid dice 100 event.");

                    var mapEventIfFalse = diceEvent.ContinueIfFalseWithMapEventIndex == 0xffff
                        ? null : events[(int)diceEvent.ContinueIfFalseWithMapEventIndex];
                    lastEventStatus = game.RollDice100() < diceEvent.Chance;
                    return lastEventStatus ? diceEvent.Next : mapEventIfFalse;
                }
                case EventType.Conversation:
                {
                    if (!(@event is ConversationEvent conversationEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid conversation event.");

                    switch (conversationEvent.Interaction)
                    {
                        case ConversationEvent.InteractionType.Talk:
                            if (trigger != EventTrigger.Mouth)
                            {
                                aborted = true;
                                return null;
                            }
                            game.ShowConversation(conversationPartner, characterIndex, conversationEvent, new Game.ConversationItems());
                            return null;
                        default:
                            // Note: this is handled by the conversation window.
                            // It should never appear inside a running event chain.
                            aborted = true;
                            return null;
                    }
                }
                case EventType.PrintText:
                case EventType.Create:
                case EventType.Exit:
                case EventType.Interact:
                {
                    // Note: These are only used by conversations and are handled in
                    // game.ShowConversation. So we don't need to do anything here.
                    throw new AmbermoonException(ExceptionScope.Data, "Conversation events must not be called outside of conversations.");
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
                                x, y, @event.Next, true);
                        }
                        else // Close and No have the same meaning here
                        {
                            if (decisionEvent.NoEventIndex != 0xffff)
                            {
                                map.TriggerEventChain(game, EventTrigger.Always,
                                    x, y, events[(int)decisionEvent.NoEventIndex], false);
                            }
                        }
                    });
                    return null; // next event is only executed after popup response
                }
                case EventType.ChangeMusic:
                    if (!(@event is ChangeMusicEvent changeMusicEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid change music event.");
                    game.PlayMusic((Data.Enumerations.Song)changeMusicEvent.MusicIndex);
                    break;
                case EventType.Spawn:
                    if (!(@event is SpawnEvent spawnEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid spawn event.");
                    game.SpawnTransport(spawnEvent.MapIndex == 0 ? map.Index : spawnEvent.MapIndex,
                        spawnEvent.X, spawnEvent.Y, spawnEvent.TravelType);
                    lastEventStatus = true;
                    break;
                case EventType.Unknown:
                    // TODO
                    break;
                default:
                    Console.WriteLine($"Unknown event type found: {@event.Type}");
                    return @event.Next;
            }

            return @event.Next;
        }

        public static bool TriggerEventChain(this Map map, Game game, EventTrigger trigger, uint x, uint y,
            Event firstMapEvent, bool lastEventStatus = false)
        {
            var mapEvent = firstMapEvent;

            while (mapEvent != null)
            {
                mapEvent = mapEvent.ExecuteEvent(map, game, ref trigger, x, y, ref lastEventStatus, out bool aborted, out var _);

                if (aborted)
                    return false;
            }

            return true;
        }

        public static Event GetSecondaryBranchSuccessor(this Event @event, List<Event> events)
        {
            uint nextEventIndex = 0xffff;

            if (@event is ConditionEvent conditionEvent)
                nextEventIndex = conditionEvent.ContinueIfFalseWithMapEventIndex;
            else if (@event is Dice100RollEvent dice100RollEvent)
                nextEventIndex = dice100RollEvent.ContinueIfFalseWithMapEventIndex;
            else if (@event is DoorEvent doorEvent)
                nextEventIndex = doorEvent.UnlockFailedEventIndex;
            else if (@event is ChestEvent chestEvent)
                nextEventIndex = chestEvent.UnlockFailedEventIndex;
            else if (@event is DecisionEvent decisionEvent)
                nextEventIndex = decisionEvent.NoEventIndex;

            return nextEventIndex == 0xffff ? null : events[(int)nextEventIndex];
        }
    }
}
