using System;
using System.Collections.Generic;
using Ambermoon.Data;
using static Ambermoon.EventExtensions;

namespace Ambermoon.UI;

// TODO: Support for text popup NPCs which are not events at all!
// TODO: GlobalVar_SylphQuestStarted must be using this then!

class CustomGlobalVariableEvent
{
    private readonly QuestLog questLog;
    private readonly GameCore game;
    private readonly uint globalVariable;
    private readonly Func<IEventProvider, Event, bool>? eventTriggerCheck;
    private readonly Func<uint, uint, bool>? textPopupNPCCheck;

    private protected CustomGlobalVariableEvent(QuestLog questLog, GameCore game, Func<IEventProvider, Event, bool> eventTriggerCheck, uint globalVariable)
    {
        this.questLog = questLog;
        this.game = game;
        this.globalVariable = globalVariable;
        this.eventTriggerCheck = eventTriggerCheck;
    }

    private protected CustomGlobalVariableEvent(QuestLog questLog, GameCore game, Func<uint, uint, bool> textPopupNPCCheck, uint globalVariable)
    {
        this.questLog = questLog;
        this.game = game;
        this.globalVariable = globalVariable;
        this.textPopupNPCCheck = textPopupNPCCheck;
    }

    public bool Check(IEventProvider eventProvider, Event @event)
    {
        bool? result = eventTriggerCheck?.Invoke(eventProvider, @event);

        if (result == true)
        {
            game.CurrentSavegame!.SetGlobalVariable(globalVariable, true);
            questLog.CheckEvent(eventProvider, new ActionEvent { TypeOfAction = ActionEvent.ActionType.SetGlobalVariable, ObjectIndex = globalVariable });
        }

        return result ?? false;
    }

    public bool CheckTextPopupNPC(uint mapIndex, uint mapCharacterIndex)
    {
        bool? result = textPopupNPCCheck?.Invoke(mapIndex, mapCharacterIndex);

        if (result == true)
        {
            game.CurrentSavegame!.SetGlobalVariable(globalVariable, true);
            questLog.CheckEvent(null, new ActionEvent { TypeOfAction = ActionEvent.ActionType.SetGlobalVariable, ObjectIndex = globalVariable });
        }

        return result ?? false;
    }
}

file class CustomGlobalVariableMapEvent(QuestLog questLog, GameCore game, Func<Map, Event, bool> eventTriggerCheck, uint globalVariable)
    : CustomGlobalVariableEvent(questLog, game, (eventProvider, @event) => eventProvider is Map map && eventTriggerCheck(map, @event), globalVariable)
{

}

file class CustomGlobalVariableNPCEvent(QuestLog questLog, GameCore game, Func<NPC, Event, bool> eventTriggerCheck, uint globalVariable)
    : CustomGlobalVariableEvent(questLog, game, (eventProvider, @event) => eventProvider is NPC npc && eventTriggerCheck(npc, @event), globalVariable)
{

}

file class CustomGlobalVariableTextPopupNPCEvent(QuestLog questLog, GameCore game, Func<uint, uint, bool> textPopupNPCCheck, uint globalVariable)
    : CustomGlobalVariableEvent(questLog, game, textPopupNPCCheck, globalVariable)
{

}

