using System;
using System.Linq;
using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.Render;
using Ambermoon.UI;
using TextColor = Ambermoon.Data.Enumerations.Color;

namespace Ambermoon;

partial class GameCore
{
    const uint LockpickItemIndex = 138;

    Action? itemDragCancelledHandler = null;

    public IItemManager ItemManager { get; }

    internal void DropGold(uint amount)
    {
        layout.ClosePopup(false, true);
        CurrentInventory!.RemoveGold(amount);
        layout.UpdateLayoutButtons();
        UpdateCharacterInfo();
    }

    internal void DropFood(uint amount)
    {
        layout.ClosePopup(false, true);
        CurrentInventory!.RemoveFood(amount);
        layout.UpdateLayoutButtons();
        UpdateCharacterInfo();
    }

    internal void StoreGold(uint amount)
    {
        layout.ClosePopup(false, true);
        var chest = OpenStorage as Chest;
        const uint MaxGoldPerChest = 0xffff;
        amount = Math.Min(amount, MaxGoldPerChest - chest!.Gold);
        CurrentInventory!.RemoveGold(amount);
        chest.Gold += amount;
        layout.UpdateLayoutButtons();
        UpdateCharacterInfo();
    }

    internal void StoreFood(uint amount)
    {
        layout.ClosePopup(false, true);
        var chest = OpenStorage as Chest;
        const uint MaxFoodPerChest = 0xffff;
        amount = Math.Min(amount, MaxFoodPerChest - chest!.Food);
        CurrentInventory!.RemoveFood(amount);
        chest.Food += amount;
        layout.UpdateLayoutButtons();
        UpdateCharacterInfo();
    }

    /// <summary>
    /// Tries to store the item inside the opened storage.
    /// </summary>
    /// <param name="itemSlot">Item to store. Don't change the itemSlot itself!</param>
    /// <returns>Status of dropping</returns>
    internal bool StoreItem(ItemSlot itemSlot, uint maxAmount)
    {
        if (OpenStorage == null)
            return false; // should not happen

        var slots = OpenStorage.Slots.ToList();

        if (ItemManager.GetItem(itemSlot.ItemIndex).Flags.HasFlag(ItemFlags.Stackable))
        {
            foreach (var slot in slots)
            {
                if (!slot.Empty && slot.ItemIndex == itemSlot.ItemIndex)
                {
                    // This will update itemSlot
                    int oldAmount = itemSlot.Amount;
                    slot.Add(itemSlot, (int)maxAmount);
                    int dropped = oldAmount - itemSlot.Amount;
                    maxAmount -= (uint)dropped;
                    if (maxAmount == 0)
                        return true;
                }
            }
        }

        foreach (var slot in slots)
        {
            if (slot.Empty)
            {
                // This will update itemSlot
                slot.Add(itemSlot, (int)maxAmount);
                return true;
            }
        }

        return false;
    }

    internal int DropItem(PartyMember partyMember, uint itemIndex, int amount)
    {
        return DropItem(SlotFromPartyMember(partyMember)!.Value, null, ItemSlot.CreateFromItem(ItemManager, itemIndex, ref amount, true));
    }

    /// <summary>
    /// Drops the item in the inventory of the given player.
    /// Returns the remaining amount of items that could not
    /// be dropped or 0 if all items were dropped successfully.
    /// </summary>
    internal int DropItem(int partyMemberIndex, int? slotIndex, ItemSlot item)
    {
        var partyMember = GetPartyMember(partyMemberIndex);

        if (partyMember == null || !partyMember.CanTakeItems(ItemManager, item))
            return item.Amount;

        bool stackable = ItemManager.GetItem(item.ItemIndex).Flags.HasFlag(ItemFlags.Stackable);

        var slots = slotIndex == null
            ? stackable ? partyMember.Inventory.Slots.Where(s => s.ItemIndex == item.ItemIndex && s.Amount < 99).ToArray() : new ItemSlot[0]
            : new ItemSlot[1] { partyMember.Inventory.Slots[slotIndex.Value] };
        int amountToAdd = item.Amount;

        if (slots.Length == 0) // no slot found -> try any empty slot
        {
            var emptySlot = partyMember.Inventory.Slots.FirstOrDefault(s => s.Empty);

            if (emptySlot == null) // no free slot
                return item.Amount;

            // This reduces item.Amount internally.
            int remaining = emptySlot.Add(item);
            int added = amountToAdd - remaining;

            InventoryItemAdded(ItemManager.GetItem(emptySlot.ItemIndex), added, partyMember);

            return remaining;
        }

        var itemToAdd = ItemManager.GetItem(item.ItemIndex);

        foreach (var slot in slots)
        {
            // This reduces item.Amount internally.
            slot.Add(item);

            if (item.Empty)
                break;
        }

        int addedAmount = amountToAdd - item.Amount;
        InventoryItemAdded(itemToAdd, addedAmount, partyMember);

        return item.Amount;
    }

