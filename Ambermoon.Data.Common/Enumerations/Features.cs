using System;

namespace Ambermoon.Data.Enumerations
{
    [Flags]
    public enum Features
    {
        None = 0x00,
        Elements = 0x01,
        AdjustedSpellDamage = 0x02,
        SpellDamageBonus = 0x04,
        ReducedFoodWeight = 0x08,
        AmbermoonAdvanced = Elements | AdjustedSpellDamage | SpellDamageBonus | ReducedFoodWeight
    }
}
