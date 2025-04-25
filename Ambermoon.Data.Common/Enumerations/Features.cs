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
        AdvancedSpells = 0x80,
        WaspTransport = 0x100,
        AdvancedCombatBackgrounds = 0x200,
		ClairvoyanceGrantsSearchSkill = 0x400,
        ExtendedCurseEffects = 0x800,
        AdvancedMonsterFlags = 0x1000, // TODO: Later add to AmbermoonAdvanced
        ItemElements = 0x2000, // TODO: Later add to AmbermoonAdvanced
        AmbermoonAdvanced = Elements | AdjustedSpellDamage | SpellDamageBonus | ReducedFoodWeight | AdjustedSPAndSLP | AdjustedEPFactors | SageScrollIdentification | AdvancedSpells | WaspTransport | AdvancedCombatBackgrounds | ClairvoyanceGrantsSearchSkill | ExtendedCurseEffects
    }
}
