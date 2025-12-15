using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Pyrdacor.Extensions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.Objects;

internal sealed class EventData
{
    public Event? Event { get; private set; }
    public ushort? NextEventIndex { get; private set; }

    public static EventData Read(IDataReader dataReader)
    {
        var type = dataReader.ReadEnum8<EventType>();
        var next = dataReader.ReadWord();

        switch (type)
        {
            case EventType.Teleport:
                return new()
                {
                    Event = new TeleportEvent()
                    {
                        X = dataReader.ReadByte(),
                        Y = dataReader.ReadByte(),
                        Direction = dataReader.ReadEnum8<CharacterDirection>(),
                        NewTravelType = dataReader.ReadNullableEnum8<TravelType>(),
                        Transition = dataReader.ReadEnum8<TeleportEvent.TransitionType>(),
                        MapIndex = dataReader.ReadWord(),
                    },
                    NextEventIndex = next
                };
            case EventType.Door:
                return new()
                {
                    Event = new DoorEvent()
                    {
                        LockpickingChanceReduction = dataReader.ReadByte(),
                        DoorIndex = dataReader.ReadByte(),
                        TextIndex = dataReader.ReadByte(),
                        UnlockTextIndex = dataReader.ReadByte(),
                        KeyIndex = dataReader.ReadWord(),
                        UnlockFailedEventIndex = dataReader.ReadWord(),
                    },
                    NextEventIndex = next
                };
            case EventType.Chest:
                return new()
                {
                    Event = new ChestEvent()
                    {
                        LockpickingChanceReduction = dataReader.ReadByte(),
                        FindChanceReduction = dataReader.ReadByte(),
                        TextIndex = dataReader.ReadByte(),
                        ChestIndex = dataReader.ReadByte(),
                        Flags = dataReader.ReadEnum8<ChestEvent.ChestFlags>(),
                        KeyIndex = dataReader.ReadWord(),
                        UnlockFailedEventIndex = dataReader.ReadWord(),
                    },
                    NextEventIndex = next
                };
            case EventType.MapText:
                return new()
                {
                    Event = new PopupTextEvent()
                    {
                        EventImageIndex = dataReader.ReadByte(),
                        PopupTrigger = dataReader.ReadEnum8<EventTrigger>(),
                        TriggerIfBlind = dataReader.ReadByte() != 0,
                        TextIndex = dataReader.ReadByte(),
                    },
                    NextEventIndex = next
                };
            case EventType.Spinner:
                return new()
                {
                    Event = new SpinnerEvent()
                    {
                        Direction = dataReader.ReadEnum8<CharacterDirection>(),
                    },
                    NextEventIndex = next
                };
            case EventType.Trap:
                return new()
                {
                    Event = new TrapEvent()
                    {
                        Ailment = dataReader.ReadEnum8<TrapEvent.TrapAilment>(),
                        Target = dataReader.ReadEnum8<TrapEvent.TrapTarget>(),
                        AffectedGenders = dataReader.ReadEnum8<GenderFlag>(),
                        BaseDamage = dataReader.ReadByte(),
                    },
                    NextEventIndex = next
                };
            case EventType.ChangeBuffs:
            {
                var affectedBuffs = dataReader.ReadByte();

                return new()
                {
                    Event = new ChangeBuffsEvent()
                    {
                        AffectedBuff = affectedBuffs == 0 ? null : (ActiveSpellType)(affectedBuffs - 1),
                        Add = dataReader.ReadByte() != 0,
                        Value = dataReader.ReadWord(),
                        Duration = dataReader.ReadWord(),
                    },
                    NextEventIndex = next
                };
            }
            case EventType.Riddlemouth:
                return new()
                {
                    Event = new RiddlemouthEvent()
                    {
                        RiddleTextIndex = dataReader.ReadByte(),
                        SolutionTextIndex = dataReader.ReadByte(),
                        CorrectAnswerDictionaryIndex1 = dataReader.ReadWord(),
                        CorrectAnswerDictionaryIndex2 = dataReader.ReadWord(),
                    },
                    NextEventIndex = next
                };
            case EventType.Reward:
                return new()
                {
                    Event = new RewardEvent()
                    {
                        TypeOfReward = dataReader.ReadEnum8<RewardEvent.RewardType>(),
                        Operation = dataReader.ReadEnum8<RewardEvent.RewardOperation>(),
                        Random = dataReader.ReadByte() != 0,
                        Target = dataReader.ReadEnum8<RewardEvent.RewardTarget>(),
                        RewardTypeValue = dataReader.ReadWord(),
                        Value = dataReader.ReadWord(),
                    },
                    NextEventIndex = next
                };
            case EventType.ChangeTile:
                return new()
                {
                    Event = new ChangeTileEvent()
                    {
                        X = dataReader.ReadByte(),
                        Y = dataReader.ReadByte(),
                        FrontTileIndex = dataReader.ReadWord(),
                        MapIndex = dataReader.ReadWord(),
                    },
                    NextEventIndex = next
                };
            case EventType.StartBattle:
                return new()
                {
                    Event = new StartBattleEvent()
                    {
                        MonsterGroupIndex = dataReader.ReadWord(),
                    },
                    NextEventIndex = next
                };
            case EventType.EnterPlace:
            {
                var placeType = dataReader.ReadEnum8<PlaceType>();
                uint merchantDataIndex = placeType == PlaceType.Merchant || placeType == PlaceType.Library
                    ? dataReader.ReadWord()
                    : 0u;

                return new()
                {
                    Event = new EnterPlaceEvent()
                    {
                        ClosedTextIndex = dataReader.ReadByte(),
                        PlaceType = placeType,
                        OpeningHour = dataReader.ReadByte(),
                        ClosingHour = dataReader.ReadByte(),
                        UsePlaceTextIndex = dataReader.ReadByte(),
                        PlaceIndex = dataReader.ReadWord(),
                        MerchantDataIndex = merchantDataIndex,
                    },
                    NextEventIndex = next
                };
            }
            case EventType.Condition:
                return new()
                {
                    Event = new ConditionEvent()
                    {
                        TypeOfCondition = dataReader.ReadEnum8<ConditionEvent.ConditionType>(),
                        Value = dataReader.ReadByte(),
                        Count = dataReader.ReadByte(),
                        DisallowedAilments = dataReader.ReadEnum16<Condition>(),
                        ObjectIndex = dataReader.ReadWord(),
                        ContinueIfFalseWithMapEventIndex = dataReader.ReadWord(),
                    },
                    NextEventIndex = next
                };
            case EventType.Action:
                return new()
                {
                    Event = new ActionEvent()
                    {
                        TypeOfAction = dataReader.ReadEnum8<ActionEvent.ActionType>(),
                        Value = dataReader.ReadByte(),
                        Count = dataReader.ReadByte(),
                        ObjectIndex = dataReader.ReadWord(),
                    },                    
                    NextEventIndex = next
                };
            case EventType.Dice100Roll:
                return new()
                {
                    Event = new Dice100RollEvent()
                    {
                        Chance = dataReader.ReadByte(),
                        ContinueIfFalseWithMapEventIndex = dataReader.ReadWord(),
                    },
                    NextEventIndex = next
                };
            case EventType.Conversation:
            {
                var interaction = dataReader.ReadEnum8<ConversationEvent.InteractionType>();
                uint value = interaction < ConversationEvent.InteractionType.JoinParty
                    ? dataReader.ReadWord()
                    : 0u;

                return new()
                {
                    Event = new ConversationEvent()
                    {
                        Interaction = interaction,
                        Value = (ushort)value,
                    },
                    NextEventIndex = next
                };
            }
            case EventType.PrintText:
                return new()
                {
                    Event = new PrintTextEvent()
                    {
                        NPCTextIndex = dataReader.ReadByte(),
                    },
                    NextEventIndex = next
                };
            case EventType.Create:
            {
                var creationType = dataReader.ReadEnum8<CreateEvent.CreateType>();
                uint itemIndex = creationType == CreateEvent.CreateType.Item
                    ? dataReader.ReadWord()
                    : 0u;

                return new()
                {
                    Event = new CreateEvent()
                    {
                        TypeOfCreation = creationType,
                        Amount = dataReader.ReadWord(),
                        ItemIndex = itemIndex,
                    },
                    NextEventIndex = next
                };
            }
            case EventType.Decision:
                return new()
                {
                    Event = new DecisionEvent()
                    {
                        TextIndex = dataReader.ReadByte(),
                        NoEventIndex = dataReader.ReadWord(),
                    },
                    NextEventIndex = next
                };
            case EventType.ChangeMusic:
                return new()
                {
                    Event = new ChangeMusicEvent()
                    {
                        MusicIndex = dataReader.ReadByte(),
                        Volume = dataReader.ReadByte(),
                    },
                    NextEventIndex = next
                };
            case EventType.Exit:
                return new()
                {
                    Event = new ExitEvent(),
                    NextEventIndex = next
                };
            case EventType.Spawn:
                return new()
                {
                    Event = new SpawnEvent()
                    {
                        X = dataReader.ReadByte(),
                        Y = dataReader.ReadByte(),
                        TravelType = dataReader.ReadEnum8<TravelType>(),                        
                        MapIndex = dataReader.ReadWord(),
                    },
                    NextEventIndex = next
                };
            case EventType.Interact:
                return new()
                {
                    Event = new InteractEvent(),
                    NextEventIndex = next
                };
            case EventType.RemovePartyMember:
                return new()
                {
                    Event = new RemovePartyMemberEvent()
                    {
                        CharacterIndex = dataReader.ReadByte(),
                        ChestIndexEquipment = dataReader.ReadByte(),
                        ChestIndexInventory = dataReader.ReadByte(),
                    },
                    NextEventIndex = next
                };
            case EventType.Delay:
                return new()
                {
                    Event = new DelayEvent()
                    {
                        Milliseconds = dataReader.ReadWord(),
                    },
                    NextEventIndex = next
                };
            case EventType.PartyMemberCondition:
                return new()
                {
                    Event = new PartyMemberConditionEvent()
                    {
                        TypeOfCondition = dataReader.ReadEnum8<PartyMemberConditionEvent.PartyMemberConditionType>(),
                        ConditionValueIndex = dataReader.ReadByte(),
                        Target = dataReader.ReadEnum8<PartyMemberConditionEvent.PartyMemberConditionTarget>(),
                        DisallowedAilments = dataReader.ReadEnum16<Condition>(),
                        Value = dataReader.ReadWord(),
                        ContinueIfFalseWithMapEventIndex = dataReader.ReadWord(),
                    },
                    NextEventIndex = next
                };
            case EventType.Shake:
                return new()
                {
                    Event = new ShakeEvent()
                    {
                        Shakes = dataReader.ReadWord(),
                    },
                    NextEventIndex = next
                };
            case EventType.ShowMap:
                return new()
                {
                    Event = new ShowMapEvent()
                    {
                        Options = dataReader.ReadEnum8<ShowMapEvent.MapOptions>(),
                    },
                    NextEventIndex = next
                };
            default:
                throw new AmbermoonException(ExceptionScope.Data, "Invalid event type.");
        }
    }

