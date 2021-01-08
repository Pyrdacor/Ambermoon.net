namespace Ambermoon.Data
{
    public enum Spell
    {
        None,
        HealingHand,
        RemoveFear,
        RemovePanic,
        RemoveShadows,
        RemoveBlindness,
        RemovePain,
        RemoveDisease,
        SmallHealing,
        RemovePoison,
        NeutralizePoison,
        MediumHealing,
        DispellUndead,
        DestroyUndead,
        HolyWord,
        WakeTheDead,
        ChangeAshes,
        ChangeDust,
        GreatHealing,
        MassHealing,
        Resurrection,
        RemoveRigidness,
        RemoveLamedness,
        HealAging,
        StopAging,
        StoneToFlesh,
        WakeUp,
        RemoveIrritation,
        RemoveDrugged,
        RemoveMadness,
        RestoreStamina,
        ChargeItem,
        Light,
        MagicalTorch,
        MagicalLantern,
        MagicalSun,
        GhostWeapon,
        CreateFood,
        RemoveCurses,
        Blink,
        Jump,
        Flight,
        WordOfMarking,
        WordOfReturning,
        MagicalShield,
        MagicalWall,
        MagicalBarrier,
        MagicalWeapon,
        MagicalAssault,
        MagicalAttack,
        Levitation,
        AntiMagicWall,
        AntiMagicSphere,
        AlchemisticGlobe,
        Hurry,
        MassHurry,
        RepairItem,
        DuplicateItem,
        LPStealer,
        SPStealer,
        UnusedAlchemistic30,
        MonsterKnowledge,
        Identification,
        Knowledge,
        Clairvoyance,
        SeeTheTruth,
        MapView,
        MagicalCompass,
        FindTraps,
        FindMonsters,
        FindPersons,
        FindSecretDoors,
        MysticalMapping,
        MysticalMapI,
        MysticalMapII,
        MysticalMapIII,
        MysticalGlobe,
        ShowMonsterLP,
        UnusedMystic18,
        UnusedMystic19,
        UnusedMystic20,
        UnusedMystic21,
        UnusedMystic22,
        UnusedMystic23,
        UnusedMystic24,
        UnusedMystic25,
        UnusedMystic26,
        UnusedMystic27,
        UnusedMystic28,
        UnusedMystic29,
        UnusedMystic30,
        MagicalProjectile,
        MagicalArrows,
        Lame,
        Poison,
        Petrify,
        CauseDisease,
        CauseAging,
        Irritate,
        CauseMadness,
        Sleep,
        Fear,
        Blind,
        Drug,
        DissolveVictim,
        Mudsling,
        Rockfall,
        Earthslide,
        Earthquake,
        Winddevil,
        Windhowler,
        Thunderbolt,
        Whirlwind,
        Firebeam,
        Fireball,
        Firestorm,
        Firepillar,
        Waterfall,
        Iceball,
        Icestorm,
        Iceshower,

        // Special spells
        Lockpicking = 181, // 6 * 30 + 1
        CallEagle = 182,
        DecreaseAge = 183, // youth potion / youth
        PlayElfHarp = 184, // magic music
        SpellPointsI = 185,
        SpellPointsII = 186,
        SpellPointsIII = 187,
        SpellPointsIV = 188,
        SpellPointsV = 189,
        AllHealing = 190, // all healing potion
        MagicalMap = 191,
        AddStrength = 192,
        AddIntelligence = 193,
        AddDexterity = 194,
        AddSpeed = 195,
        AddStamina = 196,
        AddCharisma = 197,
        AddLuck = 198,
        AddAntiMagic = 199,
        Rope = 200, // levitation on a rope / climb
        Drugs = 201 // stinking mushroom
    }

    public enum HealingSpell
    {
        None,
        HealingHand,
        RemoveFear,
        RemovePanic,
        RemoveShadows,
        RemoveBlindness,
        RemovePain,
        RemoveDisease,
        SmallHealing,
        RemovePoison,
        NeutralizePoison,
        MediumHealing,
        DispellUndead,
        DestroyUndead,
        HolyWord,
        WakeTheDead,
        ChangeAshes,
        ChangeDust,
        GreatHealing,
        MassHealing,
        Resurrection,
        RemoveRigidness,
        RemoveLamedness,
        HealAging,
        StopAging,
        StoneToFlesh,
        WakeUp,
        RemoveIrritation,
        RemoveDrugged,
        RemoveMadness,
        RestoreStamina
    }

    public enum AlchemisticSpell
    {
        None,
        ChargeItem,
        Light,
        MagicalTorch,
        MagicalLantern,
        MagicalSun,
        GhostWeapon,
        CreateFood,
        RemoveCurses,
        Blink,
        Jump,
        Flight,
        WordOfMarking,
        WordOfReturning,
        MagicalShield,
        MagicalWall,
        MagicalBarrier,
        MagicalWeapon,
        MagicalAssault,
        MagicalAttack,
        Levitation,
        AntiMagicWall,
        AntiMagicSphere,
        AlchemisticGlobe,
        Hurry,
        MassHurry,
        RepairItem,
        DuplicateItem,
        LPStealer,
        SPStealer,
        Unused30
    }

    public enum MysticSpell
    {
        None,
        MonsterKnowledge,
        Identification,
        Knowledge,
        Clairvoyance,
        SeeTheTruth,
        MapView,
        MagicalCompass,
        FindTraps,
        FindMonsters,
        FindPersons,
        FindSecretDoors,
        MysticalMapping,
        MysticalMapI,
        MysticalMapII,
        MysticalMapIII,
        MysticalGlobe,
        ShowMonsterLP,
        Unused18,
        Unused19,
        Unused20,
        Unused21,
        Unused22,
        Unused23,
        Unused24,
        Unused25,
        Unused26,
        Unused27,
        Unused28,
        Unused29,
        Unused30
    }

    public enum DestructionSpell
    {
        None,
        MagicalProjectile,
        MagicalArrows,
        Lame,
        Poison,
        Petrify,
        CauseDisease,
        CauseAging,
        Irritate,
        CauseMadness,
        Sleep,
        Fear,
        Blind,
        Drug,
        DissolveVictim,
        Mudsling,
        Rockfall,
        Earthslide,
        Earthquake,
        Winddevil,
        Windhowler,
        Thunderbolt,
        Whirlwind,
        Firebeam,
        Fireball,
        Firestorm,
        Firepillar,
        Waterfall,
        Iceball,
        Icestorm,
        Iceshower
    }

    public static class SpellExtensions
    {
        public static bool FailsAgainstPetrifiedEnemy(this Spell spell)
        {
            // Most damage dealing spells except for
            // dissolving spells fail against petrified enemies.
            return  spell == Spell.GhostWeapon ||
                    spell == Spell.LPStealer ||
                    spell == Spell.SPStealer ||
                    spell == Spell.MagicalProjectile ||
                    spell == Spell.MagicalArrows ||
                    (spell >= Spell.Mudsling && spell <= Spell.Iceshower);
        }

        public static bool DealsDamage(this Spell spell)
        {
            // No dissolve spells.
            return spell == Spell.GhostWeapon ||
                   spell == Spell.LPStealer ||
                   spell == Spell.SPStealer ||
                   spell == Spell.MagicalProjectile ||
                   spell == Spell.MagicalArrows ||
                   (spell >= Spell.Mudsling && spell <= Spell.Iceshower);
        }

        public static bool IsCastableByMonster(this Spell spell)
        {
            var spellInfo = SpellInfos.Entries[spell];

            if (spellInfo.SpellSchool == SpellSchool.Mystic ||
                (spellInfo.SpellSchool == SpellSchool.Alchemistic && spell != Spell.GhostWeapon) ||
                spellInfo.SpellSchool > SpellSchool.Destruction)
                return false;

            return
                spell != Spell.DispellUndead &&
                spell != Spell.DestroyUndead &&
                spell != Spell.HolyWord &&
                spell != Spell.WakeTheDead &&
                spell != Spell.ChangeAshes &&
                spell != Spell.ChangeDust &&
                spell != Spell.Resurrection &&
                spell != Spell.RestoreStamina &&
                spell != Spell.DissolveVictim;
        }
    }
}
