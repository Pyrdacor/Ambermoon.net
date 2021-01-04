namespace Ambermoon.Data.Enumerations
{
    public enum ActiveSpellType
    {
        Light,
        Protection,
        Attack,
        AntiMagic,
        Clairvoyance,
        MysticMap
    }

    public static class ActiveSpellTypeExtensions
    {
        public static bool AvailableInBattle(this ActiveSpellType activeSpellType) => activeSpellType switch
        {
            ActiveSpellType.Protection => true,
            ActiveSpellType.Attack => true,
            ActiveSpellType.AntiMagic => true,
            _ => false
        };
    }
}
