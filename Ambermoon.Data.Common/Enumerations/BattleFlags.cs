using System;

namespace Ambermoon.Data;

[Flags]
public enum BattleFlags : byte
{
    None = 0,
    Undead = 0x01, // can be killed by holy spells
    Demon = 0x02,
    Boss = 0x04, // immune to Fear, Paralyze, Petrify, DissolveVictim, Madness, Drugs, Irritation and won't flee
    Animal = 0x08,
    EarthSpellDamageBonus = 0x10,
    WindSpellDamageBonus = 0x20,
    FireSpellDamageBonus = 0x40
}
