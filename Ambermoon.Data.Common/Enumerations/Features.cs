
using System;

namespace Ambermoon.Data.Enumerations
{
    [Flags]
    public enum Features : uint
    {
        None = 0x00000000,
        Elements = 0x00000001,
        AdjustedSpellDamage = 0x00000002,
        SpellDamageBonus = 0x00000004,
        ReducedFoodWeight = 0x00000008,
        AdjustedSPAndSLP = 0x00000010,
        AdjustedEPFactors = 0x00000020,
        SageScrollIdentification = 0x00000040,
        AdvancedSpells = 0x00000080,
        WaspTransport = 0x00000100,
        AdvancedCombatBackgrounds = 0x00000200,
		ClairvoyanceGrantsSearchSkill = 0x00000400,
        ExtendedCurseEffects = 0x00000800,
        AdvancedMonsterFlags = 0x00001000, // TODO: Later add to AmbermoonAdvanced
        ItemElements = 0x00002000, // TODO: Later add to AmbermoonAdvanced
        ExtendedLanguages = 0x00004000, // TODO: Later add to AmbermoonAdvanced
        AdvancedAPRCalculation = 0x00008000,
        AdjustedWeaponDamage = 0x00010000,
        StaminaHPOnLevelUp = 0x00020000,
        AmbermoonAdvanced =
            Elements |
            AdjustedSpellDamage |
            SpellDamageBonus |
            ReducedFoodWeight |
            AdjustedSPAndSLP |
            AdjustedEPFactors |
            SageScrollIdentification |
            AdvancedSpells |
            WaspTransport |
            AdvancedCombatBackgrounds |
            ClairvoyanceGrantsSearchSkill |
            ExtendedCurseEffects |
            AdvancedAPRCalculation |
            AdjustedWeaponDamage |
            StaminaHPOnLevelUp
    }
}
