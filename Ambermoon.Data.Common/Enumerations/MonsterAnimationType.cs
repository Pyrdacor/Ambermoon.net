namespace Ambermoon.Data.Enumerations
{
    public enum MonsterAnimationType : byte
    {
        Move, // also used for random idle animation
        Attack,
        Unknown1,
        Cast,        
        Hurt,
        Unknown2,
        Start, // played at start of battle
        Unknown3
    }
}
