using Ambermoon.Data.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.Serialization
{
    public static class EventWriter
    {
        public static void WriteEvents(IDataWriter dataWriter,
            List<Event> events, List<Event> eventList)
        {
            dataWriter.Write((ushort)eventList.Count);
            
            foreach (var @event in eventList)
            {
                dataWriter.Write((ushort)events.IndexOf(@event));
            }

            dataWriter.Write((ushort)events.Count);

            foreach (var @event in events)
            {
                dataWriter.WriteEnumAsByte(@event.Type);
                WriteEventData(dataWriter, @event);
                dataWriter.Write((ushort)(@event.Next == null ? 0xffff : events.IndexOf(@event.Next)));
            }
        }

        /// <summary>
        /// Note that this only writes the 9 event data bytes. It assumes that the event type
        /// byte is writter already and it won't write the next event word.
        /// </summary>
        public static void WriteEventData(IDataWriter dataWriter, Event @event)
        {
            switch (@event.Type)
            {
                case EventType.Teleport:
                {
                    // 1. byte is the x coordinate
                    // 2. byte is the y coordinate
                    // 3. byte is the character direction
                    // Then 1 byte for the new travel type (0xff means keep the same)
                    // Then 1 byte for the transtion type (0-5)
                    // Then a word for the map index
                    // Then 2 unknown bytes (seem to be 00 FF)
                    var teleportEvent = @event as TeleportEvent;
                    dataWriter.Write((byte)teleportEvent.X);
                    dataWriter.Write((byte)teleportEvent.Y);
                    dataWriter.WriteEnumAsByte(teleportEvent.Direction);
                    dataWriter.Write(teleportEvent.NewTravelType == null ? (byte)0xff : (byte)teleportEvent.NewTravelType.Value);
                    dataWriter.Write((byte)teleportEvent.Transition);
                    dataWriter.Write((ushort)teleportEvent.MapIndex);
                    dataWriter.Write(teleportEvent.Unknown2);
                    break;
                }
                case EventType.Door:
                {
                    // 1. byte is a lockpicking chance reduction (0: already open, 100: can't open via lockpicking)
                    // 2. byte is the door index (used for unlock bits in savegame)
                    // 3. byte is an optional text index (0xff = no text)
                    // 4. byte is unknown
                    // 5. byte is unknown
                    // word at position 6 is the key index if a key must unlock it
                    // last word is the event index (0-based) of the event that is called when unlocking fails
                    var doorEvent = @event as DoorEvent;
                    dataWriter.Write((byte)doorEvent.LockpickingChanceReduction);
                    dataWriter.Write(doorEvent.DoorIndex);
                    dataWriter.Write((byte)doorEvent.TextIndex);
                    dataWriter.Write((byte)doorEvent.UnlockTextIndex);
                    dataWriter.Write(doorEvent.Unused);
                    dataWriter.Write((ushort)doorEvent.KeyIndex);
                    dataWriter.Write((ushort)doorEvent.UnlockFailedEventIndex);
                    break;
                }
                case EventType.Chest:
                {
                    // 1. byte is a lockpicking chance reduction (0: already open, 100: can't open via lockpicking)
                    // 2. byte is a search chance reduction (0: always found)
                    // 3. byte is an optional text index (0xff = no text)
                    // 4. byte is the chest index (0-based)
                    // 5. byte are the chest flags
                    // word at position 6 is the key index if a key must unlock it
                    // last word is the event index (0-based) of the event that is called when unlocking fails
                    var chestEvent = @event as ChestEvent;
                    dataWriter.Write((byte)chestEvent.LockpickingChanceReduction);
                    dataWriter.Write(chestEvent.FindChanceReduction);
                    dataWriter.Write((byte)chestEvent.TextIndex);
                    dataWriter.Write((byte)chestEvent.ChestIndex);
                    dataWriter.Write((byte)chestEvent.Flags);
                    dataWriter.Write((ushort)chestEvent.KeyIndex);
                    dataWriter.Write((ushort)chestEvent.UnlockFailedEventIndex);
                    break;
                }
                case EventType.MapText:
                {
                    // event image index (0xff = no image)
                    // trigger (1 = move, 2 = cursor, 3 = both)
                    // unknown boolean
                    // map text index as word
                    // 4 unknown bytes
                    var textEvent = @event as PopupTextEvent;
                    dataWriter.Write((byte)textEvent.EventImageIndex);
                    dataWriter.WriteEnumAsByte(textEvent.PopupTrigger);
                    dataWriter.Write((byte)(textEvent.TriggerIfBlind ? 1 : 0));
                    dataWriter.Write((byte)0);
                    dataWriter.Write((byte)textEvent.TextIndex);
                    dataWriter.Write(textEvent.Unknown);
                    break;
                }
                case EventType.Spinner:
                {
                    var spinnerEvent = @event as SpinnerEvent;
                    dataWriter.WriteEnumAsByte(spinnerEvent.Direction);
                    dataWriter.Write(spinnerEvent.Unused);
                    break;
                }
                case EventType.Trap:
                {
                    var trapEvent = @event as TrapEvent;
                    dataWriter.WriteEnumAsByte(trapEvent.Ailment);
                    dataWriter.WriteEnumAsByte(trapEvent.Target);
                    dataWriter.WriteEnumAsByte(trapEvent.AffectedGenders);
                    dataWriter.Write(trapEvent.BaseDamage);
                    dataWriter.Write(trapEvent.Unused);
                    break;
                }
                case EventType.ChangeBuffs:
                {
                    var removeBuffsEvent = @event as ChangeBuffsEvent;
                    dataWriter.Write(removeBuffsEvent.AffectedBuff == null ? (byte)0 : (byte)(1 + removeBuffsEvent.AffectedBuff.Value));
                    dataWriter.Write(removeBuffsEvent.Add ? (byte)1 : (byte)0);
                    dataWriter.Write(removeBuffsEvent.Unused1);
                    dataWriter.Write(removeBuffsEvent.Value);
                    dataWriter.Write(removeBuffsEvent.Duration);
                    dataWriter.Write(removeBuffsEvent.Unused2);
                    break;
                }
                case EventType.Riddlemouth:
                {
                    var riddleMouthEvent = @event as RiddlemouthEvent;
                    dataWriter.Write((byte)riddleMouthEvent.RiddleTextIndex);
                    dataWriter.Write((byte)riddleMouthEvent.SolutionTextIndex);
                    dataWriter.Write(riddleMouthEvent.Unused);
                    dataWriter.Write((ushort)riddleMouthEvent.CorrectAnswerDictionaryIndex1);
                    dataWriter.Write((ushort)riddleMouthEvent.CorrectAnswerDictionaryIndex2);
                    break;
                }
                case EventType.Reward:
                {
                    var rewardEvent = @event as RewardEvent;
                    dataWriter.WriteEnumAsByte(rewardEvent.TypeOfReward);
                    dataWriter.WriteEnumAsByte(rewardEvent.Operation);
                    dataWriter.Write((byte)(rewardEvent.Random ? 1 : 0));
                    dataWriter.WriteEnumAsByte(rewardEvent.Target);
                    dataWriter.Write(rewardEvent.Unused);
                    dataWriter.Write(rewardEvent.RewardTypeValue);
                    dataWriter.Write((ushort)rewardEvent.Value);
                    break;
                }
                case EventType.ChangeTile:
                {
                    var changeTileEvent = @event as ChangeTileEvent;
                    dataWriter.Write((byte)changeTileEvent.X);
                    dataWriter.Write((byte)changeTileEvent.Y);
                    dataWriter.Write(changeTileEvent.Unknown);
                    dataWriter.Write((ushort)changeTileEvent.FrontTileIndex);
                    dataWriter.Write((ushort)changeTileEvent.MapIndex);
                    break;
                }
                case EventType.StartBattle:
                {
                    var startBattleEvent = @event as StartBattleEvent;
                    dataWriter.Write(startBattleEvent.Unknown1);
                    dataWriter.Write((byte)startBattleEvent.MonsterGroupIndex);
                    dataWriter.Write(startBattleEvent.Unknown2);
                    break;
                }
                case EventType.EnterPlace:
                {
                    var enterPlaceEvent = @event as EnterPlaceEvent;
                    dataWriter.Write(enterPlaceEvent.ClosedTextIndex);
                    dataWriter.WriteEnumAsByte(enterPlaceEvent.PlaceType);
                    dataWriter.Write(enterPlaceEvent.OpeningHour);
                    dataWriter.Write(enterPlaceEvent.ClosingHour);
                    dataWriter.Write(enterPlaceEvent.UsePlaceTextIndex);
                    dataWriter.Write((ushort)enterPlaceEvent.PlaceIndex);
                    dataWriter.Write((ushort)enterPlaceEvent.MerchantDataIndex);
                    break;
                }
                case EventType.Condition:
                {
                    var conditionEvent = @event as ConditionEvent;
                    dataWriter.WriteEnumAsByte(conditionEvent.TypeOfCondition);
                    dataWriter.Write((byte)conditionEvent.Value);
                    dataWriter.Write((byte)conditionEvent.Count);
                    dataWriter.Write((ushort)conditionEvent.DisallowedAilments);
                    dataWriter.Write((ushort)conditionEvent.ObjectIndex);
                    dataWriter.Write((ushort)conditionEvent.ContinueIfFalseWithMapEventIndex);
                    break;
                }
                case EventType.Action:
                {
                    var actionEvent = @event as ActionEvent;
                    dataWriter.WriteEnumAsByte(actionEvent.TypeOfAction);
                    dataWriter.Write((byte)actionEvent.Value);
                    dataWriter.Write((byte)actionEvent.Count);
                    dataWriter.Write(actionEvent.Unknown1);
                    dataWriter.Write((ushort)actionEvent.ObjectIndex);
                    dataWriter.Write(actionEvent.Unknown2);
                    break;
                }
                case EventType.Dice100Roll:
                {
                    var dice100Event = @event as Dice100RollEvent;
                    dataWriter.Write((byte)dice100Event.Chance);
                    dataWriter.Write(dice100Event.Unused);
                    dataWriter.Write((ushort)dice100Event.ContinueIfFalseWithMapEventIndex);
                    break;
                }
                case EventType.Conversation:
                {
                    var conversationEvent = @event as ConversationEvent;
                    dataWriter.WriteEnumAsByte(conversationEvent.Interaction);
                    dataWriter.Write(conversationEvent.Unused1);
                    dataWriter.Write(conversationEvent.Value);
                    dataWriter.Write(conversationEvent.Unused2);
                    break;
                }
                case EventType.PrintText:
                {
                    var printTextEvent = @event as PrintTextEvent;
                    dataWriter.Write((byte)printTextEvent.NPCTextIndex);
                    dataWriter.Write(printTextEvent.Unused);
                    break;
                }
                case EventType.Create:
                {
                    var createEvent = @event as CreateEvent;
                    dataWriter.WriteEnumAsByte(createEvent.TypeOfCreation);
                    dataWriter.Write(createEvent.Unused);
                    dataWriter.Write((ushort)createEvent.Amount);
                    dataWriter.Write((ushort)createEvent.ItemIndex);
                    break;
                }
                case EventType.Decision:
                {
                    var decisionEvent = @event as DecisionEvent;
                    dataWriter.Write((byte)decisionEvent.TextIndex);
                    dataWriter.Write(decisionEvent.Unknown1);
                    dataWriter.Write((ushort)decisionEvent.NoEventIndex);
                    break;
                }
                case EventType.ChangeMusic:
                {
                    var musicEvent = @event as ChangeMusicEvent;
                    dataWriter.Write((ushort)musicEvent.MusicIndex);
                    dataWriter.Write(musicEvent.Volume);
                    dataWriter.Write(musicEvent.Unknown1);
                    break;
                }
                case EventType.Exit:
                {
                    var exitEvent = @event as ExitEvent;
                    dataWriter.Write(exitEvent.Unused);
                    break;
                }
                case EventType.Spawn:
                {
                    var spawnEvent = @event as SpawnEvent;
                    dataWriter.Write((byte)spawnEvent.X);
                    dataWriter.Write((byte)spawnEvent.Y);
                    dataWriter.WriteEnumAsByte(spawnEvent.TravelType);
                    dataWriter.Write(spawnEvent.Unknown1);
                    dataWriter.Write((ushort)spawnEvent.MapIndex);
                    dataWriter.Write(spawnEvent.Unknown2);
                    break;
                }
                case EventType.Interact:
                {
                    var interactEvent = @event as InteractEvent;
                    dataWriter.Write(interactEvent.Unused);
                    break;
                }
                case EventType.RemovePartyMember:
                {
                    var removePartyMemberEvent = @event as RemovePartyMemberEvent;
                    dataWriter.Write(removePartyMemberEvent.CharacterIndex);
                    dataWriter.Write(removePartyMemberEvent.ChestIndexEquipment);
                    dataWriter.Write(removePartyMemberEvent.ChestIndexInventory);
                    dataWriter.Write(removePartyMemberEvent.Unused);
                    break;
                }
                case EventType.Delay:
                {
                    var delayEvent = @event as DelayEvent;
                    dataWriter.Write(delayEvent.Unused1);
                    dataWriter.Write((ushort)delayEvent.Milliseconds);
                    dataWriter.Write(delayEvent.Unused2);
                    break;
                }
				case EventType.PartyMemberCondition:
				{
					var conditionEvent = @event as PartyMemberConditionEvent;
					dataWriter.WriteEnumAsByte(conditionEvent.TypeOfCondition);
					dataWriter.Write((byte)conditionEvent.ConditionValueIndex);
					dataWriter.Write((byte)conditionEvent.Target);
					dataWriter.Write((ushort)conditionEvent.DisallowedAilments);
					dataWriter.Write((ushort)conditionEvent.Value);
					dataWriter.Write((ushort)conditionEvent.ContinueIfFalseWithMapEventIndex);
					break;
				}
                case EventType.Shake:
                {
                    var shakeEvent = @event as ShakeEvent;
                    dataWriter.Write(shakeEvent.Unused1);
                    dataWriter.Write((ushort)shakeEvent.Shakes);
                    dataWriter.Write(shakeEvent.Unused2);
                    break;
                }
                case EventType.ShowMap:
                {
                    var showMapEvent = @event as ShowMapEvent;
                    dataWriter.WriteEnumAsByte(showMapEvent.Options);
                    dataWriter.Write(showMapEvent.Unused);
                    break;
                }
                case EventType.ToggleSwitch:
                {
                    var toggleSwitchEvent = @event as ToggleSwitchEvent;
                    dataWriter.Write(toggleSwitchEvent.GlobalVariableBytes);
                    dataWriter.Write((ushort)toggleSwitchEvent.FrontTileIndexOff);
                    dataWriter.Write((ushort)toggleSwitchEvent.FrontTileIndexOn);
                    break;
                }
                default:
                {
                    var debugEvent = @event as DebugEvent;
                    dataWriter.Write(debugEvent.Data);
                    break;
                }
            }
        }
    }
}
