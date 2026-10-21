using System;
using System.Collections.Generic;

#nullable enable

namespace Ambermoon;

/// <summary>
/// Options given on the command line. They only affect the current run
/// and are never written to the configuration file.
/// </summary>
sealed class CommandLineOptions
{
    public const string ExternalVersion = "external";
    public const string ModVersion = "mod";

    /// <summary>
    /// Game data folder (like the DataPath option with UseDataPath enabled).
    /// </summary>
    public string? DataPath { get; private set; }
    /// <summary>
    /// Version index of the version selector or "external" for the data path version.
    /// If given, the version selector is skipped.
    /// </summary>
    public string? Version { get; private set; }
    /// <summary>
    /// Id of a mod to start (in the configured language if available).
    /// If given, the version selector is skipped.
    /// </summary>
    public string? Mod { get; private set; }
    public bool SkipIntro { get; private set; }
    /// <summary>
    /// Skips the main menu and continues the game.
    /// </summary>
    public bool Continue { get; private set; }
    /// <summary>
    /// Skips the main menu and loads the given savegame slot.
    /// </summary>
    public int? LoadSlot { get; private set; }
    /// <summary>
    /// Seconds after the game started when a screenshot is saved to the Screenshots folder.
    /// </summary>
    public List<int> Screenshots { get; } = [];
    /// <summary>
    /// Seconds after the game started when the application is closed.
    /// </summary>
    public int? ExitAfter { get; set; }
    /// <summary>
    /// Cheat console commands executed the given seconds after the game started.
    /// </summary>
    public List<(int Seconds, string Command)> Cheats { get; } = [];
    /// <summary>
    /// Scenario script which is executed once the game runs. See <see cref="ScenarioScript"/>.
    /// </summary>
    public string? ScriptPath { get; private set; }

    /// <summary>
    /// The patcher (and its question popup) is skipped when the game is started by command line options.
    /// </summary>
    public bool NoPatcher { get; private set; }
    public bool SkipsVersionSelector => Version != null;
    public bool SkipsMainMenu => Continue || LoadSlot != null;

    public static void PrintUsage()
    {
        Console.WriteLine("Usage: Ambermoon.net [options]");
        Console.WriteLine();
        Console.WriteLine(" --data <path>      Use game data from the given folder (e.g. an Amberfiles folder)");
        Console.WriteLine(" --version <n>      Start version n of the version selector (0-based) without showing it");
        Console.WriteLine(" --version external Start the version from the data folder (implied by --data)");
        Console.WriteLine(" --mod <id>         Start the mod with the given id without showing the version selector");
        Console.WriteLine(" --skip-intro       Skip logos and intros");
        Console.WriteLine(" --no-patcher       Don't check for patches (implied by any option)");
        Console.WriteLine(" --continue         Skip the main menu and continue the game");
        Console.WriteLine(" --load <slot>      Skip the main menu and load the given savegame slot (0 = initial savegame)");
        Console.WriteLine(" --cheat <s> <cmd>  Execute a cheat console command s seconds after the game started (can be repeated)");
        Console.WriteLine(" --screenshot <s>   Save a screenshot s seconds after the game started (can be repeated)");
        Console.WriteLine(" --script <file>    Run a scenario script once the game started (see ScenarioScript.cs)");
        Console.WriteLine(" --exit-after <s>   Close the application s seconds after the game started");
        Console.WriteLine();
        Console.WriteLine("Example: Ambermoon.net --data C:\\Ambermoon-Advanced\\german\\Amberfiles --load 0 --cheat 3 \"teleport 483 20 13\" --screenshot 6 --exit-after 8");
    }

    public static CommandLineOptions Parse(string[] args)
    {
        var options = new CommandLineOptions();
        var queue = new Queue<string>(args);

        string NextValue(string option)
        {
            if (queue.Count == 0)
                throw new ArgumentException($"Missing value for option {option}.");

            return queue.Dequeue();
        }

        static int ParseSeconds(string value)
        {
            if (!int.TryParse(value, out int seconds) || seconds < 0)
                throw new ArgumentException($"Invalid number of seconds '{value}'.");

            return seconds;
        }

        options.NoPatcher = args.Length != 0;

        while (queue.Count != 0)
        {
            string arg = queue.Dequeue();

            switch (arg.ToLowerInvariant())
            {
                case "--data":
                    options.DataPath = NextValue(arg);
                    options.Version ??= ExternalVersion;
                    break;
                case "--version":
                    options.Version = NextValue(arg).ToLowerInvariant();
                    if (options.Version != ExternalVersion && !int.TryParse(options.Version, out _))
                        throw new ArgumentException($"Invalid version '{options.Version}'.");
                    break;
                case "--mod":
                    options.Mod = NextValue(arg);
                    options.Version = ModVersion;
                    break;
                case "--no-patcher":
                    break; // implied by any option
                case "--skip-intro":
                    options.SkipIntro = true;
                    break;
                case "--continue":
                    options.Continue = true;
                    break;
                case "--load":
                    if (!int.TryParse(NextValue(arg), out int slot) || slot < 0)
                        throw new ArgumentException("The savegame slot must be a number (0 is the initial savegame).");
                    options.LoadSlot = slot;
                    break;
                case "--screenshot":
                    options.Screenshots.Add(ParseSeconds(NextValue(arg)));
                    break;
                case "--cheat":
                    options.Cheats.Add((ParseSeconds(NextValue(arg)), NextValue(arg)));
                    break;
                case "--script":
                    options.ScriptPath = NextValue(arg);
                    break;
                case "--exit-after":
                    options.ExitAfter = ParseSeconds(NextValue(arg));
                    break;
                default:
                    throw new ArgumentException($"Unknown option '{arg}'.");
            }
        }

        return options;
    }
}
