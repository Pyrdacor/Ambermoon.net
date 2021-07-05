using Ambermoon.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon
{
    class Cheats
    {
        static readonly System.Random random = new System.Random(DateTime.Now.Millisecond);

        static Dictionary<string, KeyValuePair<string, Action<Game, string[]>>> cheats =
            new Dictionary<string, KeyValuePair<string, Action<Game, string[]>>>
        {
            { "help",
                Create
                (
                    "Shows general help or cheat help." + Environment.NewLine +
                    "Usage: help [cheat]",
                    Help
                )
            },
            { "godmode",
                Create
                (
                    "Makes the party invulnerable." + Environment.NewLine +
                    "Usage: godmode [0/1]",
                    Godmode
                )
            },
            { "netsrak",
                Create
                (
                    "Maxes all stats and grants all spells and languages." + Environment.NewLine +
                    "Usage: netsrak",
                    Netsrak
                )
            },
            { "maps",
                Create
                (
                    "Shows a list of all maps." + Environment.NewLine +
                    "Usage: maps" + Environment.NewLine +
                    "Usage: maps <partial_name>",
                    ShowMaps
                )
            },
            { "teleport",
                Create
                (
                    "Makes the party invulnerable." + Environment.NewLine +
                    "Usage: teleport <map_id> [x] [y] [direction]" + Environment.NewLine +
                    "   or: teleport <world> <x> <y> [direction]",
                    Teleport
                )
            },
            { "monsters",
                Create
                (
                    "Shows a list of all available monster groups." + Environment.NewLine +
                    "Usage: monsters",
                    ShowMonsters
                )
            },
            { "fight",
                Create
                (
                    "Starts a fight against a given monster group." + Environment.NewLine +
                    "Usage: fight <monster_group_id>",
                    StartBattle
                )
            },
            { "items",
                Create
                (
                    "Shows a list of all available items." + Environment.NewLine +
                    "Usage: items",
                    ShowItems
                )
            },
            { "give",
                Create
                (
                    "Gives an item to a party member." + Environment.NewLine +
                    "Usage: give <item_index> [amount] [party_member_index]",
                    GiveItem
                )
            },
            { "fly",
                Create
                (
                    "Let the party fly on world maps." + Environment.NewLine +
                    "Usage: fly",
                    Fly
                )
            }
        };

        static KeyValuePair<string, Action<Game, string[]>> Create(string help, Action<Game, string[]> action)
            => KeyValuePair.Create(help, action);

        static string currentAutoFillInput = null;
        static string currentInput = "";
        static int autoFillIndex = -1;
        static int cursorPosition = 0;
        static int historyIndex = -1;
        static readonly List<string> history = new List<string>();

        public static void ProcessInput(ConsoleKeyInfo keyInfo, Game game)
        {
            if (keyInfo.Key != ConsoleKey.Tab)
                autoFillIndex = -1;

            if (keyInfo.Key != ConsoleKey.Tab &&
                keyInfo.Key != ConsoleKey.Enter)
            {
                if (currentAutoFillInput != null)
                {
                    int lengthDiff = currentAutoFillInput.Length - currentInput.Length;
                    currentAutoFillInput = null;
                    Console.CursorLeft = 0;
                    Console.Write(currentInput);
                    if (lengthDiff > 0)
                        Console.Write(new string(' ', lengthDiff));
                    Console.CursorLeft = cursorPosition;
                }
            }

            if (keyInfo.Key != ConsoleKey.UpArrow &&
                keyInfo.Key != ConsoleKey.DownArrow)
                historyIndex = -1;

            switch (keyInfo.Key)
            {
                case ConsoleKey.Enter:
                    if (currentInput.Length != 0 || currentAutoFillInput != null)
                        ProcessCurrentInput(game);
                    return;
                case ConsoleKey.Backspace:
                    if (Console.CursorLeft > 0)
                        RemoveLastInput();
                    return;
                case ConsoleKey.Escape:
                    while (currentInput.Length != 0)
                        RemoveLastInput();
                    return;
                case ConsoleKey.Tab:
                    if (currentInput.Length != 0)
                        AutoFill();
                    return;
                case ConsoleKey.LeftArrow:
                    if (Console.CursorLeft > 0)
                        --Console.CursorLeft;
                    return;
                case ConsoleKey.RightArrow:
                    if (Console.CursorLeft < currentInput.Length)
                        ++Console.CursorLeft;
                    return;
                case ConsoleKey.Home:
                    Console.CursorLeft = 0;
                    return;
                case ConsoleKey.End:
                    Console.CursorLeft = currentInput.Length;
                    return;
                // TODO: History is not working great. Maybe add it later.
                /*case ConsoleKey.UpArrow:
                    if (history.Count != 0)
                        SetHistoryEntry(Math.Min(history.Count - 1, historyIndex + 1));
                    break;
                case ConsoleKey.DownArrow:
                    if (history.Count != 0 && historyIndex != -1)
                        SetHistoryEntry(Math.Max(0, historyIndex - 1));
                    break;*/
            }

            if (keyInfo.KeyChar >= ' ' && keyInfo.KeyChar < 127)
                AddInput(keyInfo.KeyChar);
        }

        static void SetHistoryEntry(int index)
        {
            historyIndex = Math.Min(history.Count - 1, index + 1);
            string entry = history[history.Count - historyIndex - 1];
            int lengthDiff = Math.Max(0, currentInput.Length - entry.Length);
            currentInput = entry;
            Console.CursorLeft = 0;
            Console.Write(entry);
            if (lengthDiff != 0)
                Console.WriteLine(new string(' ', lengthDiff));
        }

        static void RemoveLastInput()
        {
            if (Console.CursorLeft == currentInput.Length)
            {
                currentInput = currentInput.Remove(currentInput.Length - 1);
                Console.Write("\b \b");
            }
            else
            {
                int newCursorPosition = Console.CursorLeft - 1;
                currentInput = currentInput.Remove(newCursorPosition, 1);
                Console.CursorLeft = 0;
                Console.Write(currentInput + " ");
                Console.CursorLeft = newCursorPosition;
            }
        }

        static void AddInput(char input)
        {
            int newCursorPosition = Console.CursorLeft + 1;
            currentInput += input;
            Console.CursorLeft = 0;
            Console.Write(currentInput);
            Console.CursorLeft = newCursorPosition;
        }

        static void AutoFill()
        {
            var possibleCheats = currentInput.Contains(' ') ? null :
                cheats.Where(c => c.Key.StartsWith(currentInput.ToLower())).ToArray();

            if (possibleCheats == null || possibleCheats.Length == 0)
            {
                autoFillIndex = -1;
                currentAutoFillInput = null;
                return;
            }

            int lengthDiff = 0;
            string newCheat = possibleCheats[(++autoFillIndex) % possibleCheats.Length].Key;

            if (currentAutoFillInput == null)
                cursorPosition = Console.CursorLeft;
            else
                lengthDiff = currentAutoFillInput.Length - newCheat.Length;

            currentAutoFillInput = newCheat;
            Console.CursorLeft = 0;
            Console.Write(currentAutoFillInput);
            if (lengthDiff > 0)
                Console.Write(new string(' ', lengthDiff));
        }

        static void ProcessCurrentInput(Game game)
        {
            if (currentAutoFillInput != null)
                currentInput = currentAutoFillInput;

            currentAutoFillInput = null;

            if (!string.IsNullOrWhiteSpace(currentInput))
            {
                var parts = currentInput.Split(' ');

                if (parts.Length != 0)
                {
                    foreach (var cheat in cheats)
                    {
                        if (cheat.Key == parts[0].ToLower())
                        {
                            historyIndex = -1;
                            history.Add(currentInput);
                            currentAutoFillInput = null;
                            currentInput = "";
                            autoFillIndex = -1;
                            cursorPosition = 0;
                            Console.CursorLeft = currentInput.Length;
                            Console.WriteLine();
                            Console.WriteLine();

                            cheat.Value.Value?.Invoke(game, parts.Skip(1).ToArray());
                            return;
                        }
                    }
                }
            }
        }

        static void Help(Game game, string[] args)
        {
            if (args.Length != 0)
            {
                var cheatName = args[0].ToLower();

                if (cheats.TryGetValue(cheatName, out var cheat))
                {
                    Console.WriteLine();
                    Console.WriteLine(cheat.Key);
                    Console.WriteLine();

                    return;
                }
            }

            Console.WriteLine();
            Console.WriteLine("The following cheat commands are available:");

            foreach (var cheat in cheats)
                Console.WriteLine(cheat.Key);

            Console.WriteLine("Type 'help <cheatname>' for more details.");
            Console.WriteLine("Example: help godmode");
            Console.WriteLine();
        }

        static void Godmode(Game game, string[] args)
        {
            bool activate = args.Length == 0 || !int.TryParse(args[0], out int active) || active != 0;

            Console.WriteLine();

            if (activate)
            {
                Console.WriteLine("All party members are now immune to damage.");

                if (!game.Godmode)
                {
                    Console.WriteLine();
                    Console.WriteLine("Robert was here I guess. :)");
                }
            }
            else
            {
                Console.WriteLine("All party members are no longer immune to damage.");

                if (game.Godmode)
                {
                    Console.WriteLine();
                    Console.WriteLine("Robert has gone I guess. :)");
                }
            }

            Console.WriteLine();

            game.Godmode = activate;
        }

        static void Netsrak(Game game, string[] args)
        {
            Console.WriteLine();

            foreach (var partyMember in game.PartyMembers)
            {
                partyMember.HitPoints.CurrentValue = partyMember.HitPoints.TotalMaxValue;
                partyMember.SpellPoints.CurrentValue = partyMember.SpellPoints.TotalMaxValue;

                foreach (var attribute in Enum.GetValues<Data.Attribute>())
                {
                    partyMember.Attributes[attribute].CurrentValue = partyMember.Attributes[attribute].MaxValue;
                }

                foreach (var ability in Enum.GetValues<Ability>())
                {
                    partyMember.Abilities[ability].CurrentValue = partyMember.Abilities[ability].MaxValue;
                }

                game.UpdateCharacterBars();

                partyMember.SpokenLanguages = (Language)0xff;

                switch (partyMember.Class)
                {
                    case Class.Adventurer:
                    case Class.Alchemist:
                        partyMember.LearnedAlchemisticSpells = 0xffffffff;
                        break;
                    case Class.Healer:
                    case Class.Paladin:
                        partyMember.LearnedHealingSpells = 0xffffffff;
                        break;
                    case Class.Ranger:
                    case Class.Mystic:
                        partyMember.LearnedMysticSpells = 0xffffffff;
                        break;
                    case Class.Mage:
                        partyMember.LearnedDestructionSpells = 0xffffffff;
                        break;
                }
            }

            Console.WriteLine("All party members' LP and SP were filled.");
            Console.WriteLine("All their attributes and abilites are maxed.");
            Console.WriteLine("They all speak all languages now.");
            Console.WriteLine("And they learned all spells of their school.");
            Console.WriteLine();
            Console.WriteLine("Karsten was here I guess. :)");
            Console.WriteLine();
        }

        static void ShowMaps(Game game, string[] args)
        {
            Console.WriteLine();

            string pattern = args.Length == 0 || string.IsNullOrWhiteSpace(args[0])
                ? null : args[0].ToLower();
            var maps = pattern == null
                ? new List<Map>(game.MapManager.Maps)
                : game.MapManager.Maps.Where(map => map.Name.ToLower().Contains(pattern)).ToList();
            maps.Sort((a, b) => a.Index.CompareTo(b.Index));

            if (maps.Count <= 12)
            {
                for (int i = 0; i < maps.Count; ++i)
                    Console.WriteLine($"{maps[i].Index:000}: {maps[i].Name}");
            }
            else 
            {
                int halfCount = maps.Count / 2;
                int secondRowOffset = halfCount;

                if (maps.Count % 2 == 1)
                    ++secondRowOffset;

                for (int i = 0; i < halfCount; ++i)
                {
                    Console.Write($"{maps[i].Index:000}: {maps[i].Name}".PadRight(28));
                    Console.WriteLine($"{maps[secondRowOffset + i].Index:000}: {maps[secondRowOffset + i].Name}");
                }

                if (secondRowOffset > halfCount)
                    Console.WriteLine($"{maps[secondRowOffset - 1].Index:000}: {maps[secondRowOffset - 1].Name}");
            }
        }

        static void Teleport(Game game, string[] args)
        {
            Console.WriteLine();

            if (args.Length >= 3)
            {
                var world = Enum.GetValues<World>().Cast<World?>().FirstOrDefault(w =>
                    string.Compare(args[0].Replace(" ", ""), w.ToString().Replace(" ", ""), true) == 0);

                if (world != null)
                {
                    uint? worldX = uint.TryParse(args[1], out uint wx) ? wx : (uint?)null;
                    uint? worldY = uint.TryParse(args[2], out uint wy) ? wy : (uint?)null;
                    var worldDirection = (args.Length > 3 ? ParseDirection(args[3]) : null) ?? (CharacterDirection)(random.Next() % 4);

                    if (worldX == null || worldY == null || worldX == 0 || worldY == 0)
                    {
                        Console.WriteLine("Invalid x or y coordinate.");
                        Console.WriteLine();
                        return;
                    }

                    uint mapColumn = (worldX.Value - 1) / 50;
                    uint mapRow = (worldY.Value - 1) / 50;
                    uint mapX = worldX.Value % 50;
                    uint mapY = worldY.Value % 50;
                    uint worldMapIndex = mapColumn + mapRow * world.Value switch
                    {
                        World.Lyramion => 16u,
                        World.ForestMoon => 6u,
                        World.Morag => 4u,
                        _ => 16u
                    } + world.Value switch
                    {
                        World.Lyramion => 1u,
                        World.ForestMoon => 300u,
                        World.Morag => 513u,
                        _ => 1u
                    };

                    if (!game.Teleport(worldMapIndex, mapX, mapY, worldDirection, out bool blocked))
                    {
                        if (blocked)
                        {
                            Console.WriteLine($"Teleport to position ({worldX}, {worldY}) on world {world} is not possible.");
                        }
                        else
                        {
                            Console.WriteLine("Unable to teleport in current game state.");
                            Console.WriteLine("Try to use the command when no ingame window is open.");
                        }
                        Console.WriteLine();
                        return;
                    }

                    Console.WriteLine($"Teleported to world {world} ({worldX}, {worldY}) -> map {worldMapIndex} ({mapX}, {mapY})");
                    Console.WriteLine();
                    return;
                }
            }

            if (args.Length == 0 || !uint.TryParse(args[0], out uint mapIndex) ||
                !game.MapManager.Maps.Any(m => m.Index == mapIndex))
            {
                Console.WriteLine("Invalid map index.");
                Console.WriteLine("Type 'maps' to see a list of maps.");
                Console.WriteLine();
                return;
            }

            static CharacterDirection? ParseDirection(string input)
            {
                input = input.ToLower();
                if (input == "0" || input == "up")
                    return CharacterDirection.Up;
                else if (input == "1" || input == "right")
                    return CharacterDirection.Right;
                else if (input == "2" || input == "down")
                    return CharacterDirection.Down;
                else if (input == "3" || input == "left")
                    return CharacterDirection.Left;
                else
                    return null;
            }

            var map = game.MapManager.Maps.First(m => m.Index == mapIndex);
            uint? x = args.Length > 1 && uint.TryParse(args[1], out uint ax) ? ax : (uint?)null;
            uint? y = args.Length > 2 && uint.TryParse(args[2], out uint ay) ? ay : (uint?)null;
            var direction = (args.Length > 3 ? ParseDirection(args[3]) : null) ?? (CharacterDirection)(random.Next() % 4);
            bool randomPosition = x == null || y == null;

            if (x == null)
                x = 1u + (uint)random.Next() % (uint)map.Width;
            if (y == null)
                y = 1u + (uint)random.Next() % (uint)map.Height;

            const int MaxTries = 20;
            int tries = 0;

            while (tries++ < MaxTries)
            {
                if (x < 1 || x > map.Width || y < 1 || y > map.Height)
                {
                    if (!randomPosition)
                    {
                        Console.WriteLine($"Teleport to position ({x}, {y}) on map {mapIndex} is not possible.");
                        Console.WriteLine();
                        return;
                    }
                }
                else if (!game.Teleport(mapIndex, x.Value, y.Value, direction, out bool blocked))
                {
                    if (!randomPosition)
                    {
                        if (blocked)
                        {
                            Console.WriteLine($"Teleport to position ({x}, {y}) on map {mapIndex} is not possible.");
                        }
                        else
                        {
                            Console.WriteLine("Unable to teleport in current game state.");
                            Console.WriteLine("Try to use the command when no ingame window is open.");
                        }
                        Console.WriteLine();
                        return;
                    }
                    else if (!blocked)
                    {
                        Console.WriteLine("Unable to teleport in current game state.");
                        Console.WriteLine("Try to use the command when no ingame window is open.");
                        Console.WriteLine();
                        return;
                    }
                }
                else
                {
                    Console.WriteLine($"Teleported to map {mapIndex} ({x}, {y})");
                    Console.WriteLine();
                    return;
                }

                x = 1u + (uint)random.Next() % (uint)map.Width;
                y = 1u + (uint)random.Next() % (uint)map.Height;
            }

            Console.WriteLine($"Teleport failed after testing {MaxTries} random positions.");
            Console.WriteLine();
        }

        static void ShowMonsters(Game game, string[] args)
        {
            Console.WriteLine();

            static string GetMonsterNames(MonsterGroup monsterGroup)
            {
                var monsterNames = new Dictionary<string, int>();

                foreach (var monster in monsterGroup.Monsters)
                {
                    if (monster != null)
                    {
                        if (!monsterNames.ContainsKey(monster.Name))
                            monsterNames[monster.Name] = 1;
                        else
                            ++monsterNames[monster.Name];
                    }
                }

                return string.Join(", ", monsterNames.Select(m => $"{m.Value}x{m.Key}"));
            }

            foreach (var monsterGroup in game.CharacterManager.MonsterGroups)
            {
                Console.WriteLine($"{monsterGroup.Key:000}: {GetMonsterNames(monsterGroup.Value)}");
            }
        }

        static void StartBattle(Game game, string[] args)
        {
            Console.WriteLine();

            if (args.Length == 0 || !uint.TryParse(args[0], out uint monsterGroupIndex) ||
                !game.CharacterManager.MonsterGroups.ContainsKey(monsterGroupIndex))
            {
                Console.WriteLine("Invalid monster group index.");
                Console.WriteLine("Type 'monsters' to see a list of monster groups.");
                Console.WriteLine();
                return;
            }

            if (!game.StartBattle(monsterGroupIndex))
            {
                Console.WriteLine("Unable to start a fight in current game state.");
                Console.WriteLine("Try to use the command when no ingame window is open.");
                Console.WriteLine();
                return;
            }
        }

        static void ShowItems(Game game, string[] args)
        {
            Console.WriteLine();

            var items = new List<Item>(game.ItemManager.Items);
            items.Sort((a, b) => a.Index.CompareTo(b.Index));
            int halfCount = items.Count / 2;
            int secondRowOffset = halfCount;

            if (items.Count % 2 == 1)
                ++secondRowOffset;

            for (int i = 0; i < halfCount; ++i)
            {
                Console.Write($"{items[i].Index:000}: {items[i].Name}".PadRight(30));
                Console.WriteLine($"{items[secondRowOffset + i].Index:000}: {items[secondRowOffset + i].Name}");
            }

            if (secondRowOffset > halfCount)
                Console.WriteLine($"{items[secondRowOffset - 1].Index:000}: {items[secondRowOffset - 1].Name}");
        }

        static void GiveItem(Game game, string[] args)
        {
            Console.WriteLine();

            if (args.Length == 0 || !uint.TryParse(args[0], out uint itemIndex) ||
                !game.ItemManager.Items.Any(item => item.Index == itemIndex))
            {
                Console.WriteLine("Invalid item index.");
                Console.WriteLine("Type 'items' to see a list of items.");
                Console.WriteLine();
                return;
            }

            int amount = args.Length < 2 ? 1 : int.TryParse(args[1], out int n) ? n : -1;

            if (amount < 1)
            {
                Console.WriteLine("Item amount was invalid or below 1.");
                Console.WriteLine();
                return;
            }

            if (amount > 99)
            {
                Console.WriteLine("Item amount must not be greater than 99.");
                Console.WriteLine();
                return;
            }

            int? partyMemberIndex = args.Length < 3 ? (int?)null : int.TryParse(args[2], out int i) ? i : -1;

            if (partyMemberIndex != null && (partyMemberIndex < 1 || partyMemberIndex > Game.MaxPartyMembers))
            {
                Console.WriteLine("Party member index was invalid or outside the range 1~6.");
                Console.WriteLine();
                return;
            }

            var partyMember = partyMemberIndex == null ? game.CurrentPartyMember : game.GetPartyMember(partyMemberIndex.Value);

            if (partyMember == null)
            {
                Console.WriteLine($"Party member with index {partyMemberIndex} does not exist.");
                Console.WriteLine();
                return;
            }

            var inventorySlots = partyMember.Inventory.Slots;
            var item = game.ItemManager.GetItem(itemIndex);
            int remainingAmount = amount;
            bool stackable = false;

            if (item.Flags.HasFlag(ItemFlags.Stackable))
            {
                stackable = true;

                foreach (var slot in inventorySlots.Where(s => s.ItemIndex == itemIndex && s.Amount < 99))
                {
                    int addAmount = Math.Min(remainingAmount, 99 - slot.Amount);
                    slot.Amount += addAmount;
                    slot.NumRemainingCharges = Math.Max(slot.NumRemainingCharges, 1);
                    remainingAmount -= addAmount;
                    partyMember.TotalWeight += (uint)addAmount * item.Weight;

                    if (remainingAmount == 0)
                        break;
                }
            }

            foreach (var slot in inventorySlots.Where(s => s.Empty))
            {
                int addAmount = Math.Min(remainingAmount, stackable ? 99 : 1);
                slot.ItemIndex = itemIndex;
                slot.Amount = addAmount;
                slot.NumRemainingCharges = Math.Max(slot.NumRemainingCharges, 1);
                remainingAmount -= addAmount;
                partyMember.TotalWeight += (uint)addAmount * item.Weight;

                if (remainingAmount == 0)
                    break;
            }

            if (remainingAmount == amount)
            {
                Console.WriteLine("There was no space to add the items.");
                Console.WriteLine();
            }
            else if (remainingAmount != 0)
            {
                game.UpdateInventory();
                Console.WriteLine($"Only {amount - remainingAmount}/{amount} items could be added.");
                Console.WriteLine();
            }
            else
            {
                game.UpdateInventory();
                if (amount == 1)
                    Console.WriteLine("The item was added successfully.");
                else
                    Console.WriteLine($"All {amount} items were added successfully.");
                Console.WriteLine();
            }
        }

        static void Fly(Game game, string[] args)
        {
            Console.WriteLine();

            if (game.ActivateTransport(Data.Enumerations.TravelType.Fly))
            {
                Console.WriteLine("You are now flying! Awesome!");
            }
            else
            {
                Console.WriteLine("You can't fly now.");
            }

            Console.WriteLine();
        }
    }
}
