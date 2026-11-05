namespace Ambermoon.Data.Enumerations
{
    public enum MonsterAnimationType : byte
    {
        Move, // also used for random idle animation and for moving if there is no AdvancedMove animation
        CloseRangedAttack,
        LongRangedAttack,
        Cast,        
        Hurt,
        Die,
        Start, // played at start of battle
        AdvancedMove // Ambermoon Advanced: used for moving if present (unused in the original)
    }
}