    void InventoryItemAdded(Item item, int amount, PartyMember? partyMember = null)
    {
        partyMember ??= CurrentInventory;

        if (partyMember != null)
            partyMember.TotalWeight += (uint)amount * item.Weight;

        if (CurrentWindow.Window == Window.Inventory)
            layout.UpdateLayoutButtons();
    }

    internal void InventoryItemAdded(uint itemIndex, int amount, PartyMember partyMember)
    {
        InventoryItemAdded(ItemManager.GetItem(itemIndex), amount, partyMember);
    }

    void InventoryItemRemoved(Item item, int amount, PartyMember? partyMember = null)
    {
        partyMember ??= CurrentInventory;

        partyMember!.TotalWeight -= (uint)amount * item.Weight;

        if (CurrentWindow.Window == Window.Inventory)
            layout.UpdateLayoutButtons();
    }

    internal void InventoryItemRemoved(uint itemIndex, int amount, PartyMember? partyMember = null)
    {
        InventoryItemRemoved(ItemManager.GetItem(itemIndex), amount, partyMember);
    }

    void EquipmentAdded(Item item, int amount, Character? character = null)
    {
        bool cursed = item.Flags.HasFlag(ItemFlags.Accursed);

        character ??= CurrentInventory;

        // Note: amount is only used for ammunition. The weight is
        // influenced by the amount but not the damage/defense etc.
        character!.BonusAttackDamage = (short)(character.BonusAttackDamage + (cursed ? -1 : 1) * item.Damage);
        character.BonusDefense = (short)(character.BonusDefense + (cursed ? -1 : 1) * item.Defense);
        character.MagicAttack = (short)(character.MagicAttack + item.MagicAttackLevel);
        character.MagicDefense = (short)(character.MagicDefense + item.MagicArmorLevel);
        character.HitPoints.BonusValue += (cursed ? -1 : 1) * item.HitPoints;
        character.SpellPoints.BonusValue += (cursed ? -1 : 1) * item.SpellPoints;
        if (character.HitPoints.CurrentValue > character.HitPoints.TotalMaxValue)
            character.HitPoints.CurrentValue = character.HitPoints.TotalMaxValue;
        if (character.SpellPoints.CurrentValue > character.SpellPoints.TotalMaxValue)
            character.SpellPoints.CurrentValue = character.SpellPoints.TotalMaxValue;
        if (item.Attribute != null)
            character.Attributes[item.Attribute.Value].BonusValue += (cursed ? -1 : 1) * item.AttributeValue;
        if (item.Skill != null)
            character.Skills[item.Skill.Value].BonusValue += (cursed ? -1 : 1) * item.SkillValue;
        if (item.SkillPenalty1Value != 0)
            character.Skills[item.SkillPenalty1].BonusValue -= (int)item.SkillPenalty1Value;
        if (item.SkillPenalty2Value != 0)
            character.Skills[item.SkillPenalty2].BonusValue -= (int)item.SkillPenalty2Value;
        character.TotalWeight += (uint)amount * item.Weight;

        if (CurrentWindow.Window == Window.Inventory)
            layout.UpdateLayoutButtons();
    }

    internal void EquipmentAdded(uint itemIndex, int amount, Character character)
    {
        EquipmentAdded(ItemManager.GetItem(itemIndex), amount, character);
    }

