using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.Mod.Assets;
using Ambermoon.Mod.Packaging;
using static Ambermoon.Data.Tileset;

namespace Ambermoon.Mod.AmbermoonDC;

/// <summary>
/// A loaded dungeon theme: its labdata and its monsters.
/// </summary>
internal class DungeonTheme
{
    /// <summary>
    /// Labdata index of the first theme (the original game uses 1 to 35).
    /// </summary>
    public const uint FirstLabdataIndex = 60;
    /// <summary>
    /// Monster index of the first theme monster (the original game uses up to 100 or so).
    /// </summary>
    public const uint FirstMonsterIndex = 300;

    const TileFlags WallFlags = TileFlags.BlockAllMovement | TileFlags.BlockSight;
    const TileFlags IllusoryWallFlags = TileFlags.BlockSight | TileFlags.AllowMovementWalk;
    const TileFlags OpenDoorFlags = TileFlags.Transparency | TileFlags.AllowMovementWalk;
    const TileFlags WalkableFlags = TileFlags.AllowMovementWalk | TileFlags.AllowMovementMonster;
    const TileFlags BlockingFlags = TileFlags.BlockAllMovement;

    readonly List<(uint Wall, uint IllusoryWall, int Weight)> walls = [];
    readonly Dictionary<string, uint> spriteObjects = [];
    readonly Dictionary<string, (ThemeMonster Definition, uint Index)> monsters = [];

    public ThemeDefinition Definition { get; }
    public uint LabdataIndex { get; }
    public Labdata Labdata { get; private set; } = null!;
    public uint DoorWall { get; private set; }
    /// <summary>
    /// 0 if the door disappears when it is opened.
    /// </summary>
    public uint OpenDoorWall { get; private set; }
    public uint ChestObject { get; private set; }
    public uint ExitObject { get; private set; }
    public uint EntranceObject { get; private set; }
    /// <summary>
    /// 0 if the theme has no teleporters.
    /// </summary>
    public uint TeleporterObject { get; private set; }
    public List<uint> DecorationObjects { get; } = [];
    public List<uint> ObstacleObjects { get; } = [];

    public IEnumerable<(ThemeMonster Definition, uint Index)> Monsters => monsters.Values;

    DungeonTheme(ThemeDefinition definition, uint labdataIndex)
    {
        Definition = definition;
        LabdataIndex = labdataIndex;
    }

