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
    /// All party members which were offered to join so far.
    /// </summary>
    public List<uint> OfferedRecruits { get; set; } = [];
}
