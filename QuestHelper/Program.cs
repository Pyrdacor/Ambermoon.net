using Ambermoon;
using Ambermoon.Data;
using Ambermoon.Data.Legacy;
using Ambermoon.Data.Legacy.Serialization;

namespace QuestHelper
{
    enum QuestSourceType : int
    {
        Invalid = -2,
        None = -1,
        NPCKeyword,
        NPCItemGiven,
        Riddlemouth,
        Lever,
        MapText,
        Event,
        ItemObtained,
        ItemUsed,
        NPCTalk,
        NPCGoldGiven,
        NPCFoodGiven,
    }

    internal class Program
    {
        static string GetSourceData(QuestSourceType sourceType)
        {
            switch (sourceType)
            {
                case QuestSourceType.NPCKeyword:
                {
                    string data = "";
                    Console.Write("NPC Name: ");
                    data += Console.ReadLine() + ",";
                    Console.Write("Keyword: ");
                    data += Console.ReadLine();
                    return data;
                }
                case QuestSourceType.NPCItemGiven:
                {
                    string data = "";
                    Console.Write("NPC Name: ");
                    data += Console.ReadLine() + ",";
                    Console.Write("Item Name: ");
                    data += Console.ReadLine();
                    return data;
                }
                case QuestSourceType.Riddlemouth:
                {
                    Console.Write("Map Name: ");
                    return Console.ReadLine();
                }
                case QuestSourceType.Lever:
                {
                    Console.Write("Map Name: ");
                    return Console.ReadLine();
                }
                case QuestSourceType.MapText:
                {
                    string data = "";
                    Console.Write("Map Name: ");
                    data += Console.ReadLine() + ",";
                    Console.Write("Text: ");
                    data += Console.ReadLine();
                    return data;
                }
                case QuestSourceType.Event:
                {
                    Console.Write("Map Name: ");
                    return Console.ReadLine();
                }
                case QuestSourceType.ItemObtained:
                {
                    Console.Write("Item Name: ");
                    return Console.ReadLine();
                }
                case QuestSourceType.ItemUsed:
                {
                    Console.Write("Item Name: ");
                    return Console.ReadLine();
                }
                case QuestSourceType.NPCTalk:
                {
                    Console.Write("NPC Name: ");
                    return Console.ReadLine();
                }
                case QuestSourceType.NPCGoldGiven:
                {
                    Console.Write("NPC Name: ");
                    return Console.ReadLine();
                }
                case QuestSourceType.NPCFoodGiven:
                {
                    Console.Write("NPC Name: ");
                    return Console.ReadLine();
                }
                default:
                    return string.Empty;
            }
        }

        static void GetSource(out QuestSourceType sourceType, out string data)
        {
            var names = Enum.GetNames(typeof(QuestSourceType)).OrderBy(t => (int)Enum.Parse<QuestSourceType>(t)).ToList();

            Console.WriteLine("Enter source");
            names.ForEach(t => Console.WriteLine($"{(int)Enum.Parse<QuestSourceType>(t),2}: {t}"));
            Console.WriteLine($"{names.Count,2}: Quit");
            Console.Write("Type: ");

            if (!uint.TryParse(Console.ReadLine(), out var type) || type > names.Count)
            {
                Console.WriteLine("Invalid source type");
                sourceType = QuestSourceType.Invalid;
                data = string.Empty;
                return;
            }

            if (type == names.Count)
            {
                sourceType = QuestSourceType.None;
                data = string.Empty;
                return;
            }

            sourceType = (QuestSourceType)type;
            data = GetSourceData(sourceType);
        }