    void EquipmentRemoved(Character character, Item item, int amount, bool cursed)
    {
        // Note: amount is only used for ammunition. The weight is
        // influenced by the amount but not the damage/defense etc.
        character.BonusAttackDamage = (short)(character.BonusAttackDamage - (cursed ? -1 : 1) * item.Damage);
        character.BonusDefense = (short)(character.BonusDefense - (cursed ? -1 : 1) * item.Defense);
        character.MagicAttack = (short)(character.MagicAttack - item.MagicAttackLevel);
        character.MagicDefense = (short)(character.MagicDefense - item.MagicArmorLevel);
        character.HitPoints.BonusValue -= (cursed ? -1 : 1) * item.HitPoints;
        character.SpellPoints.BonusValue -= (cursed ? -1 : 1) * item.SpellPoints;
        if (character.HitPoints.CurrentValue > character.HitPoints.TotalMaxValue)
            character.HitPoints.CurrentValue = character.HitPoints.TotalMaxValue;
        if (character.SpellPoints.CurrentValue > character.SpellPoints.TotalMaxValue)
            character.SpellPoints.CurrentValue = character.SpellPoints.TotalMaxValue;
        if (item.Attribute != null)
            character.Attributes[item.Attribute.Value].BonusValue -= (cursed ? -1 : 1) * item.AttributeValue;
        if (item.Skill != null)
            character.Skills[item.Skill.Value].BonusValue -= (cursed ? -1 : 1) * item.SkillValue;
        if (item.SkillPenalty1Value != 0)
            character.Skills[item.SkillPenalty1].BonusValue += (int)item.SkillPenalty1Value;
        if (item.SkillPenalty2Value != 0)
            character.Skills[item.SkillPenalty2].BonusValue += (int)item.SkillPenalty2Value;
        character.TotalWeight -= (uint)amount * item.Weight;

        if (CurrentWindow.Window == Window.Inventory)
            layout.UpdateLayoutButtons();
    }

    void EquipmentRemoved(Item item, int amount, bool cursed)
    {
        EquipmentRemoved(CurrentInventory!, item, amount, cursed);
    }

    internal void EquipmentRemoved(uint itemIndex, int amount, bool cursed)
    {
        EquipmentRemoved(ItemManager.GetItem(itemIndex), amount, cursed);
    }

    internal void EquipmentRemoved(Character character, uint itemIndex, int amount, bool cursed)
    {
        EquipmentRemoved(character, ItemManager.GetItem(itemIndex), amount, cursed);
    }

    internal void ItemDraggingCancelled()
    {
        itemDragCancelledHandler?.Invoke();
    }

    internal void ShowItemPopup(ItemSlot itemSlot, Action closeAction)
    {
        var item = ItemManager.GetItem(itemSlot.ItemIndex);
        var popup = layout.OpenPopup(new Position(16, 84), 18, 6, true, false);
        var itemArea = new Rect(31, 99, 18, 18);
        popup.AddSunkenBox(itemArea);
        popup.AddItemImage(itemArea.CreateModified(1, 1, -2, -2), item.GraphicIndex);
        popup.AddText(new Position(51, 101), item.Name, TextColor.White);
        popup.AddText(new Position(51, 109), DataNameProvider.GetItemTypeName(item.Type), TextColor.White);
        popup.AddText(new Position(32, 120), string.Format(DataNameProvider.ItemWeightDisplay.Replace("{0:00000}", "{0,5}"), item.Weight), TextColor.White);
        popup.AddText(new Position(32, 130), string.Format(DataNameProvider.ItemHandsDisplay, item.NumberOfHands), TextColor.White);
        popup.AddText(new Position(32, 138), string.Format(DataNameProvider.ItemFingersDisplay, item.NumberOfFingers), TextColor.White);
        bool showCursed = item.Flags.HasFlag(ItemFlags.Accursed) && itemSlot.Flags.HasFlag(ItemSlotFlags.Identified);
        int damage = showCursed ? -item.Damage : item.Damage;
        int defense = showCursed ? -item.Defense : item.Defense;
        int valueOffset = DataNameProvider.ItemFingersDisplay.IndexOf("{") - 1;
        popup.AddText(new Position(32, 146), DataNameProvider.ItemDamageDisplay.Substring(0, valueOffset) + damage.ToString("+#;-#; 0"), TextColor.White);
        popup.AddText(new Position(32, 154), DataNameProvider.ItemDefenseDisplay.Substring(0, valueOffset) + defense.ToString("+#;-#; 0"), TextColor.White);

        popup.AddText(new Position(177, 99), DataNameProvider.ClassesHeaderString, TextColor.LightGray);
        int column = 0;
        int row = 0;
        foreach (var @class in EnumHelper.GetValues<Class>())
        {
            var classFlag = (ClassFlag)(1 << (int)@class);

            if (item.Classes.HasFlag(classFlag))
            {
                popup.AddText(new Position(177 + column * 54, 107 + row * Global.GlyphLineHeight), DataNameProvider.GetClassName(@class), TextColor.White);

                if (++row == 5)
                {
                    ++column;
                    row = 0;
                }
            }
        }
        popup.AddText(new Position(177, 146), DataNameProvider.GenderHeaderString, TextColor.LightGray);
        popup.AddText(new Position(177, 154), DataNameProvider.GetGenderName(item.Genders), TextColor.White);

        void Close()
        {
            ClosePopup();
            // Note: If we call closeAction directly any new nextClickAction
            // assignment will be lost when we return true below because the
            // nextClickHandler processing will set it to null then afterwards.
            ExecuteNextUpdateCycle(closeAction);
        }

        void HandleRightClick()
        {
            if (!popup.HasChildPopup)
            {
                Close();
            }
            else
            {
                ExecuteNextUpdateCycle(() =>
                {
                    popup.CloseChildPopup();
                    SetupRightClickHandler();
                });
            }
        }

        void SetupRightClickHandler()
        {
            nextClickHandler = button =>
            {
                if (button == MouseButtons.Right)
                {
                    HandleRightClick();
                    return true;
                }
                return false;
            };
        }

        // This can only be closed with right click
        SetupRightClickHandler();

        if (itemSlot.Flags.HasFlag(ItemSlotFlags.Identified))
        {
            var eyeButton = popup.AddButton(new Position(popup.ContentArea.Right - Button.Width + 1, popup.ContentArea.Bottom - Button.Height + 1));
            eyeButton.ButtonType = ButtonType.Eye;
            eyeButton.Disabled = false;
            eyeButton.LeftClickAction += () => ShowItemDetails(popup, itemSlot);
            eyeButton.RightClickAction += Close;
            eyeButton.Visible = true;
        }
    }

