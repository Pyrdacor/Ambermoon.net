namespace Ambermoon.Mod.AmbermoonDC;

/// <summary>
/// The state of the dungeon run which is stored with each savegame.
/// Everything else (opened chests, defeated monsters, ...) is stored in the
/// savegame itself as the floors use regular map, chest and door slots.
/// </summary>
internal class DCState
{
    public int Seed { get; set; }
    public int Floor { get; set; } = 1;
    /// <summary>
    /// Item the boss of the current floor drops (0 = none).
    /// </summary>
    public uint BossItem { get; set; }
    /// <summary>
    /// Party member which can be recruited on the current floor (0 = none).
    /// </summary>
    public uint Recruit { get; set; }
    /// <summary>
    /// Party member slots used for recruits so far (oldest first).
    /// </summary>
    public List<uint> UsedRecruitSlots { get; set; } = [];
    /// <summary>
    /// The texts of the recruits by party member index. Texts of party members
    /// are not stored in savegames, so they are restored when a game is loaded.
    /// </summary>
    public Dictionary<uint, List<string>> RecruitTexts { get; set; } = [];
}
