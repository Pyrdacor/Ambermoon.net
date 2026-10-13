using Ambermoon.Data;
using Ambermoon.Data.Enumerations;

namespace Ambermoon.Mod.AmbermoonDC;

/// <summary>
/// Creates chest contents and equipment fitting the current depth.
/// </summary>
internal class LootGenerator(ModApi api, System.Random random, int floor)
{
    readonly ModApi api = api;
    readonly System.Random random = random;
    readonly int floor = floor;
    List<Item>? equipment;

    IItemManager ItemManager => api.GameData.ItemManager;

    /// <summary>
    /// All items which can be equipped and are useful as loot.
    /// Ranged weapons are excluded as they need ammunition.
    /// </summary>
    List<Item> Equipment => equipment ??= [.. ItemManager.Items.Where(item =>
        item.EquipmentSlot != EquipmentSlot.None && item.Price > 0 && item.Classes != ClassFlag.None &&
        !item.Flags.HasFlag(ItemFlags.Accursed) && item.UsedAmmunitionType == AmmunitionType.None &&
        item.Type != ItemType.Ammunition)];

    static int Value(Item item) => item.Damage * 2 + item.Defense * 2 + item.MagicAttackLevel + item.MagicArmorLevel +
        Math.Abs(item.AttributeValue) / 2 + Math.Abs(item.SkillValue) / 2 + item.HitPoints / 4 + item.SpellPoints / 4;

    uint Pick(uint[] items) => items[random.Next(items.Length)];

    uint HealPotion => DCContent.HealPotions[Math.Min(DCContent.HealPotions.Length - 1, (floor - 1) / 8 + random.Next(2))];

    uint ManaPotion => DCContent.ManaPotions[Math.Min(DCContent.ManaPotions.Length - 1, (floor - 1) / 8 + random.Next(2))];

    uint Scroll => floor < 10 ? Pick(DCContent.EarlyScrolls) : floor < 25 ? Pick(random.Next(3) == 0 ? DCContent.EarlyScrolls : DCContent.MidScrolls)
        : Pick(random.Next(3) == 0 ? DCContent.MidScrolls : DCContent.LateScrolls);

    uint? RandomEquipment(uint budget)
    {
        var candidates = Equipment.Where(item => item.Price <= budget && item.Price >= budget / 5).ToList();

        if (candidates.Count == 0)
            candidates = Equipment.Where(item => item.Price <= budget).ToList();

        return candidates.Count == 0 ? null : candidates[random.Next(candidates.Count)].Index;
    }

    public Chest CreateChest(ChestKind kind)
    {
        var chest = new Chest { Type = ChestType.Chest, AllowsItemDrop = true };

        for (int y = 0; y < Chest.SlotRows; y++)
            for (int x = 0; x < Chest.SlotsPerRow; x++)
                chest.Slots[x, y] = new ItemSlot();

        var items = new List<(uint Item, int Amount)>();
        uint budget = DCContent.EquipmentBudget(floor);

        switch (kind)
        {
            case ChestKind.Normal:
                chest.Gold = (uint)(random.Next(3, 11) * floor);
                chest.Food = (uint)random.Next(0, 5);

                for (int i = random.Next(1, 4); i > 0; i--)
                {
                    int roll = random.Next(100);

                    if (roll < 35)
                        items.Add((HealPotion, 1));
                    else if (roll < 45)
                        items.Add((ManaPotion, 1));
                    else if (roll < 55)
                        items.Add((floor < 12 ? DCContent.Torch : DCContent.Lantern, floor < 12 ? random.Next(1, 4) : 1));
                    else if (roll < 70)
                        items.Add((Scroll, 1));
                    else if (roll < 75)
                        items.Add((DCContent.Antidote, 1));
                    else if (RandomEquipment(budget) is uint item)
                        items.Add((item, 1));
                }
                break;
            case ChestKind.Secret:
                chest.Gold = (uint)(random.Next(10, 21) * floor);
                chest.Food = (uint)random.Next(2, 7);
                items.Add((HealPotion, random.Next(1, 3)));

                if (RandomEquipment(budget * 2) is uint secretItem)
                    items.Add((secretItem, 1));

                items.Add(random.Next(2) == 0 ? (Scroll, 1) : (ManaPotion, 1));

                if (floor >= 15 && random.Next(100) < 20)
                    items.Add((Pick(DCContent.Rings), 1));
                break;
            case ChestKind.Treasure:
                chest.Gold = (uint)(random.Next(5, 16) * floor);
                items.Add((HealPotion, random.Next(2, 4)));
                items.Add((ManaPotion, random.Next(1, 3)));
                items.Add((Scroll, 1));

                if (random.Next(2) == 0)
                    items.Add((Scroll, 1));

                if (floor >= 20 && random.Next(100) < 25)
                    items.Add((DCContent.WaterOfLife, 1));

                if (random.Next(100) < 35 && RandomEquipment(budget * 3) is uint strongItem)
                    items.Add((strongItem, 1));
                break;
        }

        chest.Gold = Math.Min(chest.Gold, 9999);

        int slotIndex = 0;

        foreach (var (item, amount) in items)
        {
            int remaining = amount;

            while (remaining > 0 && slotIndex < Chest.SlotsPerRow * Chest.SlotRows)
            {
                var slot = ItemSlot.CreateFromItem(ItemManager, item, ref remaining);
                chest.GetSlot(slotIndex++).Replace(slot);
            }
        }

        return chest;
    }