    void ShowItemDetails(Popup itemPopup, ItemSlot itemSlot)
    {
        layout.HideTooltip();
        var item = ItemManager.GetItem(itemSlot.ItemIndex);
        bool cursed = itemSlot.Flags.HasFlag(ItemSlotFlags.Cursed) || item.Flags.HasFlag(ItemFlags.Accursed);
        int factor = cursed ? -1 : 1;
        var detailsPopup = itemPopup.AddPopup(new Position(32, 52), 12, 6);

        void AddValueDisplay(Position position, string formatString, int value)
        {
            detailsPopup.AddText(position, formatString.Replace("000", "00")
                .Replace(" {0:00}", value.ToString("+#;-#; 0")), TextColor.White);
        }

        AddValueDisplay(new Position(48, 68), DataNameProvider.MaxLPDisplay, factor * item.HitPoints);
        AddValueDisplay(new Position(128, 68), DataNameProvider.MaxSPDisplay, factor * item.SpellPoints);
        AddValueDisplay(new Position(48, 75), DataNameProvider.MBWDisplay, item.MagicAttackLevel);
        AddValueDisplay(new Position(128, 75), DataNameProvider.MBRDisplay, item.MagicArmorLevel);
        detailsPopup.AddText(new Position(48, 82), DataNameProvider.AttributeHeader, TextColor.LightOrange);
        if (item.Attribute != null && item.AttributeValue != 0)
        {
            detailsPopup.AddText(new Position(48, 89), DataNameProvider.GetAttributeName(item.Attribute.Value), TextColor.White);
            detailsPopup.AddText(new Position(170, 89), (factor * item.AttributeValue).ToString("+#;-#; 0"), TextColor.White);
        }
        detailsPopup.AddText(new Position(48, 96), DataNameProvider.SkillsHeaderString, TextColor.LightOrange);
        if (item.Skill != null && item.SkillValue != 0)
        {
            detailsPopup.AddText(new Position(48, 103), DataNameProvider.GetSkillName(item.Skill.Value), TextColor.White);
            detailsPopup.AddText(new Position(170, 103), (factor * item.SkillValue).ToString("+#;-#; 0"), TextColor.White);
        }
        detailsPopup.AddText(new Position(48, 110), DataNameProvider.FunctionHeader, TextColor.LightOrange);
        if (item.Spell != Spell.None && (item.InitialCharges != 0 || item.MaxCharges != 0))
        {
            detailsPopup.AddText(new Position(48, 117),
                $"{DataNameProvider.GetSpellName(item.Spell)} ({(itemSlot.NumRemainingCharges > 99 ? "**" : itemSlot.NumRemainingCharges.ToString())})",
                TextColor.White);
        }
        if (cursed)
        {
            var contentArea = detailsPopup.ContentArea;
            AddAnimatedText((area, text, color, align) => detailsPopup.AddText(area, text, color, align),
                new Rect(contentArea.X, 127, contentArea.Width, Global.GlyphLineHeight), DataNameProvider.Cursed,
                TextAlign.Center, () => layout.PopupActive && itemPopup?.HasChildPopup == true, 50, false);
        }
    }
}
