using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ambermoon.Frontend;

#nullable enable

namespace Ambermoon;

/// <summary>
/// Executes a test scenario line by line once the game runs (command line option --script).
///
/// Commands (one per line, # starts a comment):
///   wait &lt;ms&gt;                 wait some milliseconds
///   waitidle [timeout ms]     wait until no window/popup is open (default timeout 10000)
///   cheat &lt;command&gt;           execute a cheat console command (e.g. teleport, give)
///   useitem &lt;item index&gt;      use an item of the active party member like via the inventory
///   key &lt;key&gt;                 press and release a key (Ambermoon.Key name, e.g. Return, Escape)
///   click &lt;x&gt; &lt;y&gt; [right]     click at window pixel coordinates
///   dismiss                   close an open popup (left click in the window center)
///   dismissall [timeout ms] [quiet ms]
///                             close popups until no popup, window or disabled input occurred
///                             for 'quiet ms' (default 1500). Fails if this is not reached within
///                             'timeout ms' (default 15000).
///   screenshot &lt;name&gt;         save a screenshot to Screenshots/&lt;name&gt;.png
///   state                     print map, position, window and popup state
///   var &lt;index&gt; [0|1]        print a global variable, optionally check it
///   count &lt;item&gt; [amount]     print the amount of an item in the party, optionally check it
///   waitbattle [timeout ms]   wait until a battle starts, pressing Return on popups meanwhile
///                             (answers "Do you want to fight?" with yes, default timeout 20000)
///   yes                       answer a decision popup with yes (Return key)
///   closewindow               close the current window like the exit button (e.g. battle loot)
///   battlebg [index]          print the combat background of the active battle, optionally check it
///   talk &lt;npc index&gt;          start a conversation with an NPC (like talking to it on the map)
///   say &lt;keyword&gt;             say a keyword (dictionary index or word) in the conversation
///   showitem &lt;item index&gt;     show an item of the active party member in the conversation
///   giveitem &lt;item index&gt;     give an item of the active party member in the conversation
///   leave                     leave the conversation (like the exit button)
///   talkwait [timeout ms] [quiet ms]
///                             click through conversation texts and popups (Return) until nothing
///                             happened for 'quiet ms' (default 1000, timeout default 15000)
///   trigger &lt;x&gt; &lt;y&gt; [trigger]  trigger the map events at a 1-based map position
///                             (Move, Eye, Hand, Mouth, ..., default Move)
///   setvar &lt;index&gt; &lt;0|1&gt;      set a global variable
///   questlog                  open or close the quest log
///   quests                    print all sub quests which are not inactive
///   quest &lt;type&gt; [state]      print the state of a sub quest (UI.SubQuestType name), optionally check it
///                             (Inactive, Active, Blocked or Completed)
///   log &lt;text&gt;                print text
///   exit                      close the application
/// </summary>
sealed class ScenarioScript
{
    readonly Queue<(int Line, string Command)> commands = new();
    DateTime continueTime = DateTime.MinValue;
    DateTime? idleTimeout = null;
    DateTime? dismissTimeout = null;
    DateTime? battleTimeout = null;
    DateTime? talkTimeout = null;
    int talkQuietTime = 1000;
    DateTime lastPopupTime = DateTime.MinValue;
    int dismissQuietTime = 1500;
    int failures = 0;
    int checks = 0;

    public bool Finished => commands.Count == 0 && idleTimeout == null && dismissTimeout == null && battleTimeout == null && talkTimeout == null && DateTime.Now >= continueTime;

    public ScenarioScript(string path)
    {
        int lineNumber = 0;

        foreach (var line in File.ReadAllLines(path))
        {
            ++lineNumber;
            string command = line.Trim();

            if (command.Length != 0 && !command.StartsWith('#'))
                commands.Enqueue((lineNumber, command));
        }
    }

