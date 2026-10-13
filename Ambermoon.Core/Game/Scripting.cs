using System;
using System.Linq;
using System.Text;
using Ambermoon.UI;

namespace Ambermoon;

/// <summary>
/// Small API for automated test scenarios (see command line option --script).
/// </summary>
partial class GameCore
{
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
