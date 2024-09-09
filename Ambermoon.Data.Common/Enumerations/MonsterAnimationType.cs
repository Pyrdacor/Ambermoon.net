namespace Ambermoon.Data.Enumerations
{
    public enum MonsterAnimationType : byte
    {
        Move, // also used for random idle animation
        CloseRangedAttack,
        LongRangedAttack,
        Cast,        
        Hurt,
        Die,
        Start, // played at start of battle
        Unknown3
    }
}
