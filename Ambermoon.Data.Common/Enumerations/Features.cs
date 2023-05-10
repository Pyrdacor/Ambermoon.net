using System;

namespace Ambermoon.Data.Enumerations
{
    [Flags]
    public enum Features : ushort
    {
        None = 0x00,
        Elements = 0x01,
        AdjustedSpellDamage = 0x02,
        SpellDamageBonus = 0x04,
        ReducedFoodWeight = 0x08,
        AdjustedSPAndSLP = 0x10,
        AdjustedEPFactors = 0x20,
        SageScrollIdentification = 0x40,
        AdvancedSpells = 0x80, // TODO: Add later to AmbermoonAdvanced
        WaspTransport = 0x100, // TODO: Add later to AmbermoonAdvanced
        AmbermoonAdvanced = Elements | AdjustedSpellDamage | SpellDamageBonus | ReducedFoodWeight | AdjustedSPAndSLP | AdjustedEPFactors | SageScrollIdentification
    }
}