        static void SearchSource(GameData gameData, QuestSourceType sourceType, string data)
        {
            switch (sourceType)
            {
                case QuestSourceType.NPCKeyword:
                {
                    var dataParts = data.Split(',');
                    var name = dataParts[0].ToLower();
                    var keyword = dataParts[1].ToLower();
                    var npcs = gameData.CharacterManager.NPCs.Where(n => n.Name.ToLower().Contains(name)).ToList();

                    foreach (var npc in npcs)
                    {
                        foreach (ConversationEvent c in npc.Events.Where(e => e is ConversationEvent c && c.Interaction == ConversationEvent.InteractionType.Keyword).Cast<ConversationEvent>())
                        {
                            if (gameData.Dictionary.Entries[(int)c.KeywordIndex].ToLower().Contains(keyword))
                            {
                                Console.WriteLine($"NPC {npc.Index} ({npc.Name}), Keyword {c.KeywordIndex} ({gameData.Dictionary.Entries[(int)c.KeywordIndex]})");
                                GetAllEventsInSameChain(npc, c).ToList().ForEach(e => Console.WriteLine($"  {e.Index} ({e})"));
                            }
                        }
                    }

                    break;
                }
                case QuestSourceType.NPCItemGiven:
                {
                    var dataParts = data.Split(',');
                    var name = dataParts[0].ToLower();
                    var item = dataParts[1].ToLower();
                    var npcs = gameData.CharacterManager.NPCs.Where(n => n.Name.ToLower().Contains(name)).ToList();

                    foreach (var npc in npcs)
                    {
                        foreach (ConversationEvent c in npc.Events.Where(e => e is ConversationEvent c && c.Interaction == ConversationEvent.InteractionType.GiveItem).Cast<ConversationEvent>())
                        {
                            var itemName = gameData.ItemManager.GetItem(c.ItemIndex).Name;

                            if (itemName.ToLower().Contains(item))
                            {
                                Console.WriteLine($"NPC {npc.Index} ({npc.Name}), Item {c.ItemIndex} ({itemName})");
                                GetAllEventsInSameChain(npc, c).ToList().ForEach(e => Console.WriteLine($"  {e.Index} ({e})"));
                            }
                        }
                    }

                    break;
                }
                case QuestSourceType.Riddlemouth:
                {
                    var mapName = data.ToLower();
                    var maps = gameData.MapManager.Maps.Where(m => m.Name.ToLower().Contains(mapName)).ToList();

                    foreach (var map in maps)
                    {
                        var riddleMouthEvents = map.Events.Where(e => e is RiddlemouthEvent).Cast<RiddlemouthEvent>().ToList();

                        if (riddleMouthEvents.Count > 0)
                        {
                            Console.WriteLine($"Map {map.Index} ({map.Name})");
                            riddleMouthEvents.ForEach(r =>
                            {
                                var location = GetEventLocation(map, r).First();
                                var tileChangeEvent = map.Events.FirstOrDefault(e => e is ChangeTileEvent c && (c.MapIndex == 0 || c.MapIndex == map.Index) && c.X == location.X && c.Y == location.Y) as ChangeTileEvent;
                                Console.WriteLine($"X: {location.X}, Y: {location.Y}, New Tile Index: {tileChangeEvent?.FrontTileIndex ?? 0xffff}");
                            });
                        }
                    }

                    break;
                }
                case QuestSourceType.Lever:
                {
                    var mapName = data.ToLower();
                    var maps = gameData.MapManager.Maps.Where(m => m.Name.ToLower().Contains(mapName)).ToList();

                    foreach (var map in maps)
                    {
                        var handEvents = map.Events.Where(e => e is ConditionEvent c && (c.TypeOfCondition == ConditionEvent.ConditionType.Hand || (c.TypeOfCondition == ConditionEvent.ConditionType.MultiCursor && (c.ObjectIndex & 0x1) != 0))).Cast<ConditionEvent>().ToList();

                        if (handEvents.Count > 0)
                        {
                            Console.WriteLine($"Map {map.Index} ({map.Name})");
                            handEvents.ForEach(r =>
                            {
                                var location = GetEventLocation(map, r).First();
                                Console.WriteLine($"Triggered from tile at {location.X},{location.Y}");
                                var tileChangeEvents = GetAllEventsInSameChain(map, r).Where(e => e is ChangeTileEvent c).Cast<ChangeTileEvent>().ToList();
                                tileChangeEvents.ForEach(tileChangeEvent => Console.WriteLine($"X: {tileChangeEvent.X}, Y: {tileChangeEvent.Y}, New Tile Index: {tileChangeEvent.FrontTileIndex}, Map Index: {(tileChangeEvent.MapIndex == 0 ? map.Index : tileChangeEvent.MapIndex)}"));
                            });
                        }
                    }

                    break;
                }
                case QuestSourceType.MapText:
                {
                    var dataParts = data.Split(',');
                    var mapName = dataParts[0].ToLower();
                    var text = dataParts[1].ToLower();
                    var maps = gameData.MapManager.Maps.Where(m => m.Name.ToLower().Contains(mapName)).ToList();

                    foreach (var map in maps)
                    {
                        var texts = map.Texts.Select((text, index) => new { text, index }).Where(t => t.text.ToLower().Contains(text)).ToList();

                        if (texts.Count > 0)
                        {
                            Console.WriteLine($"Map {map.Index} ({map.Name})");
                            texts.ForEach(t =>
                            {
                                Console.WriteLine($"Text = {t.text}");
                                var events = map.Events.Where(e => e is PopupTextEvent p && p.TextIndex == t.index).ToList();
                                events.ForEach(e =>
                                {
                                    var chain = GetAllEventsInSameChain(map, e);
                                    var location = GetEventLocation(map, e).First();
                                    Console.WriteLine($"Triggered from tile at {location.X},{location.Y}");
                                    foreach (var ev in chain)
                                        Console.WriteLine(ev);
                                });
                            });
                        }
                    }

                    break;
                }
                case QuestSourceType.Event:
                {
                    var mapName = data.ToLower();
                    var maps = gameData.MapManager.Maps.Where(m => m.Name.ToLower().Contains(mapName)).ToList();

                    foreach (var map in maps)
                    {
                        var events = map.Events.Where(e => e is PopupTextEvent p && p.EventImageIndex != 0xff).Cast<PopupTextEvent>().ToList();

                        if (events.Count > 0)
                        {
                            Console.WriteLine($"Map {map.Index} ({map.Name})");
                            events.ForEach(e =>
                            {
                                var chain = GetAllEventsInSameChain(map, e);
                                var location = GetEventLocation(map, e).First();
                                Console.WriteLine($"Triggered from tile at {location.X},{location.Y}");
                                foreach (var ev in chain)
                                    Console.WriteLine(ev);
                            });
                        }
                    }

                    break;
                }
                case QuestSourceType.ItemObtained:
                {
                    var itemName = data.ToLower();
                    var items = gameData.ItemManager.Items.Where(item => item.Name.ToLower().Contains(itemName)).ToList();
                    var itemIndices = items.Select(i => i.Index).ToHashSet();
                    var chestReader = new ChestReader();
                    var chests = gameData.Files["Save.00/Chest_data.amb"].Files.ToDictionary(entry => (uint)entry.Key, entry => Chest.Load(chestReader, entry.Value));

                    foreach (var map in gameData.MapManager.Maps)
                    {
                        var chestEvents = map.Events.Where(e => e is ChestEvent).Cast<ChestEvent>().ToList();

                        foreach (var chestEvent in chestEvents)
                        {
                            var chest = chests[chestEvent.RealChestIndex];

                            if (chest.Slots.ToList().Any(s => itemIndices.Contains(s.ItemIndex)))
                            {
                                var chain = GetAllEventsInSameChain(map, chestEvent);
                                var location = GetEventLocation(map, chestEvent).First();
                                Console.WriteLine($"Triggered on map {map.Index} ({map.Name}) from tile at {location.X},{location.Y}");
                                foreach (var ev in chain)
                                    Console.WriteLine(ev);
                            }
                        }
                    }

                    foreach (var npc in gameData.CharacterManager.NPCs)
                    {
                        var createEvents = npc.Events.Where(e => e is CreateEvent).Cast<CreateEvent>().ToList();

                        foreach (var createEvent in createEvents)
                        {
                            if (createEvent.TypeOfCreation == CreateEvent.CreateType.Item && itemIndices.Contains(createEvent.ItemIndex))
                            {
                                var chain = GetAllEventsInSameChain(npc, createEvent);
                                Console.WriteLine($"Triggered by NPC {npc.Index} ({npc.Name})");
                                foreach (var ev in chain)
                                    Console.WriteLine(ev);
                            }
                        }
                    }

                    break;
                }
                case QuestSourceType.ItemUsed:
                {
                    var itemName = data.ToLower();
                    var items = gameData.ItemManager.Items.Where(item => item.Name.ToLower().Contains(itemName)).ToList();
                    var itemIndices = items.Select(i => i.Index).ToHashSet();

                    foreach (var map in gameData.MapManager.Maps)
                    {
                        var useItemEvents = map.Events.Where(e => e is ConditionEvent c && c.TypeOfCondition == ConditionEvent.ConditionType.UseItem).Cast<ConditionEvent>().ToList();

                        foreach (var useItemEvent in useItemEvents)
                        {
                            if (itemIndices.Contains(useItemEvent.ObjectIndex))
                            {
                                var chain = GetAllEventsInSameChain(map, useItemEvent);
                                var location = GetEventLocation(map, useItemEvent).First();
                                Console.WriteLine($"Triggered on map {map.Index} ({map.Name}) from tile at {location.X},{location.Y}");
                                foreach (var ev in chain)
                                    Console.WriteLine(ev);
                            }
                        }
                    }

                    break;
                }
                case QuestSourceType.NPCTalk:
                {
                    var name = data.ToLower();
                    var npcs = gameData.CharacterManager.NPCs.Where(n => n.Name.ToLower().Contains(name)).ToList();

                    foreach (var npc in npcs)
                    {
                        foreach (ConversationEvent c in npc.Events.Where(e => e is ConversationEvent c && c.Interaction == ConversationEvent.InteractionType.Talk).Cast<ConversationEvent>())
                        {
                            Console.WriteLine($"NPC {npc.Index} ({npc.Name})");
                            GetAllEventsInSameChain(npc, c).ToList().ForEach(e => Console.WriteLine($"  {e})"));
                        }
                    }

                    break;
                }
                case QuestSourceType.NPCGoldGiven:
                {
                    var name = data.ToLower();
                    var npcs = gameData.CharacterManager.NPCs.Where(n => n.Name.ToLower().Contains(name)).ToList();

                    foreach (var npc in npcs)
                    {
                        foreach (ConversationEvent c in npc.Events.Where(e => e is ConversationEvent c && c.Interaction == ConversationEvent.InteractionType.GiveGold).Cast<ConversationEvent>())
                        {
                            Console.WriteLine($"NPC {npc.Index} ({npc.Name})");
                            GetAllEventsInSameChain(npc, c).ToList().ForEach(e => Console.WriteLine($"  {e})"));
                        }
                    }

                    break;
                }
                case QuestSourceType.NPCFoodGiven:
                {
                    var name = data.ToLower();
                    var npcs = gameData.CharacterManager.NPCs.Where(n => n.Name.ToLower().Contains(name)).ToList();

                    foreach (var npc in npcs)
                    {
                        foreach (ConversationEvent c in npc.Events.Where(e => e is ConversationEvent c && c.Interaction == ConversationEvent.InteractionType.GiveFood).Cast<ConversationEvent>())
                        {
                            Console.WriteLine($"NPC {npc.Index} ({npc.Name})");
                            GetAllEventsInSameChain(npc, c).ToList().ForEach(e => Console.WriteLine($"  {e})"));
                        }
                    }

                    break;
                }
            }
        }