    public void Write(IDataWriter dataWriter)
    {
        if (Event == null)
            throw new AmbermoonException(ExceptionScope.Application, "Event data was null when trying to write it.");

        Write(dataWriter, Event, NextEventIndex);
    }

    public static void Write(IDataWriter dataWriter, Event @event, uint? nextEventIndex)
    {
        dataWriter.WriteEnum8(@event.Type);
        dataWriter.Write((ushort)(nextEventIndex ?? 0xffff));

        switch (@event.Type)
        {
            case EventType.Teleport:
                var teleportEvent = (TeleportEvent)@event;
                dataWriter.Write((byte)teleportEvent.X);
                dataWriter.Write((byte)teleportEvent.Y);
                dataWriter.WriteEnum8(teleportEvent.Direction);
                dataWriter.WriteNullableEnum8(teleportEvent.NewTravelType);
                dataWriter.WriteEnum8(teleportEvent.Transition);
                dataWriter.Write((ushort)teleportEvent.MapIndex);
                break;
            case EventType.Door:
                var doorEvent = (DoorEvent)@event;
                dataWriter.Write((byte)doorEvent.LockpickingChanceReduction);
                dataWriter.Write((byte)doorEvent.DoorIndex);
                dataWriter.Write((byte)doorEvent.TextIndex);
                dataWriter.Write((byte)doorEvent.UnlockTextIndex);
                dataWriter.Write((ushort)doorEvent.KeyIndex);
                dataWriter.Write((ushort)doorEvent.UnlockFailedEventIndex);
                break;
            case EventType.Chest:
                var chestEvent = (ChestEvent)@event;
                dataWriter.Write((byte)chestEvent.LockpickingChanceReduction);
                dataWriter.Write((byte)chestEvent.FindChanceReduction);
                dataWriter.Write((byte)chestEvent.TextIndex);
                dataWriter.Write((byte)chestEvent.ChestIndex);
                dataWriter.WriteEnum8(chestEvent.Flags);
                dataWriter.Write((ushort)chestEvent.KeyIndex);
                dataWriter.Write((ushort)chestEvent.UnlockFailedEventIndex);
                break;
            case EventType.MapText:
                var popupTextEvent = (PopupTextEvent)@event;
                dataWriter.Write((byte)popupTextEvent.EventImageIndex);
                dataWriter.WriteEnum8(popupTextEvent.PopupTrigger);
                dataWriter.Write((byte)(popupTextEvent.TriggerIfBlind ? 1 : 0));
                dataWriter.Write((byte)popupTextEvent.TextIndex);
                break;
            case EventType.Spinner:
                var spinnerEvent = (SpinnerEvent)@event;
                dataWriter.WriteEnum8(spinnerEvent.Direction);
                break;
            case EventType.Trap:
                var trapEvent = (TrapEvent)@event;
                dataWriter.WriteEnum8(trapEvent.Ailment);
                dataWriter.WriteEnum8(trapEvent.Target);
                dataWriter.WriteEnum8(trapEvent.AffectedGenders);
                dataWriter.Write((byte)trapEvent.BaseDamage);
                break;
            case EventType.ChangeBuffs:
                var changeBuffsEvent = (ChangeBuffsEvent)@event;
                dataWriter.Write((byte)(changeBuffsEvent.AffectedBuff == null ? 0 : (byte)(changeBuffsEvent.AffectedBuff + 1)));
                dataWriter.Write((byte)(changeBuffsEvent.Add ? 1 : 0));
                dataWriter.Write((ushort)changeBuffsEvent.Value);
                dataWriter.Write((ushort)changeBuffsEvent.Duration);
                break;
            case EventType.Riddlemouth:
                var riddlemouthEvent = (RiddlemouthEvent)@event;
                dataWriter.Write((byte)riddlemouthEvent.RiddleTextIndex);
                dataWriter.Write((byte)riddlemouthEvent.SolutionTextIndex);
                dataWriter.Write((ushort)riddlemouthEvent.CorrectAnswerDictionaryIndex1);
                dataWriter.Write((ushort)riddlemouthEvent.CorrectAnswerDictionaryIndex2);
                break;
            case EventType.Reward:
                var rewardEvent = (RewardEvent)@event;
                dataWriter.WriteEnum8(rewardEvent.TypeOfReward);
                dataWriter.WriteEnum8(rewardEvent.Operation);
                dataWriter.Write((byte)(rewardEvent.Random ? 1 : 0));
                dataWriter.WriteEnum8(rewardEvent.Target);
                dataWriter.Write((ushort)rewardEvent.RewardTypeValue);
                dataWriter.Write((ushort)rewardEvent.Value);
                break;
            case EventType.ChangeTile:
                var changeTileEvent = (ChangeTileEvent)@event;
                dataWriter.Write((byte)changeTileEvent.X);
                dataWriter.Write((byte)changeTileEvent.Y);
                dataWriter.Write((ushort)changeTileEvent.FrontTileIndex);
                dataWriter.Write((ushort)changeTileEvent.MapIndex);
                break;
            case EventType.StartBattle:
                var startBattleEvent = (StartBattleEvent)@event;
                dataWriter.Write((ushort)startBattleEvent.MonsterGroupIndex);
                break;
            case EventType.EnterPlace:
                var enterPlaceEvent = (EnterPlaceEvent)@event;
                dataWriter.WriteEnum8(enterPlaceEvent.PlaceType);
                if (enterPlaceEvent.PlaceType == PlaceType.Merchant || enterPlaceEvent.PlaceType == PlaceType.Library)
                    dataWriter.Write((ushort)enterPlaceEvent.MerchantDataIndex);
                dataWriter.Write((byte)enterPlaceEvent.ClosedTextIndex);                
                dataWriter.Write((byte)enterPlaceEvent.OpeningHour);
                dataWriter.Write((byte)enterPlaceEvent.ClosingHour);
                dataWriter.Write((byte)enterPlaceEvent.UsePlaceTextIndex);
                dataWriter.Write((ushort)enterPlaceEvent.PlaceIndex);                
                break;
            case EventType.Condition:
                var conditionEvent = (ConditionEvent)@event;
                dataWriter.WriteEnum8(conditionEvent.TypeOfCondition);
                dataWriter.Write((byte)conditionEvent.Value);
                dataWriter.Write((byte)conditionEvent.Count);
                dataWriter.WriteEnum16(conditionEvent.DisallowedAilments);
                dataWriter.Write((ushort)conditionEvent.ObjectIndex);
                dataWriter.Write((ushort)conditionEvent.ContinueIfFalseWithMapEventIndex);
                break;
            case EventType.Action:
                var actionEvent = (ActionEvent)@event;
                dataWriter.WriteEnum8(actionEvent.TypeOfAction);
                dataWriter.Write((byte)actionEvent.Value);
                dataWriter.Write((byte)actionEvent.Count);
                dataWriter.Write((ushort)actionEvent.ObjectIndex);
                break;
            case EventType.Dice100Roll:
                var dice100RollEvent = (Dice100RollEvent)@event;
                dataWriter.Write((byte)dice100RollEvent.Chance);
                dataWriter.Write((ushort)dice100RollEvent.ContinueIfFalseWithMapEventIndex);
                break;
            case EventType.Conversation:
                var conversationEvent = (ConversationEvent)@event;
                dataWriter.WriteEnum8(conversationEvent.Interaction);
                if (conversationEvent.Interaction < ConversationEvent.InteractionType.JoinParty)
                    dataWriter.Write((ushort)conversationEvent.Value);
                break;
            case EventType.PrintText:
                var printTextEvent = (PrintTextEvent)@event;
                dataWriter.Write((byte)printTextEvent.NPCTextIndex);
                break;
            case EventType.Create:
                var createEvent = (CreateEvent)@event;
                dataWriter.WriteEnum8(createEvent.TypeOfCreation);
                if (createEvent.TypeOfCreation == CreateEvent.CreateType.Item)
                    dataWriter.Write((ushort)createEvent.ItemIndex);
                dataWriter.Write((ushort)createEvent.Amount);
                break;
            case EventType.Decision:
                var decisionEvent = (DecisionEvent)@event;
                dataWriter.Write((byte)decisionEvent.TextIndex);
                dataWriter.Write((ushort)decisionEvent.NoEventIndex);
                break;
            case EventType.ChangeMusic:
                var changeMusicEvent = (ChangeMusicEvent)@event;
                dataWriter.Write((byte)changeMusicEvent.MusicIndex);
                dataWriter.Write((byte)changeMusicEvent.Volume);
                break;
            case EventType.Exit:
                break;
            case EventType.Spawn:
                var spawnEvent = (SpawnEvent)@event;
                dataWriter.Write((byte)spawnEvent.X);
                dataWriter.Write((byte)spawnEvent.Y);
                dataWriter.WriteEnum8(spawnEvent.TravelType);
                dataWriter.Write((ushort)spawnEvent.MapIndex);
                break;
            case EventType.Interact:
                break;
            case EventType.RemovePartyMember:
                var removePartyMemberEvent = (RemovePartyMemberEvent)@event;
                dataWriter.Write((byte)removePartyMemberEvent.CharacterIndex);
                dataWriter.Write((byte)removePartyMemberEvent.ChestIndexEquipment);
                dataWriter.Write((byte)removePartyMemberEvent.ChestIndexInventory);
                break;
            case EventType.Delay:
                var delayEvent = (DelayEvent)@event;
                dataWriter.Write((ushort)delayEvent.Milliseconds);
                break;
            case EventType.PartyMemberCondition:
                var partyMemberConditionEvent = (PartyMemberConditionEvent)@event;
                dataWriter.WriteEnum8(partyMemberConditionEvent.TypeOfCondition);
                dataWriter.Write((byte)partyMemberConditionEvent.ConditionValueIndex);
                dataWriter.WriteEnum8(partyMemberConditionEvent.Target);
                dataWriter.WriteEnum16(partyMemberConditionEvent.DisallowedAilments);
                dataWriter.Write((ushort)partyMemberConditionEvent.Value);
                dataWriter.Write((ushort)partyMemberConditionEvent.ContinueIfFalseWithMapEventIndex);
                break;
            case EventType.Shake:
                var shakeEvent = (ShakeEvent)@event;
                dataWriter.Write((ushort)shakeEvent.Shakes);
                break;
            case EventType.ShowMap:
                var showMapEvent = (ShowMapEvent)@event;
                dataWriter.WriteEnum8(showMapEvent.Options);
                break;
            default:
                throw new AmbermoonException(ExceptionScope.Application, "Invalid event type.");
        }
    }

