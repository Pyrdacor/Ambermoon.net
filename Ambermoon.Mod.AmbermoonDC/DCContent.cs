namespace Ambermoon.Mod.AmbermoonDC;

/// <summary>
/// Data tables of the dungeon crawler. All indices refer to the original Ambermoon 1.20 data.
/// </summary>
internal static class DCContent
{
    /// <summary>
    /// Sprites of the original monsters and persons which are copied into the labdata of each theme:
    /// key = name, value = (labdata index, object index).
    /// </summary>
    public static readonly Dictionary<string, (uint Labdata, uint Object)> Sprites = new()
    {
        { "newt", (2, 13) },
        { "spider", (2, 14) },
        { "gigantula", (2, 26) },
        { "undead", (7, 8) },
        { "bandit", (4, 14) },
        { "masterthief", (4, 15) },
        { "lizard", (4, 11) },
        { "golem", (5, 1) },
        { "guardgolem", (5, 4) },
        { "magicguard", (5, 27) },
        { "thornahuun", (5, 29) },
        { "orc", (6, 4) },
        { "giant", (6, 5) },
        { "minotaur", (6, 33) },
        { "person", (6, 6) },
        { "gargoyle", (26, 7) },
        { "drek", (26, 8) },
        { "beast", (26, 12) },
        { "minelizard", (29, 10) },
        { "minespider", (29, 11) },
        { "firedragon", (9, 20) },
        { "luminor", (9, 19) },
        { "demon", (30, 6) },
        { "nera", (30, 7) },
        { "moragdragon", (35, 8) },
    };

    /// <summary>
    /// Sprite for each monster index.
    /// </summary>
    public static readonly Dictionary<uint, string> MonsterSprites = new()
    {
        { 1, "newt" }, { 4, "newt" },
        { 2, "spider" }, { 3, "spider" }, { 5, "gigantula" },
        { 6, "undead" }, { 7, "undead" }, { 19, "undead" }, { 20, "undead" }, { 21, "undead" },
        { 9, "bandit" }, { 10, "bandit" }, { 13, "masterthief" },
        { 12, "lizard" },
        { 14, "golem" }, { 15, "golem" }, { 40, "guardgolem" },
        { 16, "orc" }, { 17, "orc" }, { 18, "giant" }, { 22, "giant" },
        { 38, "minotaur" },
        { 29, "gargoyle" }, { 37, "gargoyle" }, { 30, "drek" }, { 36, "beast" },
        { 32, "minelizard" }, { 33, "minespider" },
        { 23, "firedragon" }, { 24, "luminor" }, { 25, "luminor" }, { 26, "luminor" }, { 27, "luminor" }, { 28, "luminor" },
        { 35, "demon" }, { 34, "nera" },
        { 42, "magicguard" }, { 43, "magicguard" }, { 44, "thornahuun" },
        { 47, "moragdragon" },
    };

    /// <summary>
    /// Monster groups by tier (used for themes without monsters). The first tier is used for floors 1-3 etc.
    /// </summary>
    public static readonly (int FirstFloor, uint[] Groups)[] Tiers =
    [
        (1, [2, 3, 5, 4, 8, 10, 26]),
        (4, [6, 7, 11, 27, 13, 63, 21, 33]),
        (8, [28, 29, 14, 22, 31, 32, 44, 55, 35]),
        (13, [23, 30, 45, 56, 57, 65, 51, 64]),
        (19, [24, 46, 50, 58, 36, 38, 53, 61]),
        (26, [60, 37, 62, 67, 68, 73, 74]),
        (36, [76, 68, 74, 60, 62, 53]),
    ];

    /// <summary>
    /// Bosses without minions (every 5th floor). Index = (floor / 10).
    /// </summary>
    public static readonly uint[] SoloBossMonsters = [5, 22, 36, 44, 47];

    /// <summary>
    /// Bosses with minions (every 10th floor). Index = (floor / 10 - 1).
    /// </summary>
    public static readonly uint[] BossGroups = [30, 34, 47, 52, 69];

    // Items
    public const uint Torch = 130;
    public const uint Lantern = 243;
    public static readonly uint[] HealPotions = [211, 212, 213, 214, 215];
    public static readonly uint[] ManaPotions = [220, 221, 222, 223, 224];
    public const uint Antidote = 217;
    public const uint WaterOfLife = 254;
    public static readonly uint[] EarlyScrolls = [10, 17, 41, 76, 77, 80, 100, 114];
    public static readonly uint[] MidScrolls = [20, 27, 42, 50, 101, 115, 118, 122];
    public static readonly uint[] LateScrolls = [29, 43, 123, 124, 127, 128, 129];
    public static readonly uint[] Rings = [302, 303, 304, 305, 306, 333];
    public static readonly uint[] Ammunition = [202, 203, 204];

    /// <summary>
    /// Start equipment of the hero (equipped if possible, otherwise put into the inventory).
    /// </summary>
    public static readonly (uint Item, int Amount)[] StartEquipment =
    [
        (182, 1), // dagger
        (167, 1), // leather armor
        (177, 1), // small shield
        (162, 1), // cap
        (174, 1), // sandals
    ];

    public static readonly (uint Item, int Amount)[] StartItems =
    [
        (Torch, 6),
        (211, 4), // healing potions
        (217, 1), // antidote
    ];

    /// <summary>
    /// Maximum item price for equipment found on the given floor.
    /// </summary>
    public static uint EquipmentBudget(int floor) => (uint)(150 + floor * floor * 12);

    /// <summary>
    /// Default map music of boss floors.
    /// </summary>
    public const uint BossMusic = 26;
}
