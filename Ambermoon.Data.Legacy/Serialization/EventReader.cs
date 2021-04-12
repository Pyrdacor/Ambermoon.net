using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Serialization;
using System;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.Serialization
{
    internal class EventReader
    {
        public static void ReadEvents(IDataReader dataReader,
            List<Event> events, List<Event> eventList)
        {
            uint numEvents = dataReader.ReadWord();

            // There are numEvents 16 bit values.
            // Each gives the offset of the event to use.
            // Each event data is 12 bytes in size.

            // After this the total number of events is given.
            // Events can be chained (linked list). Each chain
            // is identified by an event id on some map tiles
            // or inside NPCs etc.

            // The last two bytes of each event data contain the
            // offset of the next event data or 0xFFFF if this is
            // the last event of the chain/list.
            // Note that the linked list can have a non-linear order.

            // E.g. in map 8 the first map event (index 0) references
            // map event 2 and this references map event 1 which is the
            // end chunk of the first map event chain.
            uint[] eventOffsets = new uint[numEvents];

            for (uint i = 0; i < numEvents; ++i)
                eventOffsets[i] = dataReader.ReadWord();

            events.Clear();

            if (numEvents > 0)
            {
                uint numTotalEvents = dataReader.ReadWord();
                var eventInfos = new List<Tuple<Event, int>>();

                // read all events and the next event index
                for (uint i = 0; i < numTotalEvents; ++i)
                {
                    var @event = ParseEvent(dataReader);
                    @event.Index = i + 1;
                    eventInfos.Add(Tuple.Create(@event, (int)dataReader.ReadWord()));
                    events.Add(@event);
                }

                foreach (var @event in eventInfos)
                {
                    @event.Item1.Next = @event.Item2 == 0xffff ? null : eventInfos[@event.Item2].Item1;
                }

                foreach (var eventOffset in eventOffsets)
                    eventList.Add(eventInfos[(int)eventOffset].Item1);
            }
        }

        static Event ParseEvent(IDataReader dataReader)
        {
            Event @event;
            var type = (EventType)dataReader.ReadByte();

            switch (type)
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
                    uint x = dataReader.ReadByte();
                    uint y = dataReader.ReadByte();
                    var direction = (CharacterDirection)dataReader.ReadByte();
                    var unknown1 = dataReader.ReadByte();
                    var transition = (TeleportEvent.TransitionType)dataReader.ReadByte();
                    uint mapIndex = dataReader.ReadWord();
                    var unknown2 = dataReader.ReadBytes(2);
                    @event = new TeleportEvent
                    {
                        MapIndex = mapIndex,
                        X = x,
                        Y = y,
                        Direction = direction,
                        Transition = transition,
                        Unknown1 = unknown1,
                        Unknown2 = unknown2,
                    };
                    break;
                }
                case EventType.Door:
                {
                    // 1. byte is a lockpicking chance reduction (0: already open, 100: can't open via lockpicking)
                    // 2. byte is the door index (used for unlock bits in savegame)
                    // 3. byte is an optional text index that is shown initially (0xff = no text)
                    // 4. byte is an optional text index if the door was unlocked (0xff = no text)
                    // 5. byte is unknown (always 0)
                    // word at position 6 is the key index if a key must unlock it
                    // last word is the event index (0-based) of the event that is called when unlocking fails
                    var lockpickingChanceReduction = dataReader.ReadByte();
                    var doorIndex = dataReader.ReadByte();
                    var textIndex = dataReader.ReadByte();
                    var unlockTextIndex = dataReader.ReadByte();
                    var unknown = dataReader.ReadByte(); // Unknown
                    uint keyIndex = dataReader.ReadWord();
                    var unlockFailEventIndex = dataReader.ReadWord();
                    @event = new DoorEvent
                    {
                        LockpickingChanceReduction = lockpickingChanceReduction,
                        DoorIndex = doorIndex,
                        TextIndex = textIndex,
                        UnlockTextIndex = unlockTextIndex,
                        Unknown = unknown,
                        KeyIndex = keyIndex,
                        UnlockFailedEventIndex = unlockFailEventIndex
                    };
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
                    var lockpickingChanceReduction = dataReader.ReadByte();
                    var flags = (ChestEvent.ChestFlags)dataReader.ReadByte();
                    var textIndex = dataReader.ReadByte();
                    uint chestIndex = dataReader.ReadByte();
                    bool removeWhenEmpty = dataReader.ReadByte() != 0;
                    uint keyIndex = dataReader.ReadWord();
                    var unlockFailEventIndex = dataReader.ReadWord();
                    @event = new ChestEvent
                    {
                        LockpickingChanceReduction = lockpickingChanceReduction,
                        Flags = flags,
                        TextIndex = textIndex,
                        ChestIndex = chestIndex,
                        RemoveWhenEmpty = removeWhenEmpty,
                        KeyIndex = keyIndex,
                        UnlockFailedEventIndex = unlockFailEventIndex
                    };
                    break;
                }
                case EventType.PopupText:
                {
                    // event image index (0xff = no image)
                    // trigger (1 = move, 2 = eye cursor, 3 = both)
                    // 1 unknown byte
                    // map text index as word
                    // 4 unknown bytes
                    var eventImageIndex = dataReader.ReadByte();
                    var popupTrigger = (EventTrigger)dataReader.ReadByte();
                    var unknown1 = dataReader.ReadByte();
                    var textIndex = dataReader.ReadWord();
                    var unknown2 = dataReader.ReadBytes(4);
                    @event = new PopupTextEvent
                    {
                        EventImageIndex = eventImageIndex,
                        PopupTrigger = popupTrigger,
                        TextIndex = textIndex,
                        Unknown1 = unknown1,
                        Unknown2 = unknown2
                    };
                    break;
                }
                case EventType.Spinner:
                {
                    var direction = (CharacterDirection)dataReader.ReadByte();
                    var unknown = dataReader.ReadBytes(8);
                    @event = new SpinnerEvent
                    {
                        Direction = direction,
                        Unknown = unknown
                    };
                    break;
                }
                case EventType.Trap:
                {
                    var ailment = (TrapEvent.TrapAilment)dataReader.ReadByte();
                    var target = (TrapEvent.TrapTarget)dataReader.ReadByte();
                    var affectedGenders = (GenderFlag)dataReader.ReadByte();
                    var baseDamage = dataReader.ReadByte();
                    var unused = dataReader.ReadBytes(5); // unused
                    @event = new TrapEvent
                    {
                        Ailment = ailment,
                        Target = target,
                        AffectedGenders = affectedGenders,
                        BaseDamage = baseDamage,
                        Unused = unused
                    };
                    break;
                }
                case EventType.RemoveBuffs:
                {
                    byte affectedBuffs = dataReader.ReadByte();
                    var unused = dataReader.ReadBytes(8);
                    @event = new RemoveBuffsEvent
                    {
                        AffectedBuff = affectedBuffs == 0 ? (ActiveSpellType?)null: (ActiveSpellType)(affectedBuffs - 1),
                        Unused = unused
                    };
                    break;
                }
                case EventType.Riddlemouth:
                {
                    var introTextIndex = dataReader.ReadByte();
                    var solutionTextIndex = dataReader.ReadByte();
                    var unknown = dataReader.ReadBytes(5);
                    var correctAnswerTextIndex = dataReader.ReadWord();
                    @event = new RiddlemouthEvent
                    {
                        RiddleTextIndex = introTextIndex,
                        SolutionTextIndex = solutionTextIndex,
                        CorrectAnswerDictionaryIndex = correctAnswerTextIndex,
                        Unknown = unknown
                    };
                    break;
                }
                case EventType.Award:
                {
                    var awardType = (AwardEvent.AwardType)dataReader.ReadByte();
                    var awardOperation = (AwardEvent.AwardOperation)dataReader.ReadByte();
                    var random = dataReader.ReadByte() != 0;
                    var awardTarget = (AwardEvent.AwardTarget)dataReader.ReadByte();
                    var unknown = dataReader.ReadByte();
                    var awardTypeValue = dataReader.ReadWord();
                    var value = dataReader.ReadWord();
                    @event = new AwardEvent
                    {
                        TypeOfAward = awardType,
                        Operation = awardOperation,
                        Random = random,
                        Target = awardTarget,
                        AwardTypeValue = awardTypeValue,
                        Value = value,
                        Unknown = unknown
                    };
                    break;
                }
                case EventType.ChangeTile:
                {
                    var x = dataReader.ReadByte();
                    var y = dataReader.ReadByte();
                    var unknown = dataReader.ReadByte();
                    var tileData = dataReader.ReadBytes(4);
                    var mapIndex = dataReader.ReadWord();
                    @event = new ChangeTileEvent
                    {
                        X = x,
                        Y = y,
                        BackTileIndex = ((uint)(tileData[1] & 0xe0) << 3) | tileData[0],
                        FrontTileIndex = ((uint)(tileData[2] & 0x07) << 8) | tileData[3],
                        MapIndex = mapIndex,
                        Unknown = unknown
                    };
                    break;
                }
                case EventType.StartBattle:
                {
                    var unknown1 = dataReader.ReadBytes(6);
                    var monsterGroupIndex = dataReader.ReadByte();
                    var unknown2 = dataReader.ReadBytes(2);
                    @event = new StartBattleEvent
                    {
                        MonsterGroupIndex = monsterGroupIndex,
                        Unknown1 = unknown1,
                        Unknown2 = unknown2
                    };
                    break;
                }
                case EventType.EnterPlace:
                {
                    // map text index when closed (0xff is default message)
                    // place type (see PlaceType)
                    // opening hour
                    // closing hour
                    // text index for using the place (sleep, train, buy, etc)
                    // place index (1-based, word)
                    // 2 unknown bytes
                    var textIndexWhenClosed = dataReader.ReadByte();
                    var placeType = (PlaceType)dataReader.ReadByte();
                    var openingHour = dataReader.ReadByte();
                    var closingHour = dataReader.ReadByte();
                    var usePlaceTextIndex = dataReader.ReadByte();
                    var placeIndex = dataReader.ReadWord();
                    var merchantIndex = dataReader.ReadWord();
                    @event = new EnterPlaceEvent
                    {
                        ClosedTextIndex = textIndexWhenClosed,
                        PlaceType = placeType,
                        OpeningHour = openingHour,
                        ClosingHour = closingHour,
                        PlaceIndex = placeIndex,
                        UsePlaceTextIndex = usePlaceTextIndex,
                        MerchantDataIndex = merchantIndex
                    };
                    break;
                }
                case EventType.Condition:
                {
                    var conditionType = (ConditionEvent.ConditionType)dataReader.ReadByte(); // TODO: this needs more research
                    var value = dataReader.ReadByte();
                    var count = dataReader.ReadByte();
                    var unknown1 = dataReader.ReadBytes(2);
                    var objectIndex = dataReader.ReadWord();
                    var jumpToIfNotFulfilled = dataReader.ReadWord();
                    @event = new ConditionEvent
                    {
                        TypeOfCondition = conditionType,
                        ObjectIndex = objectIndex,
                        Value = value,
                        Count = count,
                        Unknown1 = unknown1,
                        ContinueIfFalseWithMapEventIndex = jumpToIfNotFulfilled
                    };
                    break;
                }
                case EventType.Action:
                {
                    var actionType = (ActionEvent.ActionType)dataReader.ReadByte();
                    var value = dataReader.ReadByte();
                    var count = dataReader.ReadByte();
                    var unknown1 = dataReader.ReadBytes(2);
                    var objectIndex = dataReader.ReadWord();
                    var unknown2 = dataReader.ReadBytes(2);
                    @event = new ActionEvent
                    {
                        TypeOfAction = actionType,
                        ObjectIndex = objectIndex,
                        Value = value,
                        Unknown1 = unknown1,
                        Unknown2 = unknown2
                    };
                    break;
                }
                case EventType.Dice100Roll:
                {
                    var chance = dataReader.ReadByte();
                    var unused = dataReader.ReadBytes(6);
                    var jumpToIfNotFulfilled = dataReader.ReadWord();
                    @event = new Dice100RollEvent
                    {
                        Chance = chance,
                        Unused = unused,
                        ContinueIfFalseWithMapEventIndex = jumpToIfNotFulfilled
                    };
                    break;
                }
                case EventType.Conversation:
                {
                    var interaction = (ConversationEvent.InteractionType)dataReader.ReadByte();
                    var unused1 = dataReader.ReadBytes(4); // unused
                    var value = dataReader.ReadWord();
                    var unused2 = dataReader.ReadBytes(2); // unused
                    @event = new ConversationEvent
                    {
                        Interaction = interaction,
                        Value = value,
                        Unused1 = unused1,
                        Unused2 = unused2
                    };
                    break;
                }
                case EventType.PrintText:
                {
                    var npcTextIndex = dataReader.ReadByte();
                    var unused = dataReader.ReadBytes(8); // unused
                    @event = new PrintTextEvent
                    {
                        NPCTextIndex = npcTextIndex,
                        Unused = unused
                    };
                    break;
                }
                case EventType.Decision:
                {
                    var textIndex = dataReader.ReadByte();
                    var unknown1 = dataReader.ReadBytes(6);
                    var noEventIndex = dataReader.ReadWord();
                    @event = new DecisionEvent
                    {
                        TextIndex = textIndex,
                        NoEventIndex = noEventIndex,
                        Unknown1 = unknown1
                    };
                    break;
                }
                case EventType.ChangeMusic:
                {
                    var musicIndex = dataReader.ReadWord();
                    var volume = dataReader.ReadByte();
                    var unknown1 = dataReader.ReadBytes(6);
                    @event = new ChangeMusicEvent
                    {
                        MusicIndex = musicIndex,
                        Volume = volume,
                        Unknown1 = unknown1
                    };
                    break;
                }
                case EventType.Exit:
                {
                    @event = new ExitEvent
                    {
                        Unused = dataReader.ReadBytes(9)
                    };
                    break;
                }
                case EventType.Spawn:
                {
                    // byte0: x
                    // byte1: y
                    // byte2: travel type (see TravelType)
                    // byte3-4: unused?
                    // byte5-6: map index
                    // byte7-8: unused?
                    var x = dataReader.ReadByte();
                    var y = dataReader.ReadByte();
                    var travelType = (TravelType)dataReader.ReadByte();
                    var unknown1 = dataReader.ReadBytes(2); // unknown
                    var mapIndex = dataReader.ReadWord();
                    var unknown2 = dataReader.ReadBytes(2); // unknown
                    @event = new SpawnEvent
                    {
                        X = x,
                        Y = y,
                        TravelType = travelType,
                        Unknown1 = unknown1,
                        MapIndex = mapIndex,
                        Unknown2 = unknown2
                    };
                    break;
                }
                case EventType.Interact:
                {
                    @event = new InteractEvent
                    {
                        Unused = dataReader.ReadBytes(9)
                    };
                    break;
                }
                default:
                {
                    @event = new DebugEvent
                    {
                        Data = dataReader.ReadBytes(9)
                    };
                    break;
                }
            }

            @event.Type = type;

            return @event;
        }
    }
}
