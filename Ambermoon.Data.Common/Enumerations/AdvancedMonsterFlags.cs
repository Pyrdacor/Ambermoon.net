using System;

namespace Ambermoon.Data;

[Flags]
public enum AdvancedMonsterFlags : byte
{
    None = 0,
    ImmuneToNonElementalAttacks = 0x01,
    ImmuneToSpiritAttacks = 0x02,
    ImmuneToUndeadAttacks = 0x08,
    ImmuneToEarthAttacks = 0x10,
    ImmuneToWindAttacks = 0x20,
    ImmuneToFireAttacks = 0x40,
    ImmuneToWaterAttacks = 0x80,
}
