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
///   waitbattle [timeout ms] [optional]
///                             wait until a battle starts, pressing Return on popups meanwhile
///                             (answers "Do you want to fight?" with yes, default timeout 20000).
///                             With "optional" it is no failure if no battle starts (random monsters).
///   yes                       answer a decision popup with yes (Return key)
///   closewindow               close the current window like the exit button (e.g. battle loot)
///   takeloot [optional]       take all items, gold and food of the open chest or battle loot (active
///                             party member), with "optional" it is no failure if nothing is open
///   usekey &lt;item index&gt;       use a key or lockpick (138) on the shown lock of a chest or door
///   goto &lt;map&gt; &lt;x&gt; &lt;y&gt;        teleport to a 1-based map position or the nearest free position around it
///                             (facing the target), e.g. to stand next to a chest or monster. Without a free
///                             position around it (e.g. on the sea) the party is placed onto the target.
///   answer &lt;word&gt;             answer the shown riddlemouth or say a word in the shown dictionary
///                             (dictionary index or word), e.g. after "trigger x y Mouth"
///   battlebg [index]          print the combat background of the active battle, optionally check it
///   talk &lt;npc index&gt;          start a conversation with an NPC (like talking to it on the map)
///   say &lt;keyword&gt;             say a keyword (dictionary index or word) in the conversation
///   showitem &lt;item index&gt;     show an item of the active party member in the conversation
///   giveitem &lt;item index&gt;     give an item of the active party member in the conversation
///   takeitems                 take the items created in the conversation (active party member)
///   givegold &lt;amount&gt;         give gold in the conversation (active party member)
///   leave                     leave the conversation (like the exit button)
///   talkwait [timeout ms] [quiet ms]
///                             click through conversation texts, window messages and popups (Return)
///                             until nothing happened for 'quiet ms' (default 2000, timeout default 15000)
///   trigger &lt;x&gt; &lt;y&gt; [trigger]  trigger the map events at a 1-based map position
///                             (Move, Eye, Hand, Mouth, ..., default Move, or item:&lt;index&gt; to use an item
///                             on that tile, e.g. if the tile can't be entered)
///   interact &lt;char&gt; [trigger]  interact with a map character (1-based index, default Mouth),
///                             e.g. for text popup characters
///   activate &lt;slot&gt;           make the party member in slot 1-6 the active one
///   setvar &lt;index&gt; &lt;0|1&gt;      set a global variable
///   setskill &lt;skill&gt; &lt;value&gt;  set a skill (Ambermoon.Data.Skill name) of the active party member
///   setattr &lt;attribute&gt; &lt;value&gt;
///                             set an attribute (Ambermoon.Data.Attribute name) of the active party member
///   hour &lt;0-23&gt;               set the time of day (e.g. 23 for events which only happen at night)
///   eventbit &lt;map&gt; &lt;event&gt; [0|1]
///                             print if a map event chain (1-based) is disabled (1), optionally check it
///   attr &lt;attribute&gt; [value]  print an attribute of the active party member, optionally check it
///                             (value, +n for "increased by n since the last print" or + for "increased")
///   slp [value]               print the spell learning points of the active party member (check like attr)
///   text &lt;property&gt; [text]    print a text of the data name provider (IDataNameProvider property name),
///                             optionally check that it contains the given text
///   spellname &lt;spell&gt; [name]  print the name of a spell (Ambermoon.Data.Spell name), optionally check it
///   monstername &lt;index&gt; [name]
///                             print the name of a monster, optionally check it
///   mapflags &lt;map&gt; [hex]      print the flags of a map, optionally check them (e.g. 085a)
///   gotopoint &lt;map&gt; &lt;index&gt; [bit]
///                             print the name and savegame bit of a goto point (1-based), optionally check the bit
///   walkable &lt;map&gt; &lt;x&gt; &lt;y&gt; [0|1]
///                             print if the party can walk onto a 1-based position of a 2D map, optionally check it
///   seteventbit &lt;map&gt; &lt;event&gt; &lt;0|1&gt;
///                             disable (1) or enable (0) a map event chain (1-based), e.g. for steps of other quests
///   reloadquests              recreate the quest log from the savegame (like loading the game)
///   questlog                  open or close the quest log
///   hover &lt;x&gt; &lt;y&gt;             move the mouse to a game position (320x200), e.g. to show a tooltip
///   stats &lt;slot&gt;              open the character stats of the party member in slot 1-6
///   questlogclick &lt;line&gt;      click a text line (1-based) of the open quest log, e.g. a main quest to collapse it
///   questloghover &lt;line&gt;      move the mouse onto a text line (1-based) of the open quest log (source tooltip)
///   questexpanded &lt;type&gt; [0|1]
///                             print if a main quest (UI.MainQuestType name) is expanded, optionally check it
///   quests                    print all sub quests which are not inactive
///   quest &lt;type&gt; [state] [amount]
///                             print the state of a sub quest (UI.SubQuestType name), optionally check it and
///                             the current amount of its counter
///   mainquest &lt;type&gt; [state]  print the state of a main quest (UI.MainQuestType name), optionally check it
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
    bool battleOptional = false;
    DateTime? talkTimeout = null;
    int talkQuietTime = 2000;
    int talkPopupTries = 0;
    (int Line, string Command)? retryCommand = null;
    DateTime retryTimeout = DateTime.MinValue;
    DateTime lastPopupTime = DateTime.MinValue;
    int dismissQuietTime = 1500;
    int failures = 0;
    int checks = 0;
    readonly Dictionary<string, uint> lastValues = [];

    public bool Finished => commands.Count == 0 && retryCommand == null && idleTimeout == null && dismissTimeout == null && battleTimeout == null && talkTimeout == null && DateTime.Now >= continueTime;

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
        while (commands.Count != 0 || retryCommand != null || dismissTimeout != null || idleTimeout != null || battleTimeout != null || talkTimeout != null)
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
                    // Return closes most text popups and answers decisions with "yes".
                    // Some popups (e.g. scrolling map texts) need a click, so both are tried.
                    if (talkPopupTries++ % 2 == 0)
                    {
                        game.OnKeyDown(Key.Return, KeyModifiers.None);
                        game.OnKeyUp(Key.Return, KeyModifiers.None);
                    }
                    else
                    {
                        host.Click(host.WindowCenter, false);
                    }
                    lastPopupTime = DateTime.Now;
                    continueTime = DateTime.Now.AddMilliseconds(300);
                    return;
                }
                else if (game.ScriptConversationTextActive || game.ScriptEventPictureActive)
                {
                    host.Click(host.WindowCenter, false);
                    lastPopupTime = DateTime.Now;
                    continueTime = DateTime.Now.AddMilliseconds(300);
                    return;
                }
                else if (!game.InputEnable && game.WindowActive)
                {
                    // e.g. messages in the chest or battle loot window ("everyone gets x experience")
                    host.Click(host.GameToWindow(new Position(208, 70)), false);
                    lastPopupTime = DateTime.Now;
                    continueTime = DateTime.Now.AddMilliseconds(500);
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
                else if (game.PopupActive || game.ScriptEventPictureActive)
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
                    if (battleOptional)
                        Console.WriteLine("  no battle started");
                    else
                        Report(false, "waitbattle timed out: " + game.ScriptState());
                    battleTimeout = null;
                }
                else
                {
                    if (game.PopupActive)
                    {
                        // Return closes most text popups and answers "yes" (e.g. "Do you want to fight?").
                        // Some popups (e.g. scrolling map texts) need a click, so both are tried.
                        if (talkPopupTries++ % 2 == 0)
                        {
                            game.OnKeyDown(Key.Return, KeyModifiers.None);
                            game.OnKeyUp(Key.Return, KeyModifiers.None);
                        }
                        else
                        {
                            host.Click(host.WindowCenter, false);
                        }
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

            if (commands.Count == 0 && retryCommand == null)
                return;

            var (lineNumber, command) = retryCommand ?? commands.Dequeue();
            bool retry = retryCommand != null;
            retryCommand = null;
            string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string name = parts[0].ToLowerInvariant();
            string argument = command.Length > parts[0].Length ? command[parts[0].Length..].Trim() : "";

            if (!retry)
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
                        {
                            // the window might be busy (animations or messages which wait for a click)
                            if (!retry)
                                retryTimeout = DateTime.Now.AddMilliseconds(8000);
                            if (game.WindowActive && DateTime.Now < retryTimeout)
                            {
                                if (!game.InputEnable)
                                    host.Click(host.GameToWindow(new Position(208, 70)), false);
                                retryCommand = (lineNumber, command);
                                continueTime = DateTime.Now.AddMilliseconds(300);
                                return;
                            }
                            if (game.WindowActive)
                                Report(false, $"the window could not be closed ({game.ScriptState()})");
                            else
                                Console.WriteLine("  no window is open (e.g. an emptied treasure closes itself)");
                        }
                        break;
                    case "goto":
                    {
                        var position = game.ScriptGoto(uint.Parse(parts[1]), uint.Parse(parts[2]), uint.Parse(parts[3]));
                        if (position == null)
                            Report(false, $"goto {parts[1]} {parts[2]} {parts[3]} is not possible");
                        else
                            continueTime = DateTime.Now.AddMilliseconds(1500); // map change fade
                        return;
                    }
                    case "answer":
                    {
                        string? error = game.ScriptAnswerRiddlemouth(argument);
                        if (error != null)
                        {
                            // the riddle might not be shown yet
                            if (!retry)
                                retryTimeout = DateTime.Now.AddMilliseconds(10000);
                            if (DateTime.Now < retryTimeout)
                            {
                                if (game.PopupActive)
                                    host.Click(host.WindowCenter, false);
                                retryCommand = (lineNumber, command);
                                continueTime = DateTime.Now.AddMilliseconds(300);
                                return;
                            }
                        }
                        ReportError(error);
                        break;
                    }
                    case "usekey":
                    {
                        string? error = game.ScriptUseKey(uint.Parse(parts[1]));
                        // the remake uses owned keys automatically, then no lock is shown at all
                        if (error != null && !game.WindowActive)
                        {
                            Console.WriteLine("  no lock is shown (opened directly with an owned key?)");
                            break;
                        }
                        ReportError(error);
                        if (error == null)
                        {
                            // the key animation takes a while before the result message is shown
                            continueTime = DateTime.Now.AddMilliseconds(2500);
                            return;
                        }
                        break;
                    }
                    case "takeloot":
                    {
                        int? remaining = game.ScriptTakeLoot();
                        if (remaining == null)
                        {
                            // the chest might still be opening (e.g. after unlocking it)
                            if (!retry)
                                retryTimeout = DateTime.Now.AddMilliseconds(8000);
                            if (DateTime.Now < retryTimeout)
                            {
                                // e.g. "x opened the chest" messages wait for a click
                                if (!game.InputEnable && game.WindowActive)
                                    host.Click(host.GameToWindow(new Position(208, 70)), false);
                                retryCommand = (lineNumber, command);
                                continueTime = DateTime.Now.AddMilliseconds(200);
                                return;
                            }
                            if (parts.Length > 1 && parts[1].Equals("optional", StringComparison.OrdinalIgnoreCase))
                                Console.WriteLine("  no chest or loot is open");
                            else
                                Report(false, $"no chest or loot is open ({game.ScriptState()})");
                        }
                        else if (remaining != 0)
                            Console.WriteLine($"  {remaining} item slots could not be taken");
                        break;
                    }
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
                        battleOptional = parts.Length > 2 && parts[2].Equals("optional", StringComparison.OrdinalIgnoreCase);
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
                    case "givegold":
                        ReportError(game.ScriptGiveGold(uint.Parse(parts[1])));
                        break;
                    case "takeitems":
                    {
                        int? taken = game.ScriptTakeConversationItems();
                        if (taken == null)
                        {
                            Report(false, "no conversation is active");
                        }
                        else if (taken == -1)
                        {
                            Console.WriteLine("  the created items could not be taken (too heavy or no free slot)");
                        }
                        else if (taken == 0)
                        {
                            // items might be created after some animation
                            if (!retry)
                                retryTimeout = DateTime.Now.AddMilliseconds(4000);
                            if (DateTime.Now < retryTimeout)
                            {
                                if (game.ScriptConversationTextActive)
                                    host.Click(host.WindowCenter, false);
                                retryCommand = (lineNumber, command);
                                continueTime = DateTime.Now.AddMilliseconds(300);
                                return;
                            }
                            Console.WriteLine("  no items were created");
                        }
                        break;
                    }
                    case "leave":
                        ReportError(game.ScriptLeaveConversation());
                        break;
                    case "talkwait":
                        talkTimeout = DateTime.Now.AddMilliseconds(parts.Length > 1 ? int.Parse(parts[1]) : 15000);
                        talkQuietTime = parts.Length > 2 ? int.Parse(parts[2]) : 2000;
                        lastPopupTime = DateTime.Now;
                        continueTime = DateTime.Now.AddMilliseconds(200);
                        return;
                    case "trigger":
                    {
                        var trigger = parts.Length < 4 ? EventTrigger.Move
                            : parts[3].StartsWith("item:", StringComparison.OrdinalIgnoreCase)
                                ? EventTrigger.Item0 + int.Parse(parts[3][5..]) // using an item on that tile
                                : Enum.Parse<EventTrigger>(parts[3], true);
                        if (!game.ScriptTriggerMapEvent(trigger, uint.Parse(parts[1]), uint.Parse(parts[2])))
                            Console.WriteLine($"  no event was triggered at {parts[1]},{parts[2]}");
                        break;
                    }
                    case "interact":
                    {
                        var trigger = parts.Length > 2 ? Enum.Parse<EventTrigger>(parts[2], true) : EventTrigger.Mouth;
                        if (!game.ScriptInteractWithCharacter(uint.Parse(parts[1]), trigger))
                            Report(false, $"no interaction with map character {parts[1]} ({game.ScriptState()})");
                        break;
                    }
                    case "eventbit":
                    {
                        bool bit = game.ScriptGetEventBit(uint.Parse(parts[1]), uint.Parse(parts[2]));
                        Console.WriteLine($"  event bit {parts[1]}:{parts[2]} = {(bit ? 1 : 0)}");
                        if (parts.Length > 3)
                            Report(bit == (parts[3] == "1"), $"event bit {parts[1]}:{parts[2]} is {(bit ? 1 : 0)}, expected {parts[3]}");
                        break;
                    }
                    case "seteventbit":
                        game.ScriptSetEventBit(uint.Parse(parts[1]), uint.Parse(parts[2]), parts[3] == "1");
                        break;
                    case "activate":
                        if (!game.ScriptSetActivePartyMember(int.Parse(parts[1])))
                            Report(false, $"there is no party member in slot {parts[1]}");
                        break;
                    case "setvar":
                        game.ScriptSetGlobalVariable(uint.Parse(parts[1]), parts[2] == "1");
                        break;
                    case "hour":
                        game.ScriptSetHour(uint.Parse(parts[1]));
                        break;
                    case "setskill":
                        game.ScriptSetSkill(Enum.Parse<Data.Skill>(parts[1], true), uint.Parse(parts[2]));
                        break;
                    case "setattr":
                        game.ScriptSetAttribute(Enum.Parse<Data.Attribute>(parts[1], true), uint.Parse(parts[2]));
                        break;
                    case "attr":
                    {
                        var attribute = Enum.Parse<Data.Attribute>(parts[1], true);
                        CheckValue(attribute.ToString(), game.ScriptGetAttribute(attribute), parts.Length > 2 ? parts[2] : null);
                        break;
                    }
                    case "slp":
                        CheckValue("SLP", game.ScriptGetSpellLearningPoints(), parts.Length > 1 ? parts[1] : null);
                        break;
                    case "text":
                    {
                        string? text = game.ScriptGetText(parts[1]);
                        Console.WriteLine($"  {parts[1]}: {text ?? "unknown text"}");
                        if (parts.Length > 2)
                        {
                            string expected = command[(command.IndexOf(parts[1]) + parts[1].Length)..].Trim();
                            Report(text?.Contains(expected) == true, $"{parts[1]} is '{text}', expected to contain '{expected}'");
                        }
                        break;
                    }
                    case "spellname":
                    {
                        var spell = Enum.Parse<Data.Spell>(parts[1], true);
                        string spellName = game.ScriptGetSpellName(spell);
                        Console.WriteLine($"  spell {spell}: {spellName}");
                        if (parts.Length > 2)
                        {
                            string expected = command[(command.IndexOf(parts[1]) + parts[1].Length)..].Trim();
                            Report(spellName == expected, $"spell name of {spell} is '{spellName}', expected '{expected}'");
                        }
                        break;
                    }
                    case "monstername":
                    {
                        string? monsterName = game.ScriptGetMonsterName(uint.Parse(parts[1]));
                        Console.WriteLine($"  monster {parts[1]}: {monsterName ?? "unknown"}");
                        if (parts.Length > 2)
                        {
                            string expected = command[(command.IndexOf(parts[1]) + parts[1].Length)..].Trim();
                            Report(monsterName == expected, $"monster {parts[1]} is '{monsterName}', expected '{expected}'");
                        }
                        break;
                    }
                    case "mapflags":
                    {
                        var flags = game.ScriptGetMapFlags(uint.Parse(parts[1]));
                        string flagsText = flags == null ? "unknown map" : $"0x{(uint)flags.Value:x4}";
                        Console.WriteLine($"  map {parts[1]} flags: {flagsText}");
                        if (parts.Length > 2)
                            Report(flags != null && (uint)flags.Value == Convert.ToUInt32(parts[2], 16), $"map {parts[1]} flags are {flagsText}, expected {parts[2]}");
                        break;
                    }
                    case "gotopoint":
                    {
                        var gotoPoint = game.ScriptGetGotoPoint(uint.Parse(parts[1]), int.Parse(parts[2]));
                        string gotoText = gotoPoint == null ? "unknown goto point" : $"{gotoPoint.Value.Name} (bit {gotoPoint.Value.Bit})";
                        Console.WriteLine($"  goto point {parts[1]}:{parts[2]}: {gotoText}");
                        if (parts.Length > 3)
                            Report(gotoPoint?.Bit == uint.Parse(parts[3]), $"goto point {parts[1]}:{parts[2]} is {gotoText}, expected bit {parts[3]}");
                        break;
                    }
                    case "walkable":
                    {
                        bool? walkable = game.ScriptIsWalkable(uint.Parse(parts[1]), uint.Parse(parts[2]), uint.Parse(parts[3]));
                        string walkableText = walkable == null ? "no 2D map position" : walkable.Value ? "1" : "0";
                        Console.WriteLine($"  walkable {parts[1]}:{parts[2]},{parts[3]} = {walkableText}");
                        if (parts.Length > 4)
                            Report(walkable == (parts[4] == "1"), $"walkable {parts[1]}:{parts[2]},{parts[3]} is {walkableText}, expected {parts[4]}");
                        break;
                    }
                    case "reloadquests":
                        game.ScriptReloadQuestLog();
                        break;
                    case "questlog":
                        Console.WriteLine($"  quest log open: {game.ScriptToggleQuestLog()}");
                        break;
                    case "hover":
                        // moves the mouse to a game position (320x200), e.g. to show a tooltip
                        host.Hover(host.GameToWindow(new Position(int.Parse(parts[1]), int.Parse(parts[2]))));
                        break;
                    case "stats":
                        if (!game.ScriptOpenStats(int.Parse(parts[1])))
                            Report(false, $"the stats of party member {parts[1]} could not be opened ({game.ScriptState()})");
                        else
                            continueTime = DateTime.Now.AddMilliseconds(1000); // window fade
                        return;
                    case "questlogclick":
                        // clicks a text line (1-based) of the open quest log like the mouse does
                        host.Click(host.GameToWindow(Game.Game.ScriptQuestLogLinePosition(int.Parse(parts[1]))), false);
                        break;
                    case "questloghover":
                        // moves the mouse onto a text line (1-based) of the open quest log, e.g. to show the source tooltip
                        host.Hover(host.GameToWindow(Game.Game.ScriptQuestLogLinePosition(int.Parse(parts[1])) + new Position(110, 0)));
                        break;
                    case "questexpanded":
                    {
                        var mainQuestType = Enum.Parse<UI.MainQuestType>(parts[1], true);
                        var expanded = game.ScriptQuestExpanded(mainQuestType);
                        string expandedText = expanded == null ? "no quest log" : expanded.Value ? "1" : "0";
                        Console.WriteLine($"  {mainQuestType} expanded: {expandedText}");
                        if (parts.Length > 2)
                            Report(expanded == (parts[2] == "1"), $"{mainQuestType} expanded is {expandedText}, expected {parts[2]}");
                        break;
                    }
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
                        if (parts.Length > 3)
                        {
                            var amount = game.ScriptQuestAmount(subQuestType);
                            Report(amount == uint.Parse(parts[3]), $"{subQuestType} amount is {amount?.ToString() ?? "none"}, expected {parts[3]}");
                        }
                        break;
                    }
                    case "mainquest":
                    {
                        var mainQuestType = Enum.Parse<UI.MainQuestType>(parts[1], true);
                        var state = game.ScriptMainQuestState(mainQuestType);
                        Console.WriteLine($"  {mainQuestType}: {state?.ToString() ?? "no quest log"}");
                        if (parts.Length > 2)
                            Report(state == Enum.Parse<UI.QuestState>(parts[2], true), $"{mainQuestType} is {state?.ToString() ?? "no quest log"}, expected {parts[2]}");
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

                // the location helps to find the cause
                foreach (var frame in (ex.StackTrace ?? "").Split('\n').Take(4))
                    Console.WriteLine("    " + frame.Trim());
            }
        }
    }

    /// <summary>
    /// Prints a value and optionally checks it. The expected value can be absolute, "+n" (increased by
    /// n since the last time this value was printed) or "+" (increased at all).
    /// </summary>
    void CheckValue(string name, uint value, string? expected)
    {
        Console.WriteLine($"  {name} = {value}");

        if (expected != null)
        {
            bool known = lastValues.TryGetValue(name, out uint lastValue);

            if (expected == "+")
                Report(known && value > lastValue, $"{name} is {value}, expected more than {(known ? lastValue.ToString() : "?")}");
            else if (expected.StartsWith('+'))
                Report(known && value == lastValue + uint.Parse(expected[1..]), $"{name} is {value}, expected {(known ? lastValue.ToString() : "?")}{expected}");
            else
                Report(value == uint.Parse(expected), $"{name} is {value}, expected {expected}");
        }

        lastValues[name] = value;
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
    Position GameToWindow(Position position);
    void Click(Position position, bool right);
    void Hover(Position position);
    void Screenshot(string name);
    void Exit();
}
