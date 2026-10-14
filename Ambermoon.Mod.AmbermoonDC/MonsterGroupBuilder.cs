using Ambermoon.Data;

namespace Ambermoon.Mod.AmbermoonDC;

/// <summary>
/// Creates the monster groups of a floor from the monsters of the theme.
/// Their combat values depend on the floor. If the theme has no monsters (or no bosses),
/// the original monster groups are used. Deeper floors use stronger groups then and
/// beyond the last tier the monsters are scaled up.
/// </summary>
internal class MonsterGroupBuilder(ModApi api, DungeonTheme theme, int seed, int floor)
{
    readonly ModApi api = api;
    readonly DungeonTheme theme = theme;
    readonly int floor = floor;
    readonly System.Random random = FloorGenerator.CreateRandom(seed, floor, 5);

    /// <summary>
    /// Additional factors for bosses.
    /// </summary>
    static readonly StatFactors BossFactors = new()
    {
        HitPoints = 2.5,
        SpellPoints = 2.0,
        Attack = 1.2,
        Defense = 1.3,
        MagicAttack = 1.5,
        MagicDefense = 1.5,
        Attributes = 1.1,
        Skills = 1.1,
        Experience = 5.0,
        Gold = 5.0
    };

    // Positions in a group: the front row (2) is filled from the center.
    static readonly int[] Columns = [2, 3, 1, 4, 0, 5];

    /// <summary>
    /// Scale factor for monster stats on very deep floors.
    /// </summary>
    double Scale => floor > 36 ? 1.0 + (floor - 36) * 0.05 : 1.0;

    public (MonsterGroup Group, string Sprite) CreateGroup()
    {
        if (theme.Definition.Monsters.Count != 0)
            return CreateThemeGroup();

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
        if (withMinions ? theme.Definition.BossGroup != null : theme.Definition.Boss != null)
            return CreateThemeBossGroup(withMinions, lootItem);

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

    (MonsterGroup Group, string Sprite) CreateThemeGroup()
    {
        int floorOfTheme = (floor - 1) % 5 + 1;
        var candidates = theme.Monsters.Where(m => !m.Definition.BossOnly && m.Definition.MinFloor <= floorOfTheme).ToList();

        if (candidates.Count == 0)
            candidates = [.. theme.Monsters.Where(m => !m.Definition.BossOnly)];

        (ThemeMonster Definition, uint Index) Pick()
        {
            int value = random.Next(candidates.Sum(c => Math.Max(1, c.Definition.Weight)));

            foreach (var candidate in candidates)
            {
                value -= Math.Max(1, candidate.Definition.Weight);

                if (value < 0)
                    return candidate;
            }

            return candidates[0];
        }

        int count = Math.Clamp(1 + random.Next(2) + floor / 5, 1, 6);
        var kinds = new List<(ThemeMonster Definition, uint Index)> { Pick() };

        if (count >= 2 && random.Next(100) < 40)
        {
            var second = Pick();

            if (second.Index != kinds[0].Index)
                kinds.Add(second);
        }

        // The first kind is the main monster of the group.
        var members = new List<(ThemeMonster Definition, uint Index)>();

        for (int i = 0; members.Count < count; i++)
        {
            var available = kinds.Where(k => members.Count(m => m.Index == k.Index) < k.Definition.MaxCount).ToList();

            if (available.Count == 0)
                break;

            members.Add(available[i % available.Count]);
        }

        var group = new MonsterGroup();

        foreach (var member in members)
        {
            var monster = CreateThemeMonster(member, floor + member.Definition.LevelOffset + random.Next(-1, 2), null);
            // Spell casters prefer the back row.
            Place(group, monster, monster.HasAnySpell() && monster.SpellChancePercentage >= 50 ? 1 : 2);
        }

        return (group, DungeonTheme.SpriteKey(kinds[0].Definition));
    }

    (MonsterGroup Group, string Sprite) CreateThemeBossGroup(bool withMinions, uint lootItem)
    {
        var group = new MonsterGroup();
        var definition = theme.Definition;
        var bossMonster = theme.GetMonster(withMinions ? definition.BossGroup!.Boss : definition.Boss!.Monster);
        var bossFactors = Multiply(BossFactors, withMinions ? definition.BossGroup!.Factors : definition.Boss!.Factors);
        var boss = CreateThemeMonster(bossMonster, floor + 1 + bossMonster.Definition.LevelOffset, bossFactors);
        boss.BattleFlags |= BattleFlags.Boss;
        boss.Morale = 100;

        if (withMinions)
        {
            group.Monsters[2, 1] = boss;
            var minions = definition.BossGroup!.Minions.Select(theme.GetMonster).ToList();
            int count = Math.Min(5, 3 + floor / 20);

            for (int i = 0; i < count; i++)
            {
                var minion = minions[random.Next(minions.Count)];
                Place(group, CreateThemeMonster(minion, floor + minion.Definition.LevelOffset, null), 2);
            }
        }
        else
        {
            group.Monsters[2, 2] = boss;
        }

        if (lootItem != 0)
            AddLoot(boss, lootItem);

        return (group, DungeonTheme.SpriteKey(bossMonster.Definition));
    }

    Monster CreateThemeMonster((ThemeMonster Definition, uint Index) monster, int level, StatFactors? extraFactors)
    {
        var clone = api.CloneMonster(api.GetMonster(monster.Index)!);
        MonsterStats.Apply(clone, level, monster.Definition, extraFactors);
        return clone;
    }

    static void Place(MonsterGroup group, Monster monster, int preferredRow)
    {
        foreach (int row in new[] { preferredRow, 3 - preferredRow, 0 })
        {
            foreach (int column in Columns)
            {
                if (group.Monsters[column, row] == null)
                {
                    group.Monsters[column, row] = monster;
                    return;
                }
            }
        }
    }

    static StatFactors Multiply(StatFactors a, StatFactors b) => new()
    {
        HitPoints = a.HitPoints * b.HitPoints,
        SpellPoints = a.SpellPoints * b.SpellPoints,
        Attack = a.Attack * b.Attack,
        Defense = a.Defense * b.Defense,
        MagicAttack = a.MagicAttack * b.MagicAttack,
        MagicDefense = a.MagicDefense * b.MagicDefense,
        Attributes = a.Attributes * b.Attributes,
        Skills = a.Skills * b.Skills,
        Experience = a.Experience * b.Experience,
        Gold = a.Gold * b.Gold
    };

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
