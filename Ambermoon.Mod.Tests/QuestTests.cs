using Ambermoon.Data;
using Ambermoon.UI;

namespace Ambermoon.Mod.Tests;

static partial class Tests
{
    static void QuestTests()
    {
        Console.WriteLine("Mod quests");

        static bool Fails(string json)
        {
            try
            {
                ModQuests.Parse(json);
                return false;
            }
            catch (Exception ex) when (ex is InvalidDataException or Newtonsoft.Json.JsonException)
            {
                return true;
            }
        }

        Check(!Fails("""{ "Quests": [] }"""), "Empty quest file is valid");
        Check(Fails("""{ "Quests": [ { "Id": "a", "Name": { "en": "A" }, "Steps": [] } ] }"""), "Quests need steps");
        Check(Fails("""{ "Quests": [ { "Id": "a", "Name": { "en": "A" }, "Steps": [ { "Id": "s", "Text": { "en": "S" }, "Requires": [ "x" ] } ] } ] }"""), "Unknown required steps are invalid");
        Check(Fails("""{ "Quests": [ { "Id": "a", "Name": { "en": "A" }, "Steps": [ { "Id": "s", "Text": { "en": "S" }, "CompleteOn": [ { "Type": "Map" } ] } ] } ] }"""), "Map triggers need a map");
        Check(Fails("""{ "Quests": [ { "Id": "a", "Name": { "en": "A" }, "Steps": [ { "Id": "s", "Text": { "en": "S" }, "CompleteOn": [ { "Type": "Fly" } ] } ] } ] }"""), "Unknown trigger types are invalid");

        var file = ModQuests.Parse("""
        {
          "Quests": [
            {
              "Id": "main",
              "Name": { "en": "Main quest", "de": "Hauptquest" },
              "Steps": [
                { "Id": "enter", "Text": { "en": "Enter the cave" }, "Active": true, "CompleteOn": [ { "Type": "Map", "Map": 5 } ] },
                { "Id": "lever", "Text": { "en": "Pull the lever" }, "Hint": { "en": "It is in the north" },
                  "ActivateOn": [ { "Type": "StepCompleted", "Step": "enter" } ],
                  "CompleteOn": [ { "Type": "Event", "Map": 5, "X": 3, "Y": 4, "EventType": "Action" } ] },
                { "Id": "orcs", "Text": { "en": "Defeat orcs" }, "Amount": 3, "Active": true, "Requires": [ "lever" ],
                  "CompleteOn": [ { "Type": "Battle", "MonsterGroup": 7 } ] },
                { "Id": "secret", "Text": { "en": "Find the secret" } }
              ]
            }
          ]
        }
        """);
        var quests = new ModQuests(null, file);
        var map5 = new Map { Index = 5 };
        var map6 = new Map { Index = 6 };

        var main = quests.GetEntries(GameLanguage.German)[0];
        Check(main.Text == "Hauptquest" && main.State == QuestState.Active, "Main quest is active with a German name");
        Check(main.SubEntries[2].State == QuestState.Blocked && main.SubEntries[2].Text == "Defeat orcs (0/3)", "Steps with open requirements are blocked and show the amount");
        Check(main.SubEntries[3].State == QuestState.Inactive, "Steps without activation stay inactive");

        quests.OnBattleEnded(7, true);
        Check(quests.GetAmount("orcs") == 0, "Blocked steps make no progress");

        quests.OnPlayerMoved(map6, 1, 1);
        Check(quests.GetState("enter") == QuestState.Active, "Other maps don't complete a map trigger");
        quests.OnPlayerMoved(map5, 1, 1);
        Check(quests.GetState("enter") == QuestState.Completed && quests.GetState("lever") == QuestState.Active, "Map trigger completes the step and activates the next one");

        var actionEvent = (ActionEvent)Assets.EventFactory.Create(EventType.Action);
        var textEvent = Assets.EventFactory.Create(EventType.MapText);
        quests.OnEvent(map5, textEvent, 3, 4);
        quests.OnEvent(map5, actionEvent, 3, 5);
        Check(quests.GetState("lever") == QuestState.Active, "Event triggers check the event type and position");
        quests.OnEvent(map5, actionEvent, 3, 4);
        Check(quests.GetState("lever") == QuestState.Completed, "Event trigger completes the step");

        quests.OnBattleEnded(7, false);
        quests.OnBattleEnded(8, true);
        quests.OnBattleEnded(7, true);
        quests.OnBattleEnded(7, true);
        Check(quests.GetAmount("orcs") == 2 && quests.GetState("orcs") == QuestState.Active, "Only won battles against the group count");
        quests.OnBattleEnded(7, true);
        Check(quests.GetState("orcs") == QuestState.Completed && quests.GetEntries(GameLanguage.English)[0].SubEntries[2].Text == "Defeat orcs (3/3)", "Amount completes the step");

        // Save and load
        var data = new ModSaveData();
        quests.Save(data);
        var loaded = new ModQuests(null, file);
        loaded.Load(ModSaveData.FromJson(data.ToJson()));
        Check(loaded.GetState("orcs") == QuestState.Completed && loaded.GetState("secret") == QuestState.Inactive, "Quest progress is restored");

        // Code API
        loaded.Complete("secret");
        Check(loaded.GetEntries(GameLanguage.English)[0].State == QuestState.Completed, "Main quest is completed when all steps are");
        loaded.Reset();
        Check(loaded.GetState("enter") == QuestState.Active && loaded.GetState("orcs") == QuestState.Active && loaded.GetAmount("orcs") == 0, "Reset restores the initial state");

        Check(!new ModQuests(null, new QuestFile()).HasQuests, "Mods without quest file have no quests (quest log disabled)");
    }
}
