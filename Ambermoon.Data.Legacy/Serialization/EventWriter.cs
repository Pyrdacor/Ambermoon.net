using Ambermoon.Data.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.Serialization
{
    internal class EventWriter
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
                SaveEvent(dataWriter, @event);
                dataWriter.Write((ushort)(@event.Next == null ? 0xffff : events.IndexOf(@event.Next)));
            }
        }

        static void SaveEvent(IDataWriter dataWriter, Event @event)
        {
            switch (@event.Type)
            {
                case EventType.Teleport:
                {
                    // 1. byte is the x coordinate
                    // 2. byte is the y coordinate
                    // 3. byte is the character direction
                    // Then 1 unknown byte
                    // Then 1 byte for the transtion type (0-5)
                    // Then a word for the map index
                    // Then 2 unknown bytes (seem to be 00 FF)
                    var teleportEvent = @event as TeleportEvent;
                    dataWriter.Write((byte)teleportEvent.X);
                    dataWriter.Write((byte)teleportEvent.Y);
                    dataWriter.WriteEnumAsByte(teleportEvent.Direction);
                    dataWriter.Write(teleportEvent.Unknown1);
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
                    // 2. byte are the chest flags
                    // 3. byte is an optional text index (0xff = no text)
                    // 4. byte is the chest index (0-based)
                    // 5. byte (0 = chest, 1 = pile/removable loot or item) or "remove if empty"
                    // word at position 6 is the key index if a key must unlock it
                    // last word is the event index (0-based) of the event that is called when unlocking fails
                    var chestEvent = @event as ChestEvent;
                    dataWriter.Write((byte)chestEvent.LockpickingChanceReduction);
                    dataWriter.WriteEnumAsByte(chestEvent.Flags);
                    dataWriter.Write((byte)chestEvent.TextIndex);
                    dataWriter.Write((byte)chestEvent.ChestIndex);
                    dataWriter.Write((byte)(chestEvent.RemoveWhenEmpty ? 1 : 0));
                    dataWriter.Write((ushort)chestEvent.KeyIndex);
                    dataWriter.Write((ushort)chestEvent.UnlockFailedEventIndex);
                    break;
                }
                case EventType.PopupText:
                {
                    // event image index (0xff = no image)
                    // trigger (1 = move, 2 = cursor, 3 = both)
                    // unknown boolean
                    // map text index as word
                    // 4 unknown bytes
                    var textEvent = @event as PopupTextEvent;
                    dataWriter.Write((byte)textEvent.EventImageIndex);
                    dataWriter.WriteEnumAsByte(textEvent.PopupTrigger);
                    dataWriter.Write((byte)(textEvent.UnknownBool ? 1 : 0));
                    dataWriter.Write((ushort)textEvent.TextIndex);
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
                case EventType.RemoveBuffs:
                {
                    var removeBuffsEvent = @event as RemoveBuffsEvent;
                    dataWriter.Write(removeBuffsEvent.AffectedBuff == null ? (byte)0 : (byte)removeBuffsEvent.AffectedBuff.Value);
                    dataWriter.Write(removeBuffsEvent.Unused);
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
                case EventType.Award:
                {
                    var awardEvent = @event as AwardEvent;
                    dataWriter.WriteEnumAsByte(awardEvent.TypeOfAward);
                    dataWriter.WriteEnumAsByte(awardEvent.Operation);
                    dataWriter.Write((byte)(awardEvent.Random ? 1 : 0));
                    dataWriter.WriteEnumAsByte(awardEvent.Target);
                    dataWriter.Write(awardEvent.Unknown);
                    dataWriter.Write(awardEvent.AwardTypeValue);
                    dataWriter.Write((ushort)awardEvent.Value);
                    break;
                }
                case EventType.ChangeTile:
                {
                    var changeTileEvent = @event as ChangeTileEvent;
                    byte[] tileData = new byte[4];
                    tileData[0] = (byte)(changeTileEvent.BackTileIndex & 0xff);
                    tileData[1] = (byte)(((changeTileEvent.BackTileIndex >> 8) & 0x07) << 5);
                    tileData[2] = (byte)((changeTileEvent.FrontTileIndex >> 8) & 0x07);
                    tileData[3] = (byte)(changeTileEvent.FrontTileIndex & 0xff);
                    dataWriter.Write((byte)changeTileEvent.X);
                    dataWriter.Write((byte)changeTileEvent.Y);
                    dataWriter.Write(changeTileEvent.Unknown);
                    dataWriter.Write(tileData);
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
                    dataWriter.Write(conditionEvent.Unknown1);
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
                    dataWriter.Write(createEvent.Unused);
                    dataWriter.Write((byte)createEvent.Amount);
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