    /// <summary>
    /// Executes commands until a waiting command is reached.
    /// </summary>
    public void Update(Game.Game game, IScenarioScriptHost host)
    {
        while (commands.Count != 0 || dismissTimeout != null || idleTimeout != null || battleTimeout != null || talkTimeout != null)
        {
            if (DateTime.Now < continueTime)
                return;

            if (talkTimeout != null)
            {
                if (DateTime.Now > talkTimeout)
                {
                    Report(false, "talkwait timed out: " + game.ScriptState());
                    talkTimeout = null;
                }
                else if (game.PopupActive)
                {
                    // Return closes text popups and answers decisions with "yes"
                    game.OnKeyDown(Key.Return, KeyModifiers.None);
                    game.OnKeyUp(Key.Return, KeyModifiers.None);
                    lastPopupTime = DateTime.Now;
                    continueTime = DateTime.Now.AddMilliseconds(300);
                    return;
                }
                else if (game.ScriptConversationTextActive)
                {
                    host.Click(host.WindowCenter, false);
                    lastPopupTime = DateTime.Now;
                    continueTime = DateTime.Now.AddMilliseconds(300);
                    return;
                }
                else if (!game.InputEnable || (DateTime.Now - lastPopupTime).TotalMilliseconds < talkQuietTime)
                {
                    if (!game.InputEnable)
                        lastPopupTime = DateTime.Now;
                    return;
                }
                else
                {
                    talkTimeout = null;
                }
            }

            if (dismissTimeout != null)
            {
                if (DateTime.Now > dismissTimeout)
                {
                    Report(false, "dismissall timed out: " + game.ScriptState());
                    dismissTimeout = null;
                }
                else if (game.PopupActive)
                {
                    host.Click(host.WindowCenter, false);
                    lastPopupTime = DateTime.Now;
                    continueTime = DateTime.Now.AddMilliseconds(300);
                    return;
                }
                else if (!game.InputEnable || game.WindowActive)
                {
                    // windows (like the inventory while using an item) or disabled input count as activity
                    lastPopupTime = DateTime.Now;
                    return;
                }
                else if ((DateTime.Now - lastPopupTime).TotalMilliseconds < dismissQuietTime)
                {
                    return;
                }
                else
                {
                    dismissTimeout = null;
                }
            }

            if (battleTimeout != null)
            {
                if (game.BattleActive)
                {
                    battleTimeout = null;
                }
                else if (DateTime.Now > battleTimeout)
                {
                    Report(false, "waitbattle timed out: " + game.ScriptState());
                    battleTimeout = null;
                }
                else
                {
                    if (game.PopupActive)
                    {
                        // Return closes text popups and answers "yes" (e.g. "Do you want to fight?")
                        game.OnKeyDown(Key.Return, KeyModifiers.None);
                        game.OnKeyUp(Key.Return, KeyModifiers.None);
                    }
                    continueTime = DateTime.Now.AddMilliseconds(300);
                    return;
                }
            }

            if (idleTimeout != null)
            {
                if (game.WindowOrPopupActive || !game.InputEnable)
                {
                    if (DateTime.Now < idleTimeout)
                        return;

                    Report(false, "waitidle timed out: " + game.ScriptState());
                }

                idleTimeout = null;
            }

            if (commands.Count == 0)
                return;

            var (lineNumber, command) = commands.Dequeue();
            string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string name = parts[0].ToLowerInvariant();
            string argument = command.Length > parts[0].Length ? command[parts[0].Length..].Trim() : "";

            Console.WriteLine($"[script {lineNumber}] {command}");

            try
            {
                switch (name)
                {
                    case "wait":
                        continueTime = DateTime.Now.AddMilliseconds(int.Parse(parts[1]));
                        return;
                    case "waitidle":
                        idleTimeout = DateTime.Now.AddMilliseconds(parts.Length > 1 ? int.Parse(parts[1]) : 10000);
                        continueTime = DateTime.Now.AddMilliseconds(100);
                        return;
                    case "cheat":
                        Cheats.ProcessInput(argument, game);
                        break;
                    case "useitem":
                        if (!game.ScriptUseItem(uint.Parse(parts[1])))
                            Report(false, $"item {parts[1]} could not be used ({game.ScriptState()})");
                        break;
                    case "key":
                        var key = Enum.Parse<Key>(parts[1], true);
                        game.OnKeyDown(key, KeyModifiers.None);
                        game.OnKeyUp(key, KeyModifiers.None);
                        break;
                    case "click":
                        host.Click(new Position(int.Parse(parts[1]), int.Parse(parts[2])),
                            parts.Length > 3 && parts[3].Equals("right", StringComparison.OrdinalIgnoreCase));
                        break;
                    case "closewindow":
                        if (!game.ScriptCloseWindow())
                            Report(false, $"no window to close ({game.ScriptState()})");
                        break;
                    case "yes":
                        // Return presses the "yes" button of decision popups
                        game.OnKeyDown(Key.Return, KeyModifiers.None);
                        game.OnKeyUp(Key.Return, KeyModifiers.None);
                        break;
                    case "dismiss":
                        if (game.PopupActive)
                            host.Click(host.WindowCenter, false);
                        break;
                    case "dismissall":
                        dismissTimeout = DateTime.Now.AddMilliseconds(parts.Length > 1 ? int.Parse(parts[1]) : 15000);
                        dismissQuietTime = parts.Length > 2 ? int.Parse(parts[2]) : 1500;
                        lastPopupTime = DateTime.Now;
                        continueTime = DateTime.Now.AddMilliseconds(200);
                        return;
                    case "screenshot":
                        host.Screenshot(parts[1]);
                        break;
                    case "state":
                        Console.WriteLine("  " + game.ScriptState());
                        break;
                    case "var":
                    {
                        uint index = uint.Parse(parts[1]);
                        bool value = game.ScriptGetGlobalVariable(index);
                        Console.WriteLine($"  global variable {index} = {(value ? 1 : 0)}");
                        if (parts.Length > 2)
                            Report(value == (parts[2] == "1"), $"global variable {index} is {(value ? 1 : 0)}, expected {parts[2]}");
                        break;
                    }
                    case "count":
                    {
                        uint item = uint.Parse(parts[1]);
                        int amount = game.ScriptCountItem(item);
                        Console.WriteLine($"  item {item}: {amount}");
                        if (parts.Length > 2)
                            Report(amount == int.Parse(parts[2]), $"item {item} amount is {amount}, expected {parts[2]}");
                        break;
                    }
                    case "waitbattle":
                        battleTimeout = DateTime.Now.AddMilliseconds(parts.Length > 1 ? int.Parse(parts[1]) : 20000);
                        continueTime = DateTime.Now.AddMilliseconds(100);
                        return;
                    case "battlebg":
                    {
                        var background = game.ScriptBattleBackground();
                        Console.WriteLine($"  battle background: {(background?.ToString() ?? "no battle")}");
                        if (parts.Length > 1)
                            Report(background == uint.Parse(parts[1]), $"battle background is {(background?.ToString() ?? "no battle")}, expected {parts[1]}");
                        break;
                    }
                    case "talk":
                        ReportError(game.ScriptTalk(uint.Parse(parts[1])));
                        break;
                    case "say":
                        ReportError(game.ScriptSay(argument));
                        break;
                    case "showitem":
                    case "giveitem":
                        ReportError(game.ScriptConversationItem(uint.Parse(parts[1]), name == "giveitem"));
                        break;
                    case "leave":
                        ReportError(game.ScriptLeaveConversation());
                        break;
                    case "talkwait":
                        talkTimeout = DateTime.Now.AddMilliseconds(parts.Length > 1 ? int.Parse(parts[1]) : 15000);
                        talkQuietTime = parts.Length > 2 ? int.Parse(parts[2]) : 1000;
                        lastPopupTime = DateTime.Now;
                        continueTime = DateTime.Now.AddMilliseconds(200);
                        return;
                    case "trigger":
                    {
                        var trigger = parts.Length > 3 ? Enum.Parse<EventTrigger>(parts[3], true) : EventTrigger.Move;
                        if (!game.ScriptTriggerMapEvent(trigger, uint.Parse(parts[1]), uint.Parse(parts[2])))
                            Console.WriteLine($"  no event was triggered at {parts[1]},{parts[2]}");
                        break;
                    }
                    case "setvar":
                        game.ScriptSetGlobalVariable(uint.Parse(parts[1]), parts[2] == "1");
                        break;
                    case "questlog":
                        Console.WriteLine($"  quest log open: {game.ScriptToggleQuestLog()}");
                        break;
                    case "quests":
                        Console.WriteLine(game.ScriptQuests());
                        break;
                    case "quest":
                    {
                        var subQuestType = Enum.Parse<UI.SubQuestType>(parts[1], true);
                        var state = game.ScriptQuestState(subQuestType);
                        Console.WriteLine($"  {subQuestType}: {state?.ToString() ?? "no quest log"}");
                        if (parts.Length > 2)
                            Report(state == Enum.Parse<UI.QuestState>(parts[2], true), $"{subQuestType} is {state?.ToString() ?? "no quest log"}, expected {parts[2]}");
                        break;
                    }
                    case "log":
                        Console.WriteLine("  " + argument);
                        break;
                    case "exit":
                        commands.Clear();
                        host.Exit();
                        return;
                    default:
                        Report(false, $"unknown command '{parts[0]}'");
                        break;
                }
            }
            catch (Exception ex)
            {
                Report(false, $"line {lineNumber} failed: {ex.Message}");
            }
        }
    }

    void ReportError(string? error)
    {
        if (error != null)
            Report(false, error);
    }

    void Report(bool ok, string message)
    {
        ++checks;

        if (!ok)
            ++failures;

        Console.WriteLine(ok ? "  PASS " + message : "  FAIL " + message);
    }

    public string Summary => $"Script finished: {checks - failures} of {checks} checks passed, {failures} failed.";
}

/// <summary>
/// Window functionality needed by scenario scripts.
/// </summary>
interface IScenarioScriptHost
{
    Position WindowCenter { get; }
    void Click(Position position, bool right);
    void Screenshot(string name);
    void Exit();
}
