using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Newtonsoft.Json;

namespace Ambermoon.Mod.AmbermoonDC;

/// <summary>
/// A dungeon theme (loaded from themes/*.json of the mod).
/// Every 5 floors a random theme is chosen. All texture indices refer to the
/// global 3D textures of the base game (Wall3D.amb, Overlay3D.amb, Object3D.amb, Floors.amb).
/// </summary>
internal class ThemeDefinition
{
    /// <summary>
    /// Unique id. Defaults to the file name.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Name per language ("en", "de", ...).
    /// </summary>
    public Dictionary<string, string> Name { get; set; } = [];
    /// <summary>
    /// Map palettes. Each floor uses one of them. They should fit the textures of the theme
    /// (run the palettes command of Ambermoon.Mod.Tests to find fitting palettes).
    /// </summary>
    public List<uint> Palettes { get; set; } = [6];
    /// <summary>
    /// Combat background set (0 to 15).
    /// </summary>
    public uint CombatBackground { get; set; } = 7;
    public uint WallHeight { get; set; } = 300;
    public byte FloorTexture { get; set; }
    public byte FloorColor { get; set; }
    public byte CeilingTexture { get; set; }
    public byte CeilingColor { get; set; }
    /// <summary>
    /// Map music of the floors (one per floor in turn).
    /// </summary>
    public List<uint> Music { get; set; } = [];
    public uint BossMusic { get; set; } = DCContent.BossMusic;
    /// <summary>
    /// 1 to 3 wall variations. Each one is available as a normal wall and as an illusory wall.
    /// </summary>
    public List<ThemeWall> Walls { get; set; } = [];
    public ThemeDoor? Door { get; set; }
    public ThemeObject Chest { get; set; } = null!;
    /// <summary>
    /// The way down to the next floor (hole, stairs, ladder).
    /// </summary>
    public ThemeObject Exit { get; set; } = null!;
    /// <summary>
    /// The way back up at the start of a floor (it is blocked).
    /// </summary>
    public ThemeObject Entrance { get; set; } = null!;
    public ThemeObject? Teleporter { get; set; }
    /// <summary>
    /// Sprite of the companions who can be recruited. Default is the person of labdata 6.
    /// </summary>
    public ThemeObject? RecruitSprite { get; set; }
    /// <summary>
    /// Walkable decoration (bones, stones, junk, ...).
    /// </summary>
    public List<ThemeObject> Decorations { get; set; } = [];
    /// <summary>
    /// Decoration which blocks movement (barrels, statues, coffins, ...).
    /// </summary>
    public List<ThemeObject> Obstacles { get; set; } = [];
    public List<ThemeMonster> Monsters { get; set; } = [];
    /// <summary>
    /// Boss of every 5th floor. The global bosses are used if not given.
    /// </summary>
    public ThemeBoss? Boss { get; set; }
    /// <summary>
    /// Boss with minions of every 10th floor. The global boss groups are used if not given.
    /// </summary>
    public ThemeBossGroup? BossGroup { get; set; }

    public string GetName(GameLanguage language) => Localized(Name, language) ?? Id;

    public static string? Localized(Dictionary<string, string> texts, GameLanguage language)
    {
        string key = language switch
        {
            GameLanguage.German => "de",
            GameLanguage.French => "fr",
            GameLanguage.Polish => "pl",
            GameLanguage.Czech => "cz",
            _ => "en"
        };

        return texts.TryGetValue(key, out var text) ? text : texts.TryGetValue("en", out text) ? text : texts.Values.FirstOrDefault();
    }

    public static ThemeDefinition Parse(string json, string defaultId)
    {
        var theme = JsonConvert.DeserializeObject<ThemeDefinition>(json, ModJson.Settings)
            ?? throw new InvalidDataException("Empty theme.");

        if (string.IsNullOrWhiteSpace(theme.Id))
            theme.Id = defaultId;

        theme.Validate();

        return theme;
    }

    void Validate()
    {
        void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidDataException($"Theme '{Id}': {message}");
        }

        Require(Walls.Count is >= 1 and <= 3, "1 to 3 walls are needed.");
        Require(Music.Count != 0, "At least one music index is needed.");
        Require(Palettes.Count != 0 && Palettes.All(p => p != 0), "At least one palette (1-based) is needed.");
        Require(CombatBackground < 16, "The combat background must be 0 to 15.");
        Require(Chest != null && Exit != null && Entrance != null, "The objects chest, exit and entrance are needed.");

        foreach (var obj in new[] { Chest, Exit, Entrance, Teleporter, RecruitSprite }.Concat(Decorations).Concat(Obstacles).Concat(Monsters.Select(m => m.Sprite)))
            obj?.Validate(message => Require(false, message));

        var monsterIds = new HashSet<string>();

        foreach (var monster in Monsters)
        {
            Require(!string.IsNullOrWhiteSpace(monster.Id) && monsterIds.Add(monster.Id), "Monster ids must be unique and not empty.");
            Require(monster.Template != 0, $"Monster '{monster.Id}' needs a template monster.");
            Require(monster.Sprite != null, $"Monster '{monster.Id}' needs a sprite.");
            Require(monster.Name.Count != 0 && monster.Name.Values.All(n => n.Length is > 0 and <= 15), $"Monster '{monster.Id}' needs a name (up to 15 characters).");
            Require(monster.MinFloor is >= 1 and <= 5, $"The min floor of monster '{monster.Id}' must be 1 to 5.");
            Require(monster.MaxCount is >= 1 and <= 6, $"The max count of monster '{monster.Id}' must be 1 to 6.");

            foreach (var spell in monster.Spells)
            {
                Require(Enum.TryParse<Spell>(spell, true, out var value) && value is > Spell.None and <= Spell.Iceshower,
                    $"Unknown spell '{spell}' of monster '{monster.Id}'.");
            }
        }

