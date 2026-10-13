using System;
using System.Linq;
using System.Text;
using Ambermoon.Data;
using Ambermoon.UI;

namespace Ambermoon;

/// <summary>
/// Small API for automated test scenarios (see command line option --script).
/// </summary>
partial class GameCore
{
    sealed record ScriptConversationActions(Action<string> SayWord, Action ShowItem, Action GiveItem, Action Leave);

    ScriptConversationActions? scriptConversation;
    Action<int>? scriptConversationItemSelector;

    public bool ScriptConversationActive => scriptConversation != null && CurrentWindow.Window == UI.Window.Conversation;

    /// <summary>
    /// True if a conversation text waits for a click.
    /// </summary>
    public bool ScriptConversationTextActive => ConversationTextActive;

    /// <summary>
    /// Starts a conversation with the given NPC like talking to it on the map.
    /// Returns an error message or null on success.
    /// </summary>
    public string? ScriptTalk(uint npcIndex)
    {
        if (WindowOrPopupActive || !InputEnable)
            return "a window or popup is open";

        var npc = CharacterManager.GetNPC(npcIndex);

        if (npc == null)
            return $"there is no NPC {npcIndex}";

        // the map character index is used if the NPC is removed from the map afterwards
        int characterIndex = Array.FindIndex(Map!.CharacterReferences, c => c?.Type == CharacterType.NPC && c.Index == npcIndex);
        npc.ExecuteEvents(this, EventTrigger.Mouth, characterIndex == -1 ? 0u : (uint)characterIndex);

        return null;
    }

    /// <summary>
    /// Says a keyword (dictionary index or word) in the active conversation.
    /// </summary>
    public string? ScriptSay(string keyword)
    {
        if (scriptConversation == null)
            return "no conversation is active";

        if (uint.TryParse(keyword, out uint index))
            keyword = textDictionary.Entries[(int)index];

        scriptConversation.SayWord(keyword);

        return null;
    }

    /// <summary>
    /// Shows or gives an item of the active party member in the active conversation.
    /// </summary>
    public string? ScriptConversationItem(uint itemIndex, bool give)
    {
        if (scriptConversation == null)
            return "no conversation is active";

        int slot = Array.FindIndex(CurrentPartyMember!.Inventory.Slots, s => !s.Empty && s.ItemIndex == itemIndex);

        if (slot == -1)
            return $"the active party member has no item {itemIndex}";

        if (give)
            scriptConversation.GiveItem();
        else
            scriptConversation.ShowItem();

        if (scriptConversationItemSelector == null)
            return "the item selection could not be opened";

        scriptConversationItemSelector(slot);

        return null;
    }

    /// <summary>
    /// Leaves the active conversation like the exit button.
    /// </summary>
    public string? ScriptLeaveConversation()
    {
        if (scriptConversation == null)
            return "no conversation is active";

        scriptConversation.Leave();

        return null;
    }

    /// <summary>
    /// Triggers the map events at the given 1-based map position.
    /// </summary>
    public bool ScriptTriggerMapEvent(EventTrigger trigger, uint x, uint y)
    {
        if (WindowOrPopupActive || x == 0 || y == 0)
            return false;

        return TriggerMapEvents(trigger, x - 1, y - 1);
    }

    public void ScriptSetGlobalVariable(uint index, bool value) => CurrentSavegame!.SetGlobalVariable(index, value);

    /// <summary>
    /// Uses an item from the active party member's inventory exactly like a
    /// player would do it via the inventory (including item consumption and
    /// map event triggering). Returns false if the item is not in the inventory.
    /// </summary>
    public bool ScriptUseItem(uint itemIndex)
    {
        if (CurrentPartyMember == null || WindowOrPopupActive)
            return false;

        int? slot = SlotFromPartyMember(CurrentPartyMember);

        if (slot == null)
            return false;

        int inventorySlot = Array.FindIndex(CurrentPartyMember.Inventory.Slots, s => !s.Empty && s.ItemIndex == itemIndex);

        if (inventorySlot == -1)
            return false;

        return OpenPartyMember(slot.Value, true, () =>
        {
            var itemSlot = CurrentInventory!.Inventory.Slots[inventorySlot];
            layout.UseItem(layout.GetInventoryGrid(), inventorySlot, itemSlot);
        });
    }

    public bool ScriptGetGlobalVariable(uint index) => CurrentSavegame!.GetGlobalVariable(index);

    /// <summary>
    /// Closes the current window (like the exit button). Returns false if only the map is shown.
    /// </summary>
    public bool ScriptCloseWindow()
    {
        if (!WindowActive || PopupActive)
            return false;

        CloseWindow();
        return true;
    }

    /// <summary>
    /// Amount of the given item in the inventories and equipment of all party members.
    /// </summary>
    public int ScriptCountItem(uint itemIndex)
    {
        return PartyMembers.Sum(p =>
            p.Inventory.Slots.Where(s => !s.Empty && s.ItemIndex == itemIndex).Sum(s => s.Amount) +
            p.Equipment.Slots.Values.Where(s => !s.Empty && s.ItemIndex == itemIndex).Sum(s => s.Amount));
    }

    /// <summary>
    /// Combat background index of the active battle or null if no battle window is open.
    /// </summary>
    public uint? ScriptBattleBackground()
    {
        if (CurrentWindow.Window != UI.Window.Battle || CurrentWindow.WindowParameters.Length < 4)
            return null;

        return CurrentWindow.WindowParameters[3] as uint?;
    }

    /// <summary>
    /// Opens or closes the quest log. Returns true if it is open afterwards.
    /// </summary>
    public bool ScriptToggleQuestLog()
    {
        ToggleQuestLog();
        return QuestLog?.Open == true;
    }

    /// <summary>
    /// State of the given sub quest or null if there is no quest log or no such quest.
    /// </summary>
    public QuestState? ScriptQuestState(SubQuestType subQuestType)
    {
        return QuestLog?.Quests.SelectMany(q => q.SubQuests).FirstOrDefault(q => q.Type == subQuestType)?.State;
    }

    /// <summary>
    /// All sub quests which are not inactive, grouped by main quest.
    /// </summary>
    public string ScriptQuests()
    {
        if (QuestLog == null)
            return "no quest log";

        var builder = new StringBuilder();

        foreach (var quest in QuestLog.Quests)
        {
            var subQuests = quest.SubQuests.Where(q => q.State != QuestState.Inactive).ToArray();

            if (subQuests.Length == 0)
                continue;

            builder.AppendLine($"  {quest.Type}");

            foreach (var subQuest in subQuests)
            {
                string amount = subQuest.MinAmount == null ? "" : $" ({subQuest.CurrentAmount}/{subQuest.MinAmount})";
                builder.AppendLine($"    {subQuest.Type}: {subQuest.State}{amount}");
            }
        }

        return builder.Length == 0 ? "  no active quests" : builder.ToString().TrimEnd();
    }

    public string ScriptState()
    {
        var builder = new StringBuilder();
        builder.Append($"Map {Map?.Index}, position {player?.Position.X + 1},{player?.Position.Y + 1}, direction {player?.Direction}");
        builder.Append($", window {CurrentWindow.Window}, popup {PopupActive}, battle {BattleActive}");
        return builder.ToString();
    }
}
