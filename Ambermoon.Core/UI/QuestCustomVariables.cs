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
    private readonly Game game;
    private readonly uint globalVariable;
    private readonly Func<IEventProvider, Event, bool> eventTriggerCheck;
    private readonly Func<uint, uint, bool> textPopupNPCCheck;

    private protected CustomGlobalVariableEvent(QuestLog questLog, Game game, Func<IEventProvider, Event, bool> eventTriggerCheck, uint globalVariable)
    {
        this.questLog = questLog;
        this.game = game;
        this.globalVariable = globalVariable;
        this.eventTriggerCheck = eventTriggerCheck;
    }

    private protected CustomGlobalVariableEvent(QuestLog questLog, Game game, Func<uint, uint, bool> textPopupNPCCheck, uint globalVariable)
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
            game.CurrentSavegame.SetGlobalVariable(globalVariable, true);
            questLog.CheckEvent(eventProvider, new ActionEvent { TypeOfAction = ActionEvent.ActionType.SetGlobalVariable, ObjectIndex = globalVariable });
        }

        return result ?? false;
    }

    public bool CheckTextPopupNPC(uint mapIndex, uint mapCharacterIndex)
    {
        bool? result = textPopupNPCCheck?.Invoke(mapIndex, mapCharacterIndex);

        if (result == true)
        {
            game.CurrentSavegame.SetGlobalVariable(globalVariable, true);
            questLog.CheckEvent(null, new ActionEvent { TypeOfAction = ActionEvent.ActionType.SetGlobalVariable, ObjectIndex = globalVariable });
        }

        return result ?? false;
    }
}

file class CustomGlobalVariableMapEvent(QuestLog questLog, Game game, Func<Map, Event, bool> eventTriggerCheck, uint globalVariable)
    : CustomGlobalVariableEvent(questLog, game, (eventProvider, @event) => eventProvider is Map map && eventTriggerCheck(map, @event), globalVariable)
{

}

file class CustomGlobalVariableNPCEvent(QuestLog questLog, Game game, Func<NPC, Event, bool> eventTriggerCheck, uint globalVariable)
    : CustomGlobalVariableEvent(questLog, game, (eventProvider, @event) => eventProvider is NPC npc && eventTriggerCheck(npc, @event), globalVariable)
{

}

file class CustomGlobalVariableTextPopupNPCEvent(QuestLog questLog, Game game, Func<uint, uint, bool> textPopupNPCCheck, uint globalVariable)
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
    }
}