        Require(Monsters.Count == 0 || Monsters.Any(m => !m.BossOnly), "At least one monster must not be a boss only monster.");

        if (Boss != null)
            Require(monsterIds.Contains(Boss.Monster), $"Unknown boss monster '{Boss.Monster}'.");

        if (BossGroup != null)
        {
            Require(monsterIds.Contains(BossGroup.Boss), $"Unknown boss monster '{BossGroup.Boss}'.");
            Require(BossGroup.Minions.Count != 0 && BossGroup.Minions.All(monsterIds.Contains), "The boss group needs known minions.");
        }
    }
}

internal class ThemeWall
{
    /// <summary>
    /// Index into Wall3D.amb.
    /// </summary>
    public uint Texture { get; set; }
    public List<ThemeOverlay> Overlays { get; set; } = [];
    /// <summary>
    /// Color on the automap.
    /// </summary>
    public byte Color { get; set; } = 9;
    /// <summary>
    /// How often this wall is used compared to the others.
    /// </summary>
    public int Weight { get; set; } = 1;
}

internal class ThemeOverlay
{
    /// <summary>
    /// Index into Overlay3D.amb.
    /// </summary>
    public uint Texture { get; set; }
    public uint X { get; set; }
    public uint Y { get; set; }
    public uint Width { get; set; }
    public uint Height { get; set; }
    public bool Blend { get; set; }
}

internal class ThemeDoor
{
    /// <summary>
    /// Wall texture of the closed door.
    /// </summary>
    public uint Closed { get; set; }
    /// <summary>
    /// Wall texture of the open door (it should be transparent).
    /// If 0, the door disappears when it is opened.
    /// </summary>
    public uint Open { get; set; }
    public byte Color { get; set; } = 10;
}

/// <summary>
/// An object. Either a copy of an object of an original labdata ("Labdata" and "Object")
/// or a single texture ("Texture", "Width", "Height" and optionally the other values).
/// </summary>
internal class ThemeObject
{
    public uint Labdata { get; set; }
    public uint Object { get; set; }
    /// <summary>
    /// Index into Object3D.amb.
    /// </summary>
    public uint Texture { get; set; }
    public uint Width { get; set; }
    public uint Height { get; set; }
    public uint? MappedWidth { get; set; }
    public uint? MappedHeight { get; set; }
    public uint Frames { get; set; } = 1;
    public short Z { get; set; }

    public bool IsCopy => Labdata != 0;

    public void Validate(Action<string> error)
    {
        if (IsCopy ? Object == 0 : Texture == 0 || Width == 0 || Height == 0)
            error("Objects need a labdata and an object index or a texture index with width and height.");
    }
}

/// <summary>
/// Optional factors for the combat values which are calculated from the level.
/// </summary>
internal class StatFactors
{
    public double HitPoints { get; set; } = 1.0;
    public double SpellPoints { get; set; } = 1.0;
    public double Attack { get; set; } = 1.0;
    public double Defense { get; set; } = 1.0;
    public double MagicAttack { get; set; } = 1.0;
    public double MagicDefense { get; set; } = 1.0;
    public double Attributes { get; set; } = 1.0;
    public double Skills { get; set; } = 1.0;
    public double Experience { get; set; } = 1.0;
    public double Gold { get; set; } = 1.0;
}

internal class ThemeMonster
{
    public string Id { get; set; } = "";
    /// <summary>
    /// Original monster which provides the combat graphic, the animations and the palette.
    /// </summary>
    public uint Template { get; set; }
    /// <summary>
    /// Name per language ("en", "de", ...). Up to 15 characters.
    /// </summary>
    public Dictionary<string, string> Name { get; set; } = [];
    /// <summary>
    /// Sprite on the map.
    /// </summary>
    public ThemeObject Sprite { get; set; } = null!;
    public Class? Class { get; set; }
    public CharacterElement? Element { get; set; }
    public BattleFlags? Flags { get; set; }
    /// <summary>
    /// Spell names (for example "Fireball"). Empty = no spells.
    /// </summary>
    public List<string> Spells { get; set; } = [];
    public byte SpellChance { get; set; } = 50;
    public byte? AttacksPerRound { get; set; }
    public uint Morale { get; set; } = 80;
    /// <summary>
    /// Added to the floor number to get the level of the monster.
    /// </summary>
    public int LevelOffset { get; set; }
    /// <summary>
    /// Maximum number of this monster in a group.
    /// </summary>
    public int MaxCount { get; set; } = 4;
    /// <summary>
    /// How often this monster appears compared to the others.
    /// </summary>
    public int Weight { get; set; } = 1;
    /// <summary>
    /// First floor (within the 5 floors of the theme, 1 to 5) on which the monster appears.
    /// </summary>
    public int MinFloor { get; set; } = 1;
    /// <summary>
    /// The monster only appears as a boss or boss minion.
    /// </summary>
    public bool BossOnly { get; set; }
    public StatFactors Factors { get; set; } = new();
}

internal class ThemeBoss
{
    public string Monster { get; set; } = "";
    /// <summary>
    /// Factors which are applied in addition to the factors of the monster.
    /// </summary>
    public StatFactors Factors { get; set; } = new();
}

internal class ThemeBossGroup
{
    public string Boss { get; set; } = "";
    public List<string> Minions { get; set; } = [];
    /// <summary>
    /// Factors for the boss which are applied in addition to the factors of the monster.
    /// </summary>
    public StatFactors Factors { get; set; } = new();
}