        static Event? GetBranchTarget(IEventProvider eventProvider, Event @event)
        {
            Event? Result(uint eventIndex) => eventIndex == 0xffff ? null : eventProvider.Events[(int)eventIndex];

            if (@event is ConditionEvent conditionEvent)
                return Result(conditionEvent.ContinueIfFalseWithMapEventIndex);
            if (@event is DecisionEvent decisionEvent)
                return Result(decisionEvent.NoEventIndex);
            if (@event is DoorEvent doorEvent)
                return Result(doorEvent.UnlockFailedEventIndex);
            if (@event is ChestEvent chestEvent)
                return Result(chestEvent.UnlockFailedEventIndex);
            if (@event is PartyMemberConditionEvent partyMemberConditionEvent)
                return Result(partyMemberConditionEvent.ContinueIfFalseWithMapEventIndex);
            if (@event is Dice100RollEvent dice100RollEvent)
                return Result(dice100RollEvent.ContinueIfFalseWithMapEventIndex);

            return null;
        }

        static IEnumerable<Position> GetEventLocation(Map map, Event @event)
        {
            var chain = GetEventChain(map, @event);

            if (chain != null)
            {
                int eventIndex = 1 + map.EventList.IndexOf(chain);

                if (map.Type == MapType.Map2D)
                {
                    for (int y = 0; y < map.Height; y++)
                    {
                        for (int x = 0; x < map.Width; x++)
                        {
                            if (map.Tiles[x, y].MapEventId == eventIndex)
                                yield return new Position(1 + x, 1 + y);
                        }
                    }
                }
                else
                {
                    for (int y = 0; y < map.Height; y++)
                    {
                        for (int x = 0; x < map.Width; x++)
                        {
                            if (map.Blocks[x, y].MapEventId == eventIndex)
                                yield return new Position(1 + x, 1 + y);
                        }
                    }
                }
            }
        }

