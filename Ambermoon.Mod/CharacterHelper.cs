using Ambermoon.Data;
using Ambermoon.Data.Enumerations;

namespace Ambermoon.Mod;

/// <summary>
/// Helpers to set up characters (party members, monsters) outside of the game UI.
/// They apply the same stat changes as the game does when items are equipped.
/// </summary>
public static class CharacterHelper
{
    /// <summary>
    /// Checks if the character could equip the item (class, gender and slot).
    /// </summary>
    public static bool CanEquip(Character character, Item item)
    {
        if (item.EquipmentSlot == EquipmentSlot.None)
            return false;

        if (character is PartyMember partyMember)
        {
            var classFlag = (ClassFlag)(1 << (int)partyMember.Class);

            if (!item.Classes.HasFlag(classFlag))
                return false;
        }

        var genderFlag = character.Gender == Gender.Female ? GenderFlag.Female : GenderFlag.Male;

        return item.Genders.HasFlag(genderFlag);
    }

    /// <summary>
    /// Removes the item of the given equipment slot (if any) and applies the stat changes.
    /// The item is lost (not moved to the inventory).
    /// </summary>
    public static void Unequip(Character character, EquipmentSlot slot, IItemManager itemManager)
    {
        var itemSlot = character.Equipment.Slots[slot];

        if (itemSlot.Empty || itemSlot.ItemIndex == 0)
        {
            itemSlot.Clear();
            return;
        }

        var item = itemManager.GetItem(itemSlot.ItemIndex);
        ApplyEquipmentStats(character, item, itemSlot.Amount, itemSlot.Flags.HasFlag(ItemSlotFlags.Cursed), false);

        if (slot == EquipmentSlot.RightHand && item.NumberOfHands == 2)
            character.Equipment.Slots[EquipmentSlot.LeftHand].Clear();

        itemSlot.Clear();
    }

    /// <summary>
    /// Equips the item in its equipment slot and applies the stat changes.
    /// An already equipped item in that slot is removed (and lost).
    /// </summary>
    public static void Equip(Character character, uint itemIndex, IItemManager itemManager, int amount = 1)
    {
        var item = itemManager.GetItem(itemIndex);
        var slot = item.EquipmentSlot;

        if (slot == EquipmentSlot.None)
            throw new ArgumentException($"Item {itemIndex} can't be equipped.");

        // Rings can be put on both fingers.
        if (slot == EquipmentSlot.RightFinger && !character.Equipment.Slots[EquipmentSlot.RightFinger].Empty &&
            character.Equipment.Slots[EquipmentSlot.LeftFinger].Empty)
            slot = EquipmentSlot.LeftFinger;

        Unequip(character, slot, itemManager);

        if (item.NumberOfHands == 2 && slot == EquipmentSlot.RightHand)
        {
            Unequip(character, EquipmentSlot.LeftHand, itemManager);
            // The second hand is blocked by an empty slot with amount 1.
            character.Equipment.Slots[EquipmentSlot.LeftHand].Replace(new ItemSlot { ItemIndex = 0, Amount = 1 });
        }
        else if (slot == EquipmentSlot.LeftHand)
        {
            var rightHand = character.Equipment.Slots[EquipmentSlot.RightHand];

            if (!rightHand.Empty && itemManager.GetItem(rightHand.ItemIndex).NumberOfHands == 2)
                Unequip(character, EquipmentSlot.RightHand, itemManager);
        }

        var itemSlot = new ItemSlot
        {
            ItemIndex = itemIndex,
            Amount = amount,
            NumRemainingCharges = item.InitialCharges,
            RechargeTimes = item.InitialRecharges,
            Flags = item.DefaultSlotFlags | (item.Flags.HasFlag(ItemFlags.Accursed) ? ItemSlotFlags.Cursed : ItemSlotFlags.None)
        };

        character.Equipment.Slots[slot].Replace(itemSlot);
        ApplyEquipmentStats(character, item, amount, item.Flags.HasFlag(ItemFlags.Accursed), true);
    }

