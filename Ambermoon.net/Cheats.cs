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
                    "Usage: maps",
                    ShowMaps
                )
            },
            { "teleport",
                Create
                (
                    "Makes the party invulnerable." + Environment.NewLine +
                    "Usage: teleport <map_id> [x] [y] [direction]",
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

                partyMember.SpokenLanguages = (Language)0xff;

                switch (partyMember.Class)
                {
                    case Class.Adventurer:
                    case Class.Alchemist:
                        partyMember.LearnedAlchemisticSpells = 0xffff;
                        break;
                    case Class.Healer:
                    case Class.Paladin:
                        partyMember.LearnedHealingSpells = 0xffff;
                        break;
                    case Class.Ranger:
                    case Class.Mystic:
                        partyMember.LearnedMysticSpells = 0xffff;
                        break;
                    case Class.Mage:
                        partyMember.LearnedDestructionSpells = 0xffff;
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

            var maps = new List<Map>(game.MapManager.Maps);
            maps.Sort((a, b) => a.Index.CompareTo(b.Index));
            int halfCount = maps.Count / 2;
            int secondRowOffset = halfCount;

            if (maps.Count % 2 == 1)
                ++secondRowOffset;

            for (int i = 0; i < halfCount; ++i)
            {
                Console.Write($"{maps[i].Index:000}: {maps[i].Name}".PadRight(24));
                Console.WriteLine($"{maps[secondRowOffset + i].Index:000}: {maps[secondRowOffset + i].Name}");
            }

            if (secondRowOffset > halfCount)
                Console.WriteLine($"{maps[secondRowOffset - 1].Index:000}: {maps[secondRowOffset - 1].Name}");
        }

        static void Teleport(Game game, string[] args)
        {
            Console.WriteLine();

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

            var map = game.MapManager.Maps.Single(m => m.Index == mapIndex);
            uint? x = args.Length > 1 && uint.TryParse(args[1], out uint ax) ? ax : (uint?)null;
            uint? y = args.Length > 2 && uint.TryParse(args[2], out uint ay) ? ay : (uint?)null;
            var direction = (args.Length > 3 ? ParseDirection(args[3]) : null) ?? (CharacterDirection)(random.Next() % 4);
            bool randomPosition = x == null || y == null;

            if (x == null)
                x = 1u + (uint)random.Next() % (uint)map.Width;
            if (y == null)
                y = 1u + (uint)random.Next() % (uint)map.Height;

            while (true)
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
                    break;

                x = 1u + (uint)random.Next() % (uint)map.Width;
                y = 1u + (uint)random.Next() % (uint)map.Height;
            }

            Console.WriteLine($"Teleported to map {mapIndex} ({x}, {y})");
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
    }
}