partial class QuestLog
{
    // Some quests have no real trigger. Mostly because they are just started by some
    // NPC text without any change in the savegame. We have plenty of global variables.
    // To be precise there are 8192 of them (original only uses a bit more than 200 and
    // AA currently about 400). So we can use them to persist the state of some quests.
    // We should use this only when necessary. This won't work when loading old savegames
    // in any case but we can't do much about it. To leave as much global vars as possible
    // we will start from the end.
    const uint GlobalVar_TolimarQuestStarted = 8191u;
    const uint GlobalVar_SylphQuestStarted = 8190u;
    const uint GlobalVar_ShowedAmberToGrandfather = 8189u;
    const uint GlobalVar_GaveOrcLeaderHeadToBaronGeorge = 8188u;
    const uint GlobalVar_GaveThiefProofToBaronGeorge = 8187u;
    const uint GlobalVar_ReturnedHorseshoesToTolimar = 8186u;
    const uint GlobalVar_ChainOfOfficeQuestStarted = 8185u;
    const uint GlobalVar_TalkedToSandire = 8184u;
    const uint GlobalVar_AmanAskedForBrooch = 8183u;
    const uint GlobalVar_ClementineToldAboutMadman = 8182u;
    const uint GlobalVar_ToldSandraAboutRescue = 8181u;
    const uint GlobalVar_UsedYellowSphere = 8180u;
    const uint GlobalVar_HeidiToldAboutFairies = 8179u;
    const uint GlobalVar_BrewedDemonSleep = 8178u;
    const uint GlobalVar_LeftForestMoon = 8177u;

    private readonly List<CustomGlobalVariableEvent> customGlobalVariableEvents = [];