    /// <summary>
    /// Selects a strong item for the boss which is an upgrade for one of the party members.
    /// </summary>
    public uint SelectBossItem(List<PartyMember> party)
    {
        uint budget = DCContent.EquipmentBudget(floor) * 4;
        Item? best = null;
        int bestImprovement = 0;

        foreach (var partyMember in party)
        {
            foreach (var item in Equipment.Where(item => item.Price <= budget && CharacterHelper.CanEquip(partyMember, item)))
            {
                var equipped = partyMember.Equipment.Slots[item.EquipmentSlot];
                int currentValue = equipped.Empty || equipped.ItemIndex == 0 ? 0 : Value(ItemManager.GetItem(equipped.ItemIndex));
                int improvement = Value(item) - currentValue;

                // Slight randomness so it is not always the same item.
                improvement = improvement * 10 + random.Next(10);

                if (improvement > bestImprovement)
                {
                    best = item;
                    bestImprovement = improvement;
                }
            }
        }

        return best?.Index ?? (floor >= 15 ? Pick(DCContent.Rings) : DCContent.HealPotions[Math.Min(4, floor / 8)]);
    }

    /// <summary>
    /// Gives a recruit the best equipment the current depth allows.
    /// </summary>
    public void EquipRecruit(PartyMember partyMember)
    {
        uint budget = DCContent.EquipmentBudget(floor);
        var slots = new[] { EquipmentSlot.RightHand, EquipmentSlot.Body, EquipmentSlot.Head, EquipmentSlot.Feet, EquipmentSlot.LeftHand };

        foreach (var slot in slots)
        {
            if (slot == EquipmentSlot.LeftHand)
            {
                var weapon = partyMember.Equipment.Slots[EquipmentSlot.RightHand];

                if (!weapon.Empty && weapon.ItemIndex != 0 && ItemManager.GetItem(weapon.ItemIndex).NumberOfHands == 2)
                    continue;
            }

            var best = Equipment
                .Where(item => item.EquipmentSlot == slot && item.Price <= budget && CharacterHelper.CanEquip(partyMember, item))
                .Where(item => slot != EquipmentSlot.LeftHand || item.NumberOfHands <= 1)
                .OrderByDescending(Value)
                .ThenBy(_ => random.Next())
                .FirstOrDefault();

            if (best == null)
                continue;

            var current = partyMember.Equipment.Slots[slot];
            int currentValue = current.Empty || current.ItemIndex == 0 ? 0 : Value(ItemManager.GetItem(current.ItemIndex));

            if (Value(best) > currentValue)
                CharacterHelper.Equip(partyMember, best.Index, ItemManager);
        }

        CharacterHelper.AddToInventory(partyMember, HealPotion, 2, ItemManager);
    }
}