    /// <summary>
    /// Loads all themes of the mod (themes/*.json, ordered by file name).
    /// Must be called before the game is created, as the battle graphics
    /// of the new monsters are prepared when the game is created.
    /// </summary>
    public static List<DungeonTheme> LoadAll(ModApi api)
    {
        var themes = new List<DungeonTheme>();
        uint nextMonsterIndex = FirstMonsterIndex;

        foreach (var file in api.Source.ListFiles("themes").Where(f => f.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
        {
            var definition = ThemeDefinition.Parse(api.Source.ReadAllText(file), Path.GetFileNameWithoutExtension(file));

            if (themes.Any(t => t.Definition.Id == definition.Id))
                throw new InvalidDataException($"Theme id '{definition.Id}' is used twice.");

            var theme = new DungeonTheme(definition, FirstLabdataIndex + (uint)themes.Count);
            theme.BuildLabdata(api);
            theme.CreateMonsters(api, ref nextMonsterIndex);
            themes.Add(theme);
        }

        if (themes.Count == 0)
            throw new InvalidDataException("The mod contains no dungeon themes.");

        return themes;
    }

    /// <summary>
    /// Theme of the given floor. Every 5 floors a random theme is chosen.
    /// The same theme is never used twice in a row (if there are at least 2 themes).
    /// </summary>
    public static int ThemeIndexForFloor(int seed, int floor, int themeCount)
    {
        int block = (floor - 1) / 5;
        int theme = -1;

        for (int b = 0; b <= block; b++)
        {
            var random = FloorGenerator.CreateRandom(seed, b, 7);
            int previous = theme;

            do
                theme = random.Next(themeCount);
            while (themeCount > 1 && theme == previous);
        }

        return theme;
    }

    public uint RandomWall(System.Random random)
    {
        int value = random.Next(walls.Sum(w => w.Weight));

        foreach (var wall in walls)
        {
            if (value < wall.Weight)
                return wall.Wall;

            value -= wall.Weight;
        }

        return walls[0].Wall;
    }

    public uint RandomIllusoryWall(System.Random random) => walls[random.Next(walls.Count)].IllusoryWall;

    public uint MusicForFloor(int floor) => Definition.Music[(floor - 1) % Definition.Music.Count];

    public string Name(GameLanguage language) => Definition.GetName(language);

    public uint GetSpriteObject(string sprite) => spriteObjects.TryGetValue(sprite, out var obj) ? obj : spriteObjects["undead"];

    public static string SpriteKey(ThemeMonster monster) => "theme:" + monster.Id;

    public (ThemeMonster Definition, uint Index) GetMonster(string id) => monsters[id];

    void BuildLabdata(ModApi api)
    {
        var definition = Definition;
        var builder = new LabdataBuilder(api, definition.CombatBackground, definition.WallHeight);

        builder.SetFloor(definition.FloorTexture, definition.FloorColor);
        builder.SetCeiling(definition.CeilingTexture, definition.CeilingColor);

        foreach (var wall in definition.Walls)
        {
            var overlays = wall.Overlays.Select(o => new WallOverlay(o.Texture, o.X, o.Y, o.Width, o.Height, o.Blend)).ToList();
            uint normal = builder.AddWall(wall.Texture, WallFlags, AutomapType.Wall, wall.Color, overlays);
            uint illusory = builder.AddWall(wall.Texture, IllusoryWallFlags, AutomapType.Wall, wall.Color, overlays);
            walls.Add((normal, illusory, Math.Max(1, wall.Weight)));
        }

        var door = definition.Door ?? new ThemeDoor { Closed = 117, Open = 118 };
        DoorWall = builder.AddWall(door.Closed, WallFlags, AutomapType.Door, door.Color);
        OpenDoorWall = door.Open == 0 ? 0 : builder.AddWall(door.Open, OpenDoorFlags, AutomapType.DoorOpen, door.Color);

        uint AddObject(ThemeObject obj, AutomapType automapType, TileFlags flags, bool keepFlagsOfCopy = false)
        {
            if (obj.IsCopy)
                return builder.CopyObject(obj.Labdata, obj.Object, keepFlagsOfCopy ? null : flags);

            return builder.AddObject(obj.Texture, obj.Width, obj.Height, flags, automapType, obj.MappedWidth, obj.MappedHeight, obj.Frames, obj.Z);
        }

        ChestObject = AddObject(definition.Chest, AutomapType.Chest, BlockingFlags);
        ExitObject = AddObject(definition.Exit, AutomapType.Exit, TileFlags.AllowMovementWalk);
        EntranceObject = AddObject(definition.Entrance, AutomapType.Exit, TileFlags.AllowMovementWalk);
        TeleporterObject = definition.Teleporter == null ? 0 : AddObject(definition.Teleporter, AutomapType.Teleporter, TileFlags.AllowMovementWalk);

        foreach (var decoration in definition.Decorations)
            DecorationObjects.Add(AddObject(decoration, AutomapType.None, WalkableFlags));

        foreach (var obstacle in definition.Obstacles)
            ObstacleObjects.Add(AddObject(obstacle, AutomapType.None, BlockingFlags));

        foreach (var monster in definition.Monsters)
        {
            spriteObjects[SpriteKey(monster)] = AddObject(monster.Sprite, AutomapType.Monster,
                TileFlags.RandomAnimationStart | TileFlags.AllowMovementWalk, true);
        }

        // Sprites of the original monsters (for the global boss lists and if a theme has no monsters) and the recruits.
        foreach (var sprite in DCContent.Sprites)
            spriteObjects[sprite.Key] = builder.CopyObject(sprite.Value.Labdata, sprite.Value.Object);

        if (builder.Labdata.Objects.Count > 100)
            throw new InvalidDataException($"Theme '{definition.Id}' has too many objects.");

        Labdata = builder.Labdata;
        api.Maps.SetLabdata(LabdataIndex, Labdata);
    }

    void CreateMonsters(ModApi api, ref uint nextIndex)
    {
        foreach (var definition in Definition.Monsters)
        {
            var template = api.GetMonster(definition.Template)
                ?? throw new InvalidDataException($"Theme '{Definition.Id}': There is no template monster {definition.Template}.");
            var monster = api.CloneMonster(template);

            monster.Index = nextIndex++;
            monster.Name = ThemeDefinition.Localized(definition.Name, api.Language)!.ToUpper();
            monster.Class = definition.Class ?? template.Class;
            monster.Element = definition.Element ?? template.Element;
            monster.BattleFlags = definition.Flags ?? (template.BattleFlags & ~BattleFlags.Boss);
            monster.Morale = definition.Morale;
            monster.SpellChancePercentage = definition.Spells.Count == 0 ? (byte)0 : definition.SpellChance;
            monster.LearnedHealingSpells = 0;
            monster.LearnedAlchemisticSpells = 0;
            monster.LearnedMysticSpells = 0;
            monster.LearnedDestructionSpells = 0;
            monster.LearnedSpellsType7 = 0;

            foreach (var spell in definition.Spells)
                monster.AddSpell(Enum.Parse<Spell>(spell, true));

            foreach (var slot in monster.Equipment.Slots.Values)
                slot.Clear();

            foreach (var slot in monster.Inventory.Slots)
                slot.Clear();

            monster.BonusAttackDamage = 0;
            monster.BonusDefense = 0;
            MonsterStats.Apply(monster, 1, definition, null);
            api.Characters.SetMonster(monster);
            monsters[definition.Id] = (definition, monster.Index);
        }
    }
}

/// <summary>
/// Combat values of the theme monsters. They are calculated from the level
/// and the optional factors of the theme.
/// </summary>
internal static class MonsterStats
{
    static uint Value(double value, double factor, uint max) => (uint)Math.Clamp(Math.Round(value * factor), 0, max);

    public static void Apply(Monster monster, int level, ThemeMonster definition, StatFactors? extraFactors)
    {
        var f = definition.Factors;
        var e = extraFactors ?? new StatFactors();
        int l = Math.Clamp(level, 1, 99);
        bool hasSpells = monster.HasAnySpell();

        monster.Level = (byte)l;

        uint hitPoints = Value(8 + 6 * l + 0.15 * l * l, f.HitPoints * e.HitPoints, 30000);
        monster.HitPoints.MaxValue = Math.Max(1, hitPoints);
        monster.HitPoints.CurrentValue = monster.HitPoints.MaxValue; // the real hit points of a monster
        uint spellPoints = hasSpells ? Value(10 + 8 * l, f.SpellPoints * e.SpellPoints, 30000) : 0;
        monster.SpellPoints.MaxValue = spellPoints;
        monster.SpellPoints.CurrentValue = spellPoints;

        monster.BaseAttackDamage = (short)Value(4 + 1.8 * l, f.Attack * e.Attack, 1000);
        monster.BaseDefense = (short)Value(0.5 * l, f.Defense * e.Defense, 1000);
        monster.MagicAttack = (short)Value(l / 5.0, f.MagicAttack * e.MagicAttack, 100);
        monster.MagicDefense = (short)Value(l / 8.0, f.MagicDefense * e.MagicDefense, 100);
        monster.AttacksPerRound = definition.AttacksPerRound ?? (byte)Math.Min(4, 1 + l / 15);
        monster.DefeatExperience = (ushort)Value(8 * l + 1.5 * l * l, f.Experience * e.Experience, ushort.MaxValue);
        monster.Gold = (ushort)Value(2 + 3 * l, f.Gold * e.Gold, ushort.MaxValue);

        void SetAttribute(Data.Attribute attribute, uint value)
        {
            monster.Attributes[attribute].CurrentValue = value;
            monster.Attributes[attribute].MaxValue = Math.Max(monster.Attributes[attribute].MaxValue, value);
        }

        void SetSkill(Skill skill, uint value)
        {
            monster.Skills[skill].CurrentValue = value;
            monster.Skills[skill].MaxValue = Math.Max(monster.Skills[skill].MaxValue, value);
        }

        double attributeFactor = f.Attributes * e.Attributes;
        uint attributeValue = Value(25 + 1.5 * l, attributeFactor, 99);

        foreach (var attribute in new[] { Data.Attribute.Strength, Data.Attribute.Intelligence, Data.Attribute.Dexterity, Data.Attribute.Speed, Data.Attribute.Stamina })
            SetAttribute(attribute, attributeValue);

        SetAttribute(Data.Attribute.Luck, Value(20 + l / 2.0, attributeFactor, 99));
        SetAttribute(Data.Attribute.AntiMagic, Value(l / 2.0, attributeFactor, 60));

        double skillFactor = f.Skills * e.Skills;
        SetSkill(Skill.Attack, Value(40 + 1.5 * l, skillFactor, 99));
        SetSkill(Skill.Parry, Value(20 + 1.2 * l, skillFactor, 99));
        SetSkill(Skill.CriticalHit, Value(l / 4.0, skillFactor, 15));
        SetSkill(Skill.UseMagic, hasSpells ? Value(50 + 1.5 * l, skillFactor, 99) : 0);
    }
}