    private void InitCustomGlobalVariableEvents()
    {
        // Tell Tolimar about tools
        customGlobalVariableEvents.Add(new CustomGlobalVariableNPCEvent(this, game,
            (npc, @event) => npc.Index == 10 && @event is ConversationEvent c && c.Interaction == ConversationEvent.InteractionType.Keyword && c.KeywordIndex == 3,
            GlobalVar_TolimarQuestStarted));

        // Talk to the cook of baron george
        customGlobalVariableEvents.Add(new CustomGlobalVariableTextPopupNPCEvent(this, game,
            (mapIndex, mapCharIndex) => mapIndex == 269 && mapCharIndex == 0, // Cook NPC
            GlobalVar_SylphQuestStarted));

        // Show Shandra's amber to grandfather
        customGlobalVariableEvents.Add(new CustomGlobalVariableNPCEvent(this, game,
            (npc, @event) => npc.Index == 1 && @event is ConversationEvent c && (c.Interaction == ConversationEvent.InteractionType.ShowItem || c.Interaction == ConversationEvent.InteractionType.GiveItem) && c.ItemIndex == 209,
            GlobalVar_ShowedAmberToGrandfather));

        // Give the orc leader's head to Baron George (Freiherr Georg, NPC 8) -- completes the Orc Plague quest.
        customGlobalVariableEvents.Add(new CustomGlobalVariableNPCEvent(this, game,
            (npc, @event) => npc.Index == 8 && @event is ConversationEvent c && c.Interaction == ConversationEvent.InteractionType.GiveItem && c.ItemIndex == 268,
            GlobalVar_GaveOrcLeaderHeadToBaronGeorge));

        // Give all four golden horseshoes to Tolimar. He only creates the tools, so nothing is stored.
        // The event is checked before the item is consumed, so all four horseshoes are still owned here.
        customGlobalVariableEvents.Add(new CustomGlobalVariableNPCEvent(this, game,
            (npc, @event) => npc.Index == 10 && @event is ConversationEvent c && c.Interaction == ConversationEvent.InteractionType.GiveItem && c.ItemIndex == 270 &&
                game.GetTotalItemCount(270) >= 4,
            GlobalVar_ReturnedHorseshoesToTolimar));

        // Lady Heidi (9) or Baron George (8) tell about the fairies and the stolen chain of office (keywords "Fairies" and "Fairy")
        customGlobalVariableEvents.Add(new CustomGlobalVariableNPCEvent(this, game,
            (npc, @event) => (npc.Index == 8 || npc.Index == 9) && @event is ConversationEvent c && c.Interaction == ConversationEvent.InteractionType.Keyword &&
                (c.KeywordIndex == 24 || c.KeywordIndex == 25),
            GlobalVar_ChainOfOfficeQuestStarted));

        // Sandire (35) offers the monster eye when you talk to her
        customGlobalVariableEvents.Add(new CustomGlobalVariableNPCEvent(this, game,
            (npc, @event) => npc.Index == 35 && @event is ConversationEvent c && c.Interaction == ConversationEvent.InteractionType.Talk,
            GlobalVar_TalkedToSandire));

        // Lady Heidi (9) tells the fairy story (keywords "Fairies" and "Fairy")
        customGlobalVariableEvents.Add(new CustomGlobalVariableNPCEvent(this, game,
            (npc, @event) => npc.Index == 9 && @event is ConversationEvent c && c.Interaction == ConversationEvent.InteractionType.Keyword &&
                (c.KeywordIndex == 24 || c.KeywordIndex == 25),
            GlobalVar_HeidiToldAboutFairies));

        // The demon sleep was brewed in the witch's cellar (map 412): the treasure with the potion is shown.
        // The potion is used up at the temple, so this keeps the step completed.
        customGlobalVariableEvents.Add(new CustomGlobalVariableMapEvent(this, game,
            (map, @event) => map.Index == 412 && @event is ChestEvent c && c.RealChestIndex == 86,
            GlobalVar_BrewedDemonSleep));

        // The airship on the forest moon (map 436) flies to Lyramion (blue navstone, map 435) or Morag
        // (yellow navstone, map 437). The navstones are used up and the machine does not store anything.
        customGlobalVariableEvents.Add(new CustomGlobalVariableMapEvent(this, game,
            (map, @event) => map.Index == 436 && @event is TeleportEvent t && (t.MapIndex == 435 || t.MapIndex == 437),
            GlobalVar_LeftForestMoon));

        // Aman (3) asks for the gardener's brooch (keyword "Thing") as long as he didn't get it (12)
        customGlobalVariableEvents.Add(new CustomGlobalVariableNPCEvent(this, game,
            (npc, @event) => npc.Index == 3 && @event is ConversationEvent c && c.Interaction == ConversationEvent.InteractionType.Keyword && c.KeywordIndex == 6 &&
                !game.CurrentSavegame!.GetGlobalVariable(12),
            GlobalVar_AmanAskedForBrooch));

        // Clementine (6) tells about the madman (keyword "Madman") as long as he is not healed (29)
        customGlobalVariableEvents.Add(new CustomGlobalVariableNPCEvent(this, game,
            (npc, @event) => npc.Index == 6 && @event is ConversationEvent c && c.Interaction == ConversationEvent.InteractionType.Keyword && c.KeywordIndex == 21 &&
                !game.CurrentSavegame!.GetGlobalVariable(29),
            GlobalVar_ClementineToldAboutMadman));

        // Talk to Sandra (5) after Sabine was rescued (27). The event is checked before her conversation runs.
        customGlobalVariableEvents.Add(new CustomGlobalVariableNPCEvent(this, game,
            (npc, @event) => npc.Index == 5 && @event is ConversationEvent c && c.Interaction == ConversationEvent.InteractionType.Talk &&
                game.CurrentSavegame!.GetGlobalVariable(27),
            GlobalVar_ToldSandraAboutRescue));

        // Use the yellow sphere at the teleporter machine (map 369). Only text 16 is shown then.
        customGlobalVariableEvents.Add(new CustomGlobalVariableMapEvent(this, game,
            (map, @event) => map.Index == 369 && @event is PopupTextEvent p && p.TextIndex == 16,
            GlobalVar_UsedYellowSphere));

        // Give the thief-plague proof to Baron George (NPC 8): the silver hand (266, Nagier killed)
        // or Nagier's letter (267, peace made) -- completes the Thief Plague quest.
        customGlobalVariableEvents.Add(new CustomGlobalVariableNPCEvent(this, game,
            (npc, @event) => npc.Index == 8 && @event is ConversationEvent c && c.Interaction == ConversationEvent.InteractionType.GiveItem && (c.ItemIndex == 266 || c.ItemIndex == 267),
            GlobalVar_GaveThiefProofToBaronGeorge));
    }
}
