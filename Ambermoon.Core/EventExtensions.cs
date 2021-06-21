using Ambermoon.Data;
using System;
using System.Linq;

namespace Ambermoon
{
    internal static class EventExtensions
    {
        public static Event ExecuteEvent(this Event @event, Map map, Game game,
            ref EventTrigger trigger, uint x, uint y, uint ticks, ref bool lastEventStatus,
            out bool aborted, IConversationPartner conversationPartner = null)
        {
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
                        return null;

                    if (!(@event is TeleportEvent teleportEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid teleport event.");

                    game.Teleport(teleportEvent);
                    break;
                }
                case EventType.Door:
                {
                    if (!(@event is DoorEvent doorEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid door event.");

                    if (!game.ShowDoor(doorEvent, false, false, map))
                    {
                        // already unlocked
                        lastEventStatus = true;
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

                    if (chestEvent.Flags.HasFlag(ChestEvent.ChestFlags.SearchSkillCheck) &&
                        game.RandomInt(0, 99) >= game.CurrentPartyMember.Abilities[Ability.Searching].TotalCurrentValue)
                    {
                        aborted = true;
                        return null;
                    }

                    game.ShowChest(chestEvent, false, false, map);
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

                    game.TriggerTrap(trapEvent);
                    return null; // next event is only executed after trap effect
                }
                case EventType.RemoveBuffs:
                {
                    if (!(@event is RemoveBuffsEvent removeBuffsEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid remove buffs event.");

                    if (removeBuffsEvent.AffectedBuff == null) // all
                    {
                        for (int i = 0; i < 6; ++i)
                        {
                            game.CurrentSavegame.ActiveSpells[i] = null;
                        }

                        game.UpdateLight();
                    }
                    else
                    {
                        int index = (int)removeBuffsEvent.AffectedBuff;

                        if (index < 6)
                        {
                            game.CurrentSavegame.ActiveSpells[index] = null;

                            if (index == (int)Data.Enumerations.ActiveSpellType.Light)
                                game.UpdateLight();
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
                        map.TriggerEventChain(game, EventTrigger.Always,
                            x, y, game.CurrentTicks, @event.Next, true);
                    });
                    return null; // next event is only executed after popup response
                }
                case EventType.Award:
                {
                    if (!(@event is AwardEvent awardEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid award event.");
                    void Award(PartyMember partyMember, Action followAction) => game.AwardPlayer(partyMember, awardEvent, followAction);
                    void Done()
                    {
                        if (awardEvent.Next != null)
                            TriggerEventChain(map, game, EventTrigger.Always, x, y, game.CurrentTicks, awardEvent.Next, true);
                    }
                    switch (awardEvent.Target)
                    {
                        case AwardEvent.AwardTarget.ActivePlayer:
                            Award(game.CurrentPartyMember, Done);
                            break;
                        case AwardEvent.AwardTarget.All:
                            if (awardEvent.TypeOfAward == AwardEvent.AwardType.HitPoints &&
                                (awardEvent.Operation == AwardEvent.AwardOperation.Decrease ||
                                awardEvent.Operation == AwardEvent.AwardOperation.DecreasePercentage))
                            {
                                Func<PartyMember, uint> damageProvider = awardEvent.Operation == AwardEvent.AwardOperation.Decrease
                                    ? (Func<PartyMember, uint>)(_ => awardEvent.Value) : p => awardEvent.Value * p.HitPoints.TotalMaxValue / 100;

                                // Note: Awards damage silently.
                                game.DamageAllPartyMembers(damageProvider, p => p.Alive, null, Done, Ailment.None, false);
                            }
                            else
                            {
                                game.ForeachPartyMember(Award, p => p.Alive, Done);
                            }
                            break;
                        default:
                            return awardEvent.Next;
                    }
                    return null;
                }
                case EventType.ChangeTile:
                {
                    if (!(@event is ChangeTileEvent changeTileEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid chest event.");

                    game.UpdateMapTile(changeTileEvent, x, y);

                    // Add it to the savegame as well.
                    // Note: Savegame stores the front tile index for 2D and wall/object index for 3D.
                    // Note: If map index is 0 (same map) we have to replace it with the real map index
                    // for savegames. Otherwise it will be interpreted as "end of tile changes marker".
                    if (changeTileEvent.MapIndex == 0)
                        changeTileEvent.MapIndex = map.Index;
                    if (changeTileEvent.X == 0)
                        changeTileEvent.X = x + 1;
                    if (changeTileEvent.Y == 0)
                        changeTileEvent.Y = y + 1;
                    game.CurrentSavegame.TileChangeEvents.SafeAdd(map.Index, changeTileEvent);
                    // Change tile events that are triggered directly should be disabled afterwards
                    int eventIndex = map.EventList.IndexOf(@event);
                    if (eventIndex != -1)
                        game.CurrentSavegame.ActivateEvent(map.Index, (uint)eventIndex, false);
                    break;
                }
                case EventType.StartBattle:
                {
                    if (!(@event is StartBattleEvent battleEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid battle event.");

                    game.StartBattle(battleEvent, battleEvent.Next, game.GetCombatBackgroundIndex(map, x, y));
                    return null;
                }
                case EventType.EnterPlace:
                {
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

                            if (conditionEvent.Value == 0 && totalCount != 0)
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            else if (conditionEvent.Value == 1 && totalCount == 0 || totalCount < conditionEvent.Count)
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
                        case ConditionEvent.ConditionType.HasAilment:
                            if (game.CurrentPartyMember.Ailments.HasFlag((Ailment)(1 << (int)conditionEvent.ObjectIndex)) != (conditionEvent.Value != 0))
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
                        case ConditionEvent.ConditionType.SayWord:
                        {
                            if (trigger != EventTrigger.Mouth)
                            {
                                aborted = true;
                                return null;
                            }
                            game.SayWord(map, x, y, events, conditionEvent);
                            return null;
                        }
                        case ConditionEvent.ConditionType.EnterNumber:
                        {
                            game.EnterNumber(map, x, y, events, conditionEvent);
                            return null;
                        }
                        case ConditionEvent.ConditionType.Levitating:
                            if (trigger != EventTrigger.Levitating)
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
                            if (trigger != EventTrigger.Eye)
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                    }

                    trigger = EventTrigger.Always; // following events should not dependent on the trigger anymore

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
                            if (!ClearSetToggle(() => !game.CurrentSavegame.IsDoorLocked(actionEvent.ObjectIndex)))
                                game.CurrentSavegame.UnlockDoor(actionEvent.ObjectIndex);
                            else
                                game.CurrentSavegame.LockDoor(actionEvent.ObjectIndex);
                            break;
                        case ActionEvent.ActionType.LockChest:
                            if (!ClearSetToggle(() => !game.CurrentSavegame.IsChestLocked(actionEvent.ObjectIndex)))
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
                        case ActionEvent.ActionType.AddAilment:
                        {
                            var ailment = (Ailment)(1 << (int)actionEvent.ObjectIndex);
                            if (ClearSetToggle(() => game.CurrentPartyMember.Ailments.HasFlag(ailment)))
                                game.AddAilment(ailment);
                            else
                                game.RemoveAilment(ailment, game.CurrentPartyMember);
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
                            game.ShowConversation(conversationPartner, conversationEvent, new Game.ConversationItems());
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
                    // This should never be executed via this extension.
                    throw new AmbermoonException(ExceptionScope.Application, $"Events of type {@event.Type} should be handled by the conversation window.");
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
                                    x, y, game.CurrentTicks, events[(int)decisionEvent.NoEventIndex], false);
                            }
                        }
                    });
                    return null; // next event is only executed after popup response
                }
                case EventType.ChangeMusic:
                    if (!(@event is ChangeMusicEvent changeMusicEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid change music event.");
                    game.PlayMusic((Data.Enumerations.Song)changeMusicEvent.MusicIndex);
                    return @event.Next;
                case EventType.Spawn:
                    if (!(@event is SpawnEvent spawnEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid spawn event.");
                    game.SpawnTransport(spawnEvent.MapIndex == 0 ? map.Index : spawnEvent.MapIndex,
                        spawnEvent.X, spawnEvent.Y, spawnEvent.TravelType);
                    return @event.Next;
                case EventType.Unknown:
                    // TODO
                    return @event.Next;
                default:
                    Console.WriteLine($"Unknown event type found: {@event.Type}");
                    return @event.Next;
            }

            lastEventStatus = true;

            return @event.Next;
        }

        public static bool TriggerEventChain(this Map map, Game game, EventTrigger trigger, uint x, uint y,
            uint ticks, Event firstMapEvent, bool lastEventStatus = false)
        {
            var mapEvent = firstMapEvent;

            while (mapEvent != null)
            {
                mapEvent = mapEvent.ExecuteEvent(map, game, ref trigger, x, y, ticks, ref lastEventStatus, out bool aborted);

                if (aborted)
                    return false;
            }

            return true;
        }
    }
}