    static void ApplyEquipmentStats(Character character, Item item, int amount, bool cursed, bool add)
    {
        int sign = add ? 1 : -1;
        int curseSign = cursed ? -1 : 1;

        character.BonusAttackDamage = (short)(character.BonusAttackDamage + sign * curseSign * item.Damage);
        character.BonusDefense = (short)(character.BonusDefense + sign * curseSign * item.Defense);
        character.MagicAttack = (short)(character.MagicAttack + sign * item.MagicAttackLevel);
        character.MagicDefense = (short)(character.MagicDefense + sign * item.MagicArmorLevel);
        character.HitPoints.BonusValue += sign * curseSign * item.HitPoints;
        character.SpellPoints.BonusValue += sign * curseSign * item.SpellPoints;

        if (character.HitPoints.CurrentValue > character.HitPoints.TotalMaxValue)
            character.HitPoints.CurrentValue = character.HitPoints.TotalMaxValue;
        if (character.SpellPoints.CurrentValue > character.SpellPoints.TotalMaxValue)
            character.SpellPoints.CurrentValue = character.SpellPoints.TotalMaxValue;
        if (item.Attribute != null)
            character.Attributes[item.Attribute.Value].BonusValue += sign * curseSign * item.AttributeValue;
        if (item.Skill != null)
            character.Skills[item.Skill.Value].BonusValue += sign * curseSign * item.SkillValue;
        if (item.SkillPenalty1Value != 0)
            character.Skills[item.SkillPenalty1].BonusValue -= sign * (int)item.SkillPenalty1Value;
        if (item.SkillPenalty2Value != 0)
            character.Skills[item.SkillPenalty2].BonusValue -= sign * (int)item.SkillPenalty2Value;

        if (add)
            character.TotalWeight += (uint)amount * item.Weight;
        else
            character.TotalWeight -= Math.Min(character.TotalWeight, (uint)amount * item.Weight);
    }

    /// <summary>
    /// Adds items to the inventory. Returns the amount which did not fit.
    /// </summary>
    public static int AddToInventory(Character character, uint itemIndex, int amount, IItemManager itemManager)
    {
        var item = itemManager.GetItem(itemIndex);
        int remaining = amount;

        // First stack onto existing slots, then use empty slots.
        foreach (var slot in character.Inventory.Slots.Where(s => s.ItemIndex == itemIndex && !s.Empty)
            .Concat(character.Inventory.Slots.Where(s => s.Empty)))
        {
            if (remaining <= 0)
                break;

            if (slot.Empty)
            {
                slot.Clear();
                slot.FillWithNewItem(itemManager, itemIndex, ref remaining);
            }
            else if (slot.ItemIndex == itemIndex && item.Flags.HasFlag(ItemFlags.Stackable))
            {
                int add = Math.Min(remaining, 99 - slot.Amount);
                slot.Amount += add;
                remaining -= add;
            }
        }

        int added = amount - remaining;
        character.TotalWeight += (uint)added * item.Weight;

        return remaining;
    }

    /// <summary>
    /// Returns the experience points needed to reach the given level.
    /// </summary>
    public static uint GetExperienceForLevel(PartyMember partyMember, uint level, Features? features = null)
        => level <= 1 ? 0 : partyMember.Class.GetExpFactor(features) * (level * level + level) / 2;

    /// <summary>
    /// Levels up the party member to the given level (including all level up
    /// effects like hit points and spell points). Lower levels are ignored.
    /// The party member is fully healed afterwards.
    /// </summary>
    public static void RaiseLevel(PartyMember partyMember, uint level, Func<int, int, int> random, Features? features = null)
    {
        level = Math.Min(50u, level);

        if (partyMember.Level < level)
        {
            uint needed = GetExperienceForLevel(partyMember, level, features);

            if (partyMember.ExperiencePoints < needed)
                partyMember.AddExperiencePoints(needed - partyMember.ExperiencePoints, random, features);
        }

        partyMember.HitPoints.CurrentValue = partyMember.HitPoints.TotalMaxValue;
        partyMember.SpellPoints.CurrentValue = partyMember.SpellPoints.TotalMaxValue;
    }
}
