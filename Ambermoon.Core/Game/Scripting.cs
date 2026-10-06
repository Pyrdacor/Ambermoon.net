using System;
using System.Linq;
using System.Text;

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
    /// Amount of the given item in the inventories and equipment of all party members.
    /// </summary>
    public int ScriptCountItem(uint itemIndex)
    {
        return PartyMembers.Sum(p =>
            p.Inventory.Slots.Where(s => !s.Empty && s.ItemIndex == itemIndex).Sum(s => s.Amount) +
            p.Equipment.Slots.Values.Where(s => !s.Empty && s.ItemIndex == itemIndex).Sum(s => s.Amount));
    }

    public string ScriptState()
    {
        var builder = new StringBuilder();
        builder.Append($"Map {Map?.Index}, position {player?.Position.X + 1},{player?.Position.Y + 1}, direction {player?.Direction}");
        builder.Append($", window {CurrentWindow.Window}, popup {PopupActive}, battle {BattleActive}");
        return builder.ToString();
    }
}
