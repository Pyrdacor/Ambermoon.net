using Ambermoon.Data;
using System;
using System.Collections.Generic;

namespace Ambermoon.UI;

class CustomGlobalVariableEvent(QuestLog questLog, Game game, Func<IEventProvider, Event, bool> eventTriggerCheck, uint globalVariable)
{
    public bool Check(IEventProvider eventProvider, Event @event)
    {
        bool result = eventTriggerCheck(eventProvider, @event);

        if (result)
        {
            game.CurrentSavegame.SetGlobalVariable(globalVariable, true);
            questLog.CheckEvent(eventProvider, new ActionEvent { TypeOfAction = ActionEvent.ActionType.SetGlobalVariable, ObjectIndex = globalVariable });
        }

        return result;
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

    private readonly List<CustomGlobalVariableEvent> customGlobalVariableEvents = [];

    private void InitCustomGlobalVariableEvents()
    {
        // Tell Tolimar about tools
        customGlobalVariableEvents.Add(new CustomGlobalVariableNPCEvent(this, game,
            (npc, @event) => npc.Index == 10 && @event is ConversationEvent c && c.Interaction == ConversationEvent.InteractionType.Keyword && c.KeywordIndex == 3,
            GlobalVar_TolimarQuestStarted));
    }
}