        static Event? GetEventChain(IEventProvider eventProvider, Event @event)
        {
            int index = eventProvider.EventList.IndexOf(@event);

            if (index != -1)
            {
                return @event;
            }

            var start = GetAllEventsInSameChain(eventProvider, @event).First();

            if (start == @event)
                return null;

            return GetEventChain(eventProvider, start);
        }

        static Event[] GetAllEventsInSameChain(IEventProvider eventProvider, Event @event)
        {
            int index = eventProvider.EventList.IndexOf(@event);

            IEnumerable<Event> GetUntilEnd(Event current, Event? end = null)
            {
                while (current != end)
                {
                    yield return current;
                    current = current.Next;
                }
            }

            if (index != -1)
            {
                return GetUntilEnd(@event, null).ToArray();
            }

            var eventChains = eventProvider.EventList;

            // Add sub-chains based on branch targets
            foreach (var branchTargetEvent in eventProvider.Events.Select(e => GetBranchTarget(eventProvider, e)).Where(t => t != null))
            {
                if (!eventChains.Contains(branchTargetEvent))
                    eventChains.Add(branchTargetEvent);
            }

            foreach (var eventChain in eventChains)
            {
                var current = eventChain;
                var checkedEvents = new HashSet<Event>();

                while (current != null)
                {
                    if (checkedEvents.Contains(current))
                        break;

                    checkedEvents.Add(current);

                    if (current == @event)
                        return GetUntilEnd(eventChain, current).Concat(GetUntilEnd(current.Next)).ToArray();

                    current = current.Next;
                }
            }

            return [];
        }

        static void Main(string[] args)
        {
            var gameData = new GameData();
            gameData.Load(args[0]);

            Console.WriteLine("Find quests");

            while (true)
            {
                Console.WriteLine();
                GetSource(out var sourceType, out var data);

                if (sourceType == QuestSourceType.Invalid)
                    continue;

                if (sourceType == QuestSourceType.None)
                    break;

                Console.WriteLine();
                SearchSource(gameData, sourceType, data);
            }
        }
    }
}
