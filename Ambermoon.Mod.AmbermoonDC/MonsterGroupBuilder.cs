using Ambermoon.Data;

namespace Ambermoon.Mod.AmbermoonDC;

/// <summary>
/// Creates the monster groups of a floor based on the original monster groups.
/// Deeper floors use stronger groups. Beyond the last tier the monsters are scaled up.
/// </summary>
internal class MonsterGroupBuilder(ModApi api, int seed, int floor)
{
    readonly ModApi api = api;
    readonly int floor = floor;
    readonly System.Random random = FloorGenerator.CreateRandom(seed, floor, 5);

    /// <summary>
    /// Scale factor for monster stats on very deep floors.
    /// </summary>
    double Scale => floor > 36 ? 1.0 + (floor - 36) * 0.05 : 1.0;

    public (MonsterGroup Group, string Sprite) CreateGroup()
    {
        int tierIndex = Array.FindLastIndex(DCContent.Tiers, t => t.FirstFloor <= floor);

        // Sometimes use a group of the previous tier for some variety.
        if (tierIndex > 0 && random.Next(100) < 25)
            tierIndex--;

        var groups = DCContent.Tiers[tierIndex].Groups;
        var group = api.GetMonsterGroup(groups[random.Next(groups.Length)])!;

        if (Scale > 1.0)
            group = CloneGroup(group, Scale);

        return (group, GetSprite(group));
    }

    /// <summary>
    /// Creates the boss group of the floor. Bosses on every 10th floor have minions.
    /// The boss carries the given item as loot.
    /// </summary>
    public (MonsterGroup Group, string Sprite) CreateBossGroup(bool withMinions, uint lootItem)
    {
        MonsterGroup group;
        Monster boss;

        if (withMinions)
        {
            int index = floor / 10 - 1;
            int cycle = index / DCContent.BossGroups.Length;
            var baseGroup = api.GetMonsterGroup(DCContent.BossGroups[index % DCContent.BossGroups.Length])!;
            group = CloneGroup(baseGroup, Scale * (1.0 + cycle));
            boss = Monsters(group).MaxBy(m => m.DefeatExperience)!;
        }
        else
        {
            int index = floor / 10;
            int cycle = index / DCContent.SoloBossMonsters.Length;
            uint monsterIndex = DCContent.SoloBossMonsters[index % DCContent.SoloBossMonsters.Length];
            boss = CloneMonster(api.GetMonster(monsterIndex)!, Scale * (1.5 + cycle));
            group = new MonsterGroup();
            var (column, row) = FindPosition(monsterIndex);
            group.Monsters[column, row] = boss;
        }

        if (lootItem != 0)
            AddLoot(boss, lootItem);

        boss.Gold = (ushort)Math.Min(ushort.MaxValue, boss.Gold + 50 * floor);

        return (group, GetSprite(boss.Index));
    }

    void AddLoot(Monster monster, uint itemIndex)
    {
        var slot = monster.Inventory.Slots.FirstOrDefault(s => s.Empty);

        if (slot == null)
            return;

        int amount = 1;
        slot.Replace(ItemSlot.CreateFromItem(api.GameData.ItemManager, itemIndex, ref amount));
    }

    /// <summary>
    /// Position of the monster in an original group (column, row) so that it is placed nicely.
    /// </summary>
    (int Column, int Row) FindPosition(uint monsterIndex)
    {
        foreach (var group in api.GameData.CharacterManager.MonsterGroups.Values)
        {
            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 6; column++)
                {
                    if (group.Monsters[column, row]?.Index == monsterIndex)
                        return (column, row);
                }
            }
        }

        return (2, 2);
    }

    static IEnumerable<Monster> Monsters(MonsterGroup group) => group.Monsters.Cast<Monster?>().Where(m => m != null).Cast<Monster>();

    MonsterGroup CloneGroup(MonsterGroup group, double scale)
    {
        var clone = new MonsterGroup();

        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 6; column++)
            {
                var monster = group.Monsters[column, row];

                if (monster != null)
                    clone.Monsters[column, row] = CloneMonster(monster, scale);
            }
        }

        return clone;
    }

    /// <summary>
    /// Creates a copy of the monster with scaled stats. The monster index stays
    /// the same so the battle graphics are found.
    /// </summary>
    Monster CloneMonster(Monster monster, double scale)
    {
        var clone = api.CloneMonster(monster);

        if (scale != 1.0)
        {
            // Note: The real hit points of a monster are stored as the current value.
            clone.HitPoints.CurrentValue = (uint)Math.Min(30000, clone.HitPoints.CurrentValue * scale);
            clone.HitPoints.MaxValue = Math.Max(clone.HitPoints.MaxValue, clone.HitPoints.CurrentValue);
            clone.BaseAttackDamage = (short)Math.Min(short.MaxValue, clone.BaseAttackDamage * scale);
            clone.BaseDefense = (short)Math.Min(short.MaxValue, clone.BaseDefense * scale);
            clone.DefeatExperience = (ushort)Math.Min(ushort.MaxValue, clone.DefeatExperience * scale);
        }

        return clone;
    }

    static string GetSprite(MonsterGroup group)
    {
        // The most common monster defines the look. On equal counts the strongest one wins.
        var monster = Monsters(group)
            .GroupBy(m => m.Index)
            .OrderByDescending(g => g.Count())
            .ThenByDescending(g => g.First().DefeatExperience)
            .First().First();

        return GetSprite(monster.Index);
    }

    static string GetSprite(uint monsterIndex) => DCContent.MonsterSprites.TryGetValue(monsterIndex, out var sprite) ? sprite : "undead";
}
