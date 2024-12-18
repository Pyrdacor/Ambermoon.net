using Ambermoon.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Ambermoon;

class Cheats
{
    static readonly System.Random random = new System.Random(DateTime.Now.Millisecond);

    static readonly Dictionary<string, KeyValuePair<string, Action<Game, string[]>>> cheats = new()
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
                "Makes the party invulnerable and kills enemies immediately." + Environment.NewLine +
                "Usage: godmode [0/1]",
                Godmode
            )
        },
        { "noclip",
            Create
            (
                "Allows moving through walls in 3D." + Environment.NewLine +
                "Usage: noclip [0/1]",
                NoClip
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
                "Teleports the group to a specific map." + Environment.NewLine +
                "Usage: teleport <map_id> [x] [y] [direction]" + Environment.NewLine +
                "   or: teleport <world> <x> <y> [direction]" + Environment.NewLine +
                "Worlds: lyramion, forestmoon, morag",
                Teleport
            )
        },
        { "monsters",
            Create
            (
                "Shows a list of all available monster groups." + Environment.NewLine +
                "Usage: monsters" + Environment.NewLine +
                "Usage: monsters <partial_name>",
                ShowMonsters
            )
        },
        { "members",
            Create
            (
                "Shows a list of all available party members." + Environment.NewLine +
                "Usage: members" + Environment.NewLine +
                "Usage: members <partial_name>",
                ShowPartyMembers
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
                "Usage: items" + Environment.NewLine +
                "Usage: items <partial_name>",
                ShowItems
            )
        },
        { "give",
            Create
            (
                "Gives an item, gold or food to a party member." + Environment.NewLine +
                "Usage: give <item_index> [amount] [party_member_index]" + Environment.NewLine +
                "Usage: give <partial_name> [amount] [party_member_index]" + Environment.NewLine +
                "Usage: give gold <amount> [party_member_index]" + Environment.NewLine +
                "Usage: give food <amount> [party_member_index]",
                Give
            )
        },
        { "fly",
            Create
            (
                "Let the party fly on world maps." + Environment.NewLine +
                "Usage: fly",
                Fly
            )
        },
        { "explore",
            Create
            (
                "Explores the whole dungeon map." + Environment.NewLine +
                "Usage: explore",
                Explore
            )
        },
        { "kill",
            Create
            (
                "Kills a specific party member." + Environment.NewLine +
                "Usage: kill [party_member_index] [death_type]" + Environment.NewLine +
					"Usage: kill <partial_party_member_name> [death_type]" + Environment.NewLine +
					"Death types: 0 (normal), 1 (ashes), 2 (dust)" + Environment.NewLine +
                "Defaults to active party member and death type 0",
                Kill
            )
        },
        { "revive",
            Create
            (
                "Revives a specific party member." + Environment.NewLine +
                "Usage: revive [party_member_index]" + Environment.NewLine +
                "If no index is given, all dead ones are revived.",
                Revive
            )
        },
        { "berserk",
            Create
            (
                "Kills all monsters on the map." + Environment.NewLine +
                "Usage: berserk",
                Berserk
            )
        },
        { "win",
            Create
            (
                "Wins the current fight immediately." + Environment.NewLine +
                "Usage: win",
                Win
            )
        },
        { "flee",
            Create
            (
                "Flees the current fight immediately." + Environment.NewLine +
                "Usage: flee",
                Flee
            )
        },
        { "light",
            Create
            (
                "Gives the party light." + Environment.NewLine +
                "Usage: light [light_level]" + Environment.NewLine +
                "Levels 1 to 3 are possible." + Environment.NewLine +
                "Defaults to max light level",
                Light
            )
        },
        { "where",
            Create
            (
                "Tells the current location." + Environment.NewLine +
                "Usage: where" + Environment.NewLine,
                Where
            )
        },
        { "level",
            Create
            (
                "Increases the level of one or all party members." + Environment.NewLine +
                "Usage: level [amount] [party_member_index]" + Environment.NewLine +
                "Use party member index 0 to increase for all party members." + Environment.NewLine +
                "If you omit the party member index, the selected one gets the levels.",
                Level
            )
        },
        { "invite",
            Create
            (
                "Invites a party member into the party." + Environment.NewLine +
                "Usage: invite <party_member_id | party_member_name>" + Environment.NewLine + Environment.NewLine +
                "Note: For 'Tar the dark' you can just type Tar." + Environment.NewLine +
                "Note: You can also only type the first few letters like 'Sab' for 'Sabine'.",
                Invite
            )
        },
        { "curse",
            Create
            (
                "Applies a condition onto one or all party members." + Environment.NewLine +
                "Usage: curse <condition> [party_member_index]" + Environment.NewLine +
                "Use party member index 0 to apply to all party members." + Environment.NewLine +
                "If you omit the party member index, the selected one is the target.",
                Curse
            )
        },
        { "cleanse",
            Create
            (
                "Cleanses the given condition from one or all party members." + Environment.NewLine +
                "Usage: cleanse [condition] [party_member_index]" + Environment.NewLine +
                "Use party member index 0 to cleanse all party members." + Environment.NewLine +
                "If you omit the party member index, the selected one is the target." + Environment.NewLine +
                "Use condition 'all' or omit it to cleanse all conditions.",
                Cleanse
            )
        },
    };

    static KeyValuePair<string, Action<Game, string[]>> Create(string help, Action<Game, string[]> action)
        => KeyValuePair.Create(help, action);

    static string currentAutoFillInput = null;
    static string currentInput = "";
    static int autoFillIndex = -1;
    static int cursorPosition = 0;
    static int historyIndex = -1;
    static readonly List<string> history = new();
    static List<ConsoleKeyInfo> trackedKeys = new();
    static bool inputDisabled = false;

    public static void ProcessInput(string input, Game game)
    {
        if (inputDisabled)
            return;

        currentAutoFillInput = null;
        currentInput = input;
        ProcessCurrentInput(game, true);
    }

    public static void ProcessInput(ConsoleKeyInfo keyInfo, Game game)
    {
        if (inputDisabled)
        {
            lock (trackedKeys)
            {
                trackedKeys.Add(keyInfo);
                return;
            }
        }

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
                    ProcessCurrentInput(game, false);
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
            case ConsoleKey.UpArrow:
                if (history.Count != 0)
                    SetHistoryEntry(Util.Limit(0, historyIndex + 1, history.Count - 1));
                break;
            case ConsoleKey.DownArrow:
                if (history.Count != 0 && historyIndex != -1)
                    SetHistoryEntry(Util.Limit(-1, historyIndex - 1, history.Count - 1));
                break;
        }

        if ((keyInfo.KeyChar >= ' ' && keyInfo.KeyChar < 127) ||
            keyInfo.KeyChar == 'ä' || keyInfo.KeyChar == 'Ä' ||
            keyInfo.KeyChar == 'ö' || keyInfo.KeyChar == 'Ö' ||
            keyInfo.KeyChar == 'ü' || keyInfo.KeyChar == 'Ü')
            AddInput(keyInfo.KeyChar);
    }

    static void SetHistoryEntry(int index)
    {
        int oldIndex = historyIndex;
        historyIndex = Util.Limit(-1, index, history.Count - 1);
        if (historyIndex == oldIndex)
            return;
        string entry = historyIndex == -1 ? "" : history[history.Count - historyIndex - 1];
        int lengthDiff = Math.Max(0, currentInput.Length - entry.Length);
        currentInput = entry;
        Console.CursorLeft = 0;
        Console.Write(entry);
        if (lengthDiff != 0)
            Console.Write(new string(' ', lengthDiff));
        Console.CursorLeft = entry.Length;
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
        {
            Console.Write(new string(' ', lengthDiff));
            Console.CursorLeft = currentAutoFillInput.Length;
        }
    }

    static void ProcessCurrentInput(Game game, bool redirectedInput)
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
                        history.Remove(currentInput);
                        history.Add(currentInput);
                        currentAutoFillInput = null;
                        currentInput = "";
                        autoFillIndex = -1;
                        cursorPosition = 0;
                        if (!redirectedInput)
                        {
                            Console.CursorLeft = currentInput.Length;
                            Console.WriteLine();
                        }
                        Console.WriteLine();
                        cheat.Value.Value?.Invoke(game, parts.Skip(1).ToArray());
                        Console.WriteLine();
                        return;
                    }
                }

                historyIndex = -1;
                currentAutoFillInput = null;
                currentInput = "";
                autoFillIndex = -1;
                cursorPosition = 0;
                if (!redirectedInput)
                {
                    Console.CursorLeft = currentInput.Length;
                    Console.WriteLine();
                }
                Console.WriteLine();
                Console.WriteLine("Invalid cheat command. Type 'help' for a list of commands.");
                Console.WriteLine();
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

        Console.WriteLine();
        Console.WriteLine("Type 'help <cheatname>' for more details.");
        Console.WriteLine("Example: help godmode");
        Console.WriteLine();
    }

    static void Godmode(Game game, string[] args)
    {
        bool activate = args.Length == 0 ? !game.Godmode : !int.TryParse(args[0], out int active) || active != 0;

        Console.WriteLine();

        if (activate && !game.Godmode)
        {
            Console.WriteLine("All party members are now immune to damage and kill instantly.");
            Console.WriteLine();
            Console.WriteLine("Robert was here I guess. :)");
        }
        else if (!activate && game.Godmode)
        {
            Console.WriteLine("All party members are no longer immune to damage and deal normal damage.");
            Console.WriteLine();
            Console.WriteLine("Robert has gone I guess. :)");
        }

        Console.WriteLine();

        game.Godmode = activate;
    }

    static void NoClip(Game game, string[] args)
    {
        bool activate = args.Length == 0 ? !game.NoClip : !int.TryParse(args[0], out int active) || active != 0;

        Console.WriteLine();

        if (activate && !game.NoClip)
        {
            Console.WriteLine("You can now move through walls in 3D.");
        }
        else if (!activate && game.NoClip)
        {
            Console.WriteLine("You can no longer move through walls in 3D.");
        }

        Console.WriteLine();

        game.NoClip = activate;
    }

    static void Netsrak(Game game, string[] args)
    {
        Console.WriteLine();

        foreach (var partyMember in game.PartyMembers)
        {
            partyMember.HitPoints.CurrentValue = partyMember.HitPoints.TotalMaxValue;
            partyMember.SpellPoints.CurrentValue = partyMember.SpellPoints.TotalMaxValue;

            foreach (var attribute in EnumHelper.GetValues<Data.Attribute>())
            {
                if (attribute < Data.Attribute.Age)
                    partyMember.Attributes[attribute].CurrentValue = partyMember.Attributes[attribute].MaxValue;
            }

            foreach (var skill in EnumHelper.GetValues<Skill>())
            {
                partyMember.Skills[skill].CurrentValue = partyMember.Skills[skill].MaxValue;
            }

            game.UpdateCharacterBars();

            partyMember.SpokenLanguages = (Language)0xff;

            switch (partyMember.Class)
            {
                case Class.Adventurer:
                case Class.Alchemist:
                    partyMember.LearnedAlchemisticSpells = game.Features.HasFlag(Data.Enumerations.Features.AdvancedSpells) ? 0x7ffffffeu : 0x3ffffffeu;
                    break;
                case Class.Healer:
                case Class.Paladin:
                    partyMember.LearnedHealingSpells = 0x7ffffffeu;
                    break;
                case Class.Ranger:
                case Class.Mystic:
                    partyMember.LearnedMysticSpells = game.Features.HasFlag(Data.Enumerations.Features.AdvancedSpells) ? 0x7ffffffeu : 0x0003fffe;
                    break;
                case Class.Mage:
                    partyMember.LearnedDestructionSpells = 0x7ffffffeu;
                    break;
            }
        }

        Console.WriteLine("All party members' LP and SP were filled.");
        Console.WriteLine("All their attributes and skills are maxed.");
        Console.WriteLine("They all speak all languages now.");
        Console.WriteLine("And they learned all spells of their school.");
        Console.WriteLine();
        Console.WriteLine("Karsten was here I guess. :)");
        Console.WriteLine();
    }

    static List<T> Filter<T>(string[] args, IEnumerable<T> list, Func<T, string> nameProvider)
    {
        string pattern = args.Length == 0 || string.IsNullOrWhiteSpace(args[0])
            ? null : args[0].ToLower();
        return pattern == null
            ? new List<T>(list)
            : list.Where(item => nameProvider(item).ToLower().Contains(pattern)).ToList();
    }

    static void ShowList<T>(string[] args, IEnumerable<T> list, Func<T, string> nameProvider,
        Func<T, uint> indexProvider, bool twoRows = true)
    {
        Console.WriteLine();

        var items = Filter(args, list, nameProvider);

        if (items.Count == 0)
        {
            Console.WriteLine("No items found.");
            return;
        }

        items.Sort((a, b) => indexProvider(a).CompareTo(indexProvider(b)));

        if (!twoRows || items.Count <= 12)
        {
            for (int i = 0; i < items.Count; ++i)
                Console.WriteLine($"{indexProvider(items[i]):000}: {nameProvider(items[i])}");
        }
        else
        {
            int halfCount = items.Count / 2;
            int secondRowOffset = halfCount;

            if (items.Count % 2 == 1)
                ++secondRowOffset;

            for (int i = 0; i < halfCount; ++i)
            {
                Console.Write($"{indexProvider(items[i]):000}: {nameProvider(items[i])}".PadRight(28));
                Console.WriteLine($"{indexProvider(items[secondRowOffset + i]):000}: {nameProvider(items[secondRowOffset + i])}");
            }

            if (secondRowOffset > halfCount)
                Console.WriteLine($"{indexProvider(items[secondRowOffset - 1]):000}: {nameProvider(items[secondRowOffset - 1])}");
        }
    }

    static void ShowMaps(Game game, string[] args)
    {
        ShowList(args, game.MapManager.Maps, map => map.Name, map => map.Index);
    }

    static void Teleport(Game game, string[] args)
    {
        Console.WriteLine();

        if (args.Length >= 3)
        {
            var world = EnumHelper.GetValues<World>().Cast<World?>().FirstOrDefault(w =>
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
                uint mapX = 1 + (worldX.Value - 1) % 50;
                uint mapY = 1 + (worldY.Value - 1) % 50;
                uint worldSize = world.Value switch
                {
                    World.Lyramion => 16u,
                    World.ForestMoon => 6u,
                    World.Morag => 4u,
                    _ => 16u
                };
                mapColumn %= worldSize;
                mapRow %= worldSize;
                uint worldMapOffset = world.Value switch
                {
                    World.Lyramion => 1u,
                    World.ForestMoon => 300u,
                    World.Morag => 513u,
                    _ => 1u
                };
                uint worldMapIndex = worldMapOffset + mapColumn + mapRow * worldSize;
                uint mapAmount = worldSize * worldSize;

                if (!game.Teleport(worldMapIndex, mapX, mapY, worldDirection, out bool blocked))
                {
                    if (blocked)
                    {
                        Console.WriteLine($"Teleport to position ({worldX}, {worldY}) on world {world} is not possible.");
                    }
                    else
                    {
                        Console.WriteLine("Unable to teleport in current game state.");
                        Console.WriteLine("Try to use the command when no ingame window is open and you are on foot.");
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
                        Console.WriteLine("Try to use the command when no ingame window is open and you are on foot.");
                    }
                    Console.WriteLine();
                    return;
                }
                else if (!blocked)
                {
                    Console.WriteLine("Unable to teleport in current game state.");
                    Console.WriteLine("Try to use the command when no ingame window is open and you are on foot.");
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
        var monsterGroups = game.CharacterManager.MonsterGroups.ToList();

        ShowList(args, monsterGroups, g => GetMonsterNames(g.Value), g => g.Key, false);

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
    }

    static void ShowPartyMembers(Game game, string[] args)
    {
        var partyMembers = game.GetCurrentSavegame().PartyMembers;

        ShowList(args, partyMembers, p => p.Value.Name, p => p.Key, false);
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
        ShowList(args, game.ItemManager.Items, item => item.Name, item => item.Index);
    }

    static void Give(Game game, string[] args)
    {
        Console.WriteLine();

        PartyMember GetPartyMember(int argIndex)
        {
            int? partyMemberIndex = args.Length < argIndex + 1 ? (int?)null : int.TryParse(args[argIndex], out int i) ? i : -1;

            if (partyMemberIndex != null && (partyMemberIndex < 1 || partyMemberIndex > Game.MaxPartyMembers))
            {
                Console.WriteLine("Party member index was invalid or outside the range 1~6.");
                Console.WriteLine();
                return null;
            }

            var partyMember = partyMemberIndex == null ? game.CurrentPartyMember : game.GetPartyMember(partyMemberIndex.Value - 1);

            if (partyMember == null)
            {
                Console.WriteLine($"Party member with index {partyMemberIndex} does not exist.");
                Console.WriteLine();
                return null;
            }

            return partyMember;
        }

        PartyMember partyMember = null;

        if (args.Length >= 2)
        {
            switch (args[0].ToLower())
            {
                case "gold":
                    if ((partyMember = GetPartyMember(2)) == null)
                        return;
                    if (int.TryParse(args[1], out int gold) || gold < 1)
                    {
                        partyMember.AddGold((uint)gold);
                        Console.WriteLine($"{gold} gold was added.");
                    }
                    else
                    {
                        Console.WriteLine("Invalid gold amount.");
                    }
                    Console.WriteLine();
                    return;
                case "food":
                    if ((partyMember = GetPartyMember(2)) == null)
                        return;
                    if (int.TryParse(args[1], out int food) || food < 1)
                    {
                        partyMember.AddFood((uint)food);
                        Console.WriteLine($"{food} food was added.");
                    }
                    else
                    {
                        Console.WriteLine("Invalid food amount.");
                    }
                    Console.WriteLine();
                    return;
            }
        }

        uint itemIndex = 0;
        int amountIndex = 1;

        if (args[0].Length > 0 && !char.IsDigit(args[0][0]))
        {
            var nameArgs = args.TakeWhile(a => !char.IsDigit(a[0]));
            string searchString = string.Join(" ", nameArgs).ToLower();
            var items = game.ItemManager.Items.Where(item => item.Name.ToLower().Contains(searchString)).ToList();

            if (items.Count == 0)
            {
                Console.WriteLine("No item was found with that name.");
                Console.WriteLine("Type 'items' to see a list of items.");
                Console.WriteLine();
                return;
            }
            else if (items.Count > 1)
            {
                var exactItem = items.FirstOrDefault(item => item.Name.ToLower() == searchString);

                if (exactItem != null)
                {
                    itemIndex = exactItem.Index;
                    amountIndex = nameArgs.Count();

                    if ((partyMember = GetPartyMember(amountIndex + 1)) == null)
                        return;
                }
                else
                {
                    Console.WriteLine("The following items match your search.");
                    Console.WriteLine("Use the index or be more precise with the name.");
                    Console.WriteLine();
                    ShowList(Array.Empty<string>(), items, item => item.Name, item => item.Index);
                    return;
                }
            }
            else
            {
                itemIndex = items[0].Index;
                amountIndex = nameArgs.Count();

                if ((partyMember = GetPartyMember(amountIndex + 1)) == null)
                    return;
            }
        }
        else
        {
            if (args.Length == 0 || !uint.TryParse(args[0], out itemIndex) ||
                !game.ItemManager.Items.Any(item => item.Index == itemIndex))
            {
                Console.WriteLine("Invalid item index.");
                Console.WriteLine("Type 'items' to see a list of items.");
                Console.WriteLine();
                return;
            }

            if ((partyMember = GetPartyMember(2)) == null)
                return;
        }

        int amount = args.Length < amountIndex + 1 ? 1 : int.TryParse(args[amountIndex], out int n) ? n : -1;

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
                slot.NumRemainingCharges = Util.Max(slot.NumRemainingCharges, 1, item.InitialCharges);
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
            slot.NumRemainingCharges = Util.Max(slot.NumRemainingCharges, 1, item.InitialCharges);
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

    static void Explore(Game game, string[] args)
    {
        Console.WriteLine();

        if (game.ExploreMap())
        {
            Console.WriteLine("The map has been explored!");
        }
        else
        {
            Console.WriteLine("You can't explore the map now.");
        }

        Console.WriteLine();
    }

    static PartyMember GetPartyMemberByIdOrSlotOrName(Game game, string partyMemberIdOrName,
        Func<PartyMember, bool> filter, Func<PartyMember> afterFilterEmptyHandler,
        Func<PartyMember[], PartyMember> afterFilterMoreThanOnceHandler, bool numberIsSlot)
    {
		PartyMember partyMember = null;

		if (uint.TryParse(partyMemberIdOrName, out uint partyMemberId))
		{
            if (numberIsSlot)
            {
                if (partyMemberId < 1 || partyMemberId > Game.MaxPartyMembers)
                {
                    Console.WriteLine("Party member slot was outside the range 1~6.");
                    Console.WriteLine();
                    return null;
                }

                partyMember = game.GetPartyMember((int)partyMemberId - 1);
            }
            else
            {
                var entries = game.GetCurrentSavegame().PartyMembers;

                if (!entries.TryGetValue(partyMemberId, out partyMember))
                {
                    Console.WriteLine($"The given party member id does not exist. Use a value from {entries.Keys.Min()} to {entries.Keys.Max()}.");
                    Console.WriteLine();
                    return null;
                }
            }

			if (!filter(partyMember))
			{
                return afterFilterEmptyHandler();
			}
		}
		else
		{
			string name = partyMemberIdOrName.ToLower();

			var partyMembers = game.GetCurrentSavegame().PartyMembers.Values.Where(p => p.Name.ToLower().StartsWith(name)).ToArray();

            if (partyMembers.Length == 0)
			{
                Console.WriteLine("No party member matches the given name.");
				Console.WriteLine();
				return null;
			}

			partyMembers = partyMembers.Where(filter).ToArray();

            if (partyMembers.Length > 0 && name == "tar")
            {
                // There is a problem when entering "Tar" as it will match
                // "Tar the dark" and "Targor". But most likey you mean Tar.
                // So we just take the first one which is Tar.
                partyMembers = partyMembers.Take(1).ToArray();
            }

			if (partyMembers.Length == 0)
			{
				return afterFilterEmptyHandler();
			}
			else if (partyMembers.Length > 1)
			{
                partyMember = afterFilterMoreThanOnceHandler(partyMembers);

                if (partyMember != null)
                    return partyMember;

				Console.WriteLine("More than one party member matches the given name.");
				Console.WriteLine("Please specify more precise. Here are the matches:");
				foreach (var p in partyMembers.Where(x => x.Index != 1))
					Console.WriteLine("  - " + p.Name);
				Console.WriteLine();
				return null;
			}
			else
			{
				partyMember = partyMembers[0];
			}
		}

        return partyMember;
	}

    static void Kill(Game game, string[] args)
    {
        Console.WriteLine();

        if ((!game.BattleActive && !game.CampActive && game.WindowActive) || game.PopupActive)
        {
            Console.WriteLine("Killing a party member is only possible on the map screen, in battle or in camp.");
            Console.WriteLine("Moreover all popups should be closed.");
            Console.WriteLine();
            return;
        }

        if (game.BattleRoundActive)
        {
            Console.WriteLine("Killing party members in battle is only possible between rounds.");
            Console.WriteLine();
            return;
        }

        if (game.PlayerIsPickingABattleAction)
        {
            Console.WriteLine("Please finish the current battle action picking first.");
            Console.WriteLine();
            return;
        }

			var partyMember = args.Length == 0 ? game.CurrentPartyMember : GetPartyMemberByIdOrSlotOrName(game, args[0], p => game.PartyMembers.Contains(p),
            () =>
            {
                if (long.TryParse(args[0], out var index))
					    Console.WriteLine($"There is no party member in slot {index}.");
                else
						Console.WriteLine($"There is no party member with a matching name in the party.");

					Console.WriteLine();
					return null;
				},
            (_) => null, true);

        if (partyMember == null)
            return;

        int deathType = args.Length < 2 ? 0 : int.TryParse(args[1], out int t) ? t : 0;
        var deathCondition = deathType switch
        {
            1 => Condition.DeadAshes,
            2 => Condition.DeadDust,
            _ => Condition.DeadCorpse
        };

        if (!partyMember.Alive)
        {
            Console.WriteLine($"{partyMember.Name} is already dead.");
            Console.WriteLine();

            if (!partyMember.Conditions.HasFlag(deathCondition))
            {
                if (deathCondition == Condition.DeadDust)
                {
                    partyMember.Conditions = Condition.DeadDust;
                    Console.WriteLine("But his death type was changed to dust.");
                    Console.WriteLine();
                }
                else if (deathCondition == Condition.DeadAshes && !partyMember.Conditions.HasFlag(Condition.DeadDust))
                {
                    partyMember.Conditions = Condition.DeadAshes;
                    Console.WriteLine("But his death type was changed to ashes.");
                    Console.WriteLine();
                }
            }

            return;
        }

        if (game.HasPartyMemberFled(partyMember))
        {
            Console.WriteLine($"{partyMember.Name} has fled the fight. Please try again after the fight has ended.");
            Console.WriteLine();
            return;
        }

        bool wasActive = game.CurrentPartyMember == partyMember;

        void Died(Character _)
        {
            partyMember.Died -= Died;
            Console.WriteLine($"{partyMember.Name} was killed!");
            Console.WriteLine();

            if (wasActive)
                game.RecheckActivePlayer();
        }

        partyMember.Died += Died;

        game.KillPartyMember(partyMember, deathCondition);
    }

    static void Revive(Game game, string[] args)
    {
        Console.WriteLine();

        if (!game.CanRevive())
        {
            Console.WriteLine("Can't revive outside the camp.");
            Console.WriteLine();
            return;
        }

        var partyMembers = args.Length == 0 ? game.PartyMembers.ToList() : new List<PartyMember>
        {
            GetPartyMemberByIdOrSlotOrName(game, args[0], p => game.PartyMembers.Contains(p),
                () =>
                {
                    if (long.TryParse(args[0], out var index))
                        Console.WriteLine($"There is no party member in slot {index}.");
                    else
                        Console.WriteLine($"There is no party member with a matching name in the party.");

                    Console.WriteLine();
                    return null;
                },
                (_) => null, true)
        };

        if (partyMembers.Count == 0 || partyMembers.First() == null)
            return;

        if (partyMembers.Count > 1)
        {
            partyMembers = partyMembers.Where(p => !p.Alive).ToList();

            if (partyMembers.Count == 0)
            {
                Console.WriteLine("All party members are already alive.");
                Console.WriteLine();
                return;
            }
        }

        if (partyMembers.Count == 1 && partyMembers[0].Alive)
        {
            Console.WriteLine($"{partyMembers[0].Name} is not dead.");
            Console.WriteLine();
            return;
        }

        inputDisabled = true;
        Console.WriteLine("Reviving party members ... ");

        game.Revive(null, partyMembers, () =>
        {
            if (partyMembers.Count == 1)
                Console.WriteLine($"{partyMembers[0].Name} was revived.");
            else
                Console.WriteLine($"{string.Join(", ", partyMembers.Select(p => p.Name))} were revived");
            Console.WriteLine();

            lock (trackedKeys)
            {
                inputDisabled = false;
                trackedKeys.ForEach(key => ProcessInput(key, game));
            }
        });
    }

    static void Berserk(Game game, string[] args)
    {
        Console.WriteLine();

        game.KillAllMapMonsters();

        Console.WriteLine("The map was cleared from all monsters.");
        Console.WriteLine();
    }

    static void EndFight(Game game, bool flee)
    {
        Console.WriteLine();

        if (!game.EndBattle(flee))
        {
            Console.WriteLine("There is no active fight, or the fight round is still active or another window is opened.");
            Console.WriteLine();
        }
    }

    static void Win(Game game, string[] args) => EndFight(game, false);

    static void Flee(Game game, string[] args) => EndFight(game, true);

    static void Light(Game game, string[] args)
    {
        Console.WriteLine();

        uint lightLevel = args.Length < 1 ? 3 : uint.TryParse(args[0], out uint i) ? i : 3;
        lightLevel = Util.Limit(1, lightLevel, 3);
        game.ActivateLight(lightLevel);

        Console.WriteLine($"Light level {lightLevel} was granted.");
        Console.WriteLine();
    }

    static void Where(Game game, string[] args)
    {
        Console.WriteLine();

        var savegame = game.GetCurrentSavegame();

        Console.WriteLine($"Map {savegame.CurrentMapIndex}, X {savegame.CurrentMapX}, Y {savegame.CurrentMapY}, Looking {savegame.CharacterDirection}");
        Console.WriteLine();
    }

    static void Level(Game game, string[] args)
    {
        Console.WriteLine();

        if (game.WindowOrPopupActive)
        {
            Console.WriteLine("Please close all popups and ensure you are on the map screen.");
            Console.WriteLine("Otherwise the level cheat won't work.");
            Console.WriteLine();
            return;
        }

        PartyMember[] GetPartyMembers(int argIndex)
        {
            int? partyMemberIndex = args.Length < argIndex + 1 ? (int?)null : int.TryParse(args[argIndex], out int i) ? i : -1;

            if (partyMemberIndex != null && partyMemberIndex > Game.MaxPartyMembers)
            {
                Console.WriteLine("Party member index was invalid or outside the range 0~6.");
                Console.WriteLine();
                return null;
            }

            if (partyMemberIndex == 0)
            {
                return game.PartyMembers.ToArray();
            }

            var partyMember = partyMemberIndex == null ? game.CurrentPartyMember : game.GetPartyMember(partyMemberIndex.Value - 1);

            if (partyMember == null)
            {
                Console.WriteLine($"Party member with index {partyMemberIndex} does not exist.");
                Console.WriteLine();
                return null;
            }

            return new PartyMember[1] { partyMember };
        }

        int amount = args.Length < 1 ? 1 : int.TryParse(args[0], out int n) ? n : -1;

        if (amount < 1)
        {
            Console.WriteLine("Amount was invalid or below 1.");
            Console.WriteLine();
            return;
        }

        var partyMembers = GetPartyMembers(1).Where(p => p.Alive).ToList();

        if (partyMembers.Count == 0)
        {
            Console.WriteLine("There is no alive target party member.");
            Console.WriteLine();
            return;
        }

        partyMembers = partyMembers.Where(p => p.Level < 50).ToList();

        if (partyMembers.Count == 0)
        {
            Console.WriteLine("There is no target party member below max level.");
            Console.WriteLine();
            return;
        }

        void Finish()
        {
            Console.WriteLine("Levels were increased.");
            Console.WriteLine();

            game.UpdateInventory();
        }

        int partyMemberCount = partyMembers.Count;
        Action[] actions = null;
        actions = partyMembers.Select((partyMember, index) => new Action
        (
            () => {
                int currentLevel = partyMember.Level;
                int targetLevel = Math.Min(50, currentLevel + amount);
                uint exp;

                do
                {
                    exp = partyMember.GetNextLevelExperiencePoints(game.Features);
                    ++partyMember.Level;
                }
                while (partyMember.Level < targetLevel);

                partyMember.Level = (byte)currentLevel;

                game.AddExperience(partyMember, exp - partyMember.ExperiencePoints, index == partyMemberCount - 1 ? Finish : actions[index + 1]);
            }
        )).ToArray();

        actions[0]();
    }

    static void Invite(Game game, string[] args)
    {
        Console.WriteLine();

        if (args.Length < 1)
        {
            Console.WriteLine("No party member id or name was given.");
            Console.WriteLine();
            return;
        }

        var partyMember = GetPartyMemberByIdOrSlotOrName(game, args[0], p => !game.PartyMembers.Contains(p),
            () =>
            {
					Console.WriteLine("All party members matching the given name are already in the party.");
					Console.WriteLine();
                return null;
				},
            (partyMembers) =>
            {
					if (partyMembers.Length == 2 && partyMembers[0].Index == 1)
					{
						// If the name matches 2 party members and one of them is the hero,
						// just use the other party member for invitation.
						return partyMembers[1];
					}

                return null;
				}, false);

        if (partyMember == null)
            return;

        if (game.PartyMembers.Contains(partyMember))
        {
            Console.WriteLine($"{partyMember.Name} is already in the party.");
            Console.WriteLine();
            return;
        }

        int result = game.AddPartyMember(partyMember);

        if (result == -1)
        {
            Console.WriteLine($"Wrong window. Please invite party members on the map screen.");
            Console.WriteLine();
        }
        else if (result == -2)
        {
            Console.WriteLine($"There are no free party slots.");
            Console.WriteLine();
        }
        else
        {
            Console.WriteLine($"{partyMember.Name} joined the party.");
            Console.WriteLine();
        }
    }

    static PartyMember[] GetPartyMembers(Game game, string[] args, int argIndex)
    {
        int? partyMemberIndex = args.Length < argIndex + 1 ? null : int.TryParse(args[argIndex], out int i) ? i : -1;

        if (partyMemberIndex != null && partyMemberIndex > Game.MaxPartyMembers)
        {
            Console.WriteLine("Party member index was invalid or outside the range 0~6.");
            Console.WriteLine();
            return null;
        }

        if (partyMemberIndex == 0)
        {
            return game.PartyMembers.ToArray();
        }

        if (partyMemberIndex == -1)
        {
            string name = args[argIndex].ToLower();
            
            var partyMembers = game.PartyMembers.Where(p => p.Name.ToLower().StartsWith(name)).ToArray();

            if (partyMembers.Length == 0)
            {
                Console.WriteLine("No party member matches the given name.");
                Console.WriteLine();
                return null;
            }

            return partyMembers;
        }

        var partyMember = partyMemberIndex == null ? game.CurrentPartyMember : game.GetPartyMember(partyMemberIndex.Value - 1);

        if (partyMember == null)
        {
            Console.WriteLine($"Party member with index {partyMemberIndex} does not exist.");
            Console.WriteLine();
            return null;
        }

        return new PartyMember[1] { partyMember };
    }

    static IEnumerable<string> GetCurseConditionNames()
    {
        foreach (var condition in Enum.GetValues<Condition>())
        {
            if (condition == Condition.DeadCorpse)
                yield return "Dead";
            else if (condition == Condition.DeadAshes)
                yield return "Ashes";
            else if (condition == Condition.DeadDust)
                yield return "Dust";
            else if (condition.CanBeAppliedManually())
                yield return Enum.GetName(condition);
        }
    }

    static string GetConditionName(Condition? condition)
    {
        return condition switch
        {
            null or Condition.None => nameof(Condition.None),
            Condition.DeadCorpse => "Dead",
            Condition.DeadAshes => "Ashes",
            Condition.DeadDust => "Dust",
            _ => condition.ToString()
        };
    }

    static Condition? ParseCondition(string[] args, int argIndex, bool apply)
    {
        string name = args[argIndex].ToLower();

        if (name.StartsWith("de"))
            return Condition.DeadCorpse;
        else if (name.StartsWith("du"))
            return Condition.DeadDust;
        else if (name.StartsWith("as"))
            return Condition.DeadAshes;

        foreach (var condition in Enum.GetNames<Condition>())
        {
            if (condition.ToLower().StartsWith(name))
            {
                var result = Enum.Parse<Condition>(condition);

                if ((apply && result.CanBeAppliedManually()) ||
                    (!apply && result.CanBeCleansed()))
                    return result;
            }
        }

        return null;
    }

    static void Curse(Game game, string[] args)
    {
        Console.WriteLine();

        if (args.Length < 1)
        {
            Console.WriteLine("No condition given.");
            Console.WriteLine();
            return;
        }

        Condition? condition = ParseCondition(args, 0, true);

        if (condition is null)
        {
            Console.WriteLine("Invalid condition given.");
            Console.WriteLine("Possible values are:");
            GetCurseConditionNames().ToList().ForEach(c => Console.WriteLine($"  - {c}"));
            Console.WriteLine();
            return;
        }

        if (condition.Value.IsBattleOnly() && !game.BattleActive)
        {
            Console.WriteLine($"Condition {GetConditionName(condition)} can only be applied in battle.");
            Console.WriteLine();
            return;
        }

        var partyMembers = GetPartyMembers(game, args, 1);

        if (partyMembers.Length == 0)
            return;

        var queue = new Queue<PartyMember>(partyMembers);
        var partyMembersToKill = new Queue<KeyValuePair<PartyMember, Condition>>(6);

        void CurseNext()
        {
            if (queue.Count == 0)
                return;

            var partyMember = queue.Dequeue();

            if (partyMember.Conditions.HasFlag(condition.Value))
            {
                Console.WriteLine($"{partyMember.Name} is already cursed with {GetConditionName(condition)}.");
                CurseNext();
                return;
            }

            if (condition.Value >= Condition.DeadCorpse)
            {
                if (partyMember.Alive)
                {
                    partyMembersToKill.Enqueue(new KeyValuePair<PartyMember, Condition>(partyMember, condition.Value));
                    CurseNext();
                    return;
                }

                partyMember.Conditions = condition.Value;
            }
            else
            {
                if (!partyMember.Alive)
                {
                    Console.WriteLine($"{partyMember.Name} is dead and can't be cursed with {GetConditionName(condition)}.");
                    CurseNext();
                    return;
                }

                partyMember.Conditions |= condition.Value;
            }

            Console.WriteLine($"{partyMember.Name} was cursed with {GetConditionName(condition)}.");

            CurseNext();
        }

        CurseNext();

        var partyMembersToUpdate = new List<PartyMember>(partyMembersToKill.Select(p => p.Key));

        if (partyMembersToKill.Count != 0)
        {
            if ((!game.BattleActive && !game.CampActive && game.WindowActive) || game.PopupActive)
            {
                Console.WriteLine("Killing a party member is only possible on the map screen, in battle or in camp.");
                Console.WriteLine("Moreover all popups should be closed.");
                Console.WriteLine();
                return;
            }

            if (game.BattleRoundActive)
            {
                Console.WriteLine("Killing party members in battle is only possible between rounds.");
                Console.WriteLine();
                return;
            }

            bool activeDied = false;

            void KillNext()
            {
                if (partyMembersToKill.Count == 0)
                    return;

                var (partyMember, deathCondition) = partyMembersToKill.Dequeue();
                bool wasActive = game.CurrentPartyMember == partyMember;

                void Died(Character _)
                {
                    partyMember.Died -= Died;
                    Console.WriteLine($"{partyMember.Name} was killed!");
                    Console.WriteLine();

                    if (wasActive)
                        activeDied = true;

                    KillNext();
                }

                partyMember.Died = null; // Remove any previous event handler
                partyMember.Died += Died;

                game.KillPartyMember(partyMember, deathCondition);
            }

            game.StartSequence();
            game.Pause();

            KillNext();

            game.UpdateCharacters(() =>
            {
                if (activeDied)
                    game.RecheckActivePlayer();

                game.EndSequence();
                game.Resume();
            }, partyMembersToUpdate);
        }
    }

    static void Cleanse(Game game, string[] args)
    {
        Console.WriteLine();

        // We use mask 0xff7f for all. This excludes condition "Fleeing" which basically states if someone is about to flee.
        // We not really use it in the remake but still if we do at some point, this should not be cleansable.
        Condition allConditions = (Condition)0xff7f;
        Condition? condition = args.Length < 1 || args[0].ToLower() == "all" ? allConditions : ParseCondition(args, 0, false);

        if (condition is null)
        {
            Console.WriteLine("Invalid condition given.");
            Console.WriteLine("Possible values are:");
            GetCurseConditionNames().Prepend("All").ToList().ForEach(c => Console.WriteLine($"  - {c}"));
            Console.WriteLine();
            return;
        }

        var partyMembers = GetPartyMembers(game, args, 1);

        if (partyMembers.Length == 0)
            return;

        var queue = new Queue<PartyMember>(partyMembers);
        var partyMembersToRevive = new Queue<PartyMember>(6);
        bool canRevive = game.CanRevive();

        void CleanseNext()
        {
            if (queue.Count == 0)
                return;

            var partyMember = queue.Dequeue();

            if (condition.Value != allConditions && !partyMember.Conditions.HasFlag(condition.Value))
            {
                if (partyMember.Alive || condition.Value != Condition.DeadCorpse)
                {
                    Console.WriteLine($"{partyMember.Name} is not {GetConditionName(condition)}.");
                    CleanseNext();
                    return;
                }
            }

            if (condition.Value.HasFlag(Condition.DeadCorpse))
            {
                if (partyMember.Alive)
                {
                    if (condition.Value != allConditions)
                    {
                        Console.WriteLine($"{partyMember.Name} is not dead.");
                        CleanseNext();
                        return;
                    }
                }
                else
                {
                    int newCondition = (int)partyMember.Conditions & ~(int)condition.Value;

                    if (newCondition >= (int)Condition.DeadAshes) // still ashes or dust?
                    {
                        Console.WriteLine($"{partyMember.Name} is ashes or dust and can't be revived. First remove dust and/or ashes.");
                        CleanseNext();
                        return;
                    }

                    if (canRevive)
                        partyMember.Conditions = Condition.DeadCorpse; // prepare so we can just revive
                    partyMembersToRevive.Enqueue(partyMember);
                    CleanseNext();
                    return;
                }

                partyMember.Conditions &= ~condition.Value;
                game.UpdateCharacterStatus(partyMember);
            }
            else
            {
                if (!partyMember.Alive && condition.Value == Condition.DeadDust)
                    partyMember.Conditions = Condition.DeadAshes;
                else if (!partyMember.Alive && condition.Value == Condition.DeadAshes)
                    partyMember.Conditions = Condition.DeadCorpse;
                else
                    partyMember.Conditions &= ~condition.Value;

                game.UpdateCharacterStatus(partyMember);
            }

            if (condition.Value == allConditions)
                Console.WriteLine($"{partyMember.Name} was cleansed from all conditions.");
            else
                Console.WriteLine($"{partyMember.Name} was cleansed from {GetConditionName(condition)}.");

            CleanseNext();
        }

        CleanseNext();

        var partyMembersToUpdate = new List<PartyMember>(partyMembersToRevive);

        if (partyMembersToRevive.Count != 0)
        {
            if (!canRevive)
            {
                Console.WriteLine("Reviving a party member is only possible in camp.");
                Console.WriteLine("Moreover all popups should be closed.");
                Console.WriteLine();
                return;
            }

            game.StartSequence();
            game.Pause();
            game.Revive(partyMembersToRevive.Peek(), partyMembersToRevive.ToList(), () =>
            {
                game.EndSequence();
                game.Resume();
            });
        }
    }
}