    public static void ReadEvents(IDataReader dataReader,
        List<Event> events, List<Event> eventList)
    {
        events.Clear();
        eventList.Clear();

        int numEvents = dataReader.ReadWord();
        var eventLookup = new Dictionary<int, EventData>();

        for (int i = 0; i < numEvents; i++)
        {
            var eventData = Read(dataReader);
            events.Add(eventData.Event!);
            eventLookup.Add(i, eventData);
        }

        int numChains = dataReader.ReadWord();

        for (int i = 0; i < numChains; i++)
        {
            eventList.Add(events[dataReader.ReadWord()]);
        }

        foreach (var eventData in eventLookup.Values.ToList())
        {
            if (eventData.NextEventIndex != null && eventData.NextEventIndex != 0xffff)
            {
                eventData.Event!.Next = eventLookup[eventData.NextEventIndex.Value].Event;
            }
        }
    }

    public static void WriteEvents(IDataWriter dataWriter,
        List<Event> events, List<Event> eventList)
    {
        dataWriter.Write((ushort)events.Count);

        foreach (var eventItem in events)
        {
            Write(dataWriter, eventItem, eventItem.Next == null ? null : (uint)events.IndexOf(eventItem.Next));
        }

        dataWriter.Write((ushort)eventList.Count);

        foreach (var eventItem in eventList)
        {
            dataWriter.Write((ushort)events.IndexOf(eventItem));
        }
    }
}
