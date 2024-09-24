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
        Escape,
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
        GhostInferno, // Advanced only
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
		ShowElements, // Advanced only
		RecognizeWeakPoint, // Advanced only
		SeeWeaknesses, // Advanced only
		KnowledgeOfTheWeakness, // Advanced only
		ForeseeMagic, // Advanced only
		ForeseeAttack, // Advanced only
		MysticDecay, // Advanced only
		ProtectionSphere, // Advanced only
		UnusedMystic26,
        UnusedMystic27,
        UnusedMystic28,
        UnusedMystic29,
        MysticImitation, // Advanced only
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
        Drugs = 201, // stinking mushroom
        SelfHealing = 202, // Advanced only
        SelfReviving = 203, // Advanced only
        ExpExchange = 204, // Advanced only
        MountWasp = 205 // Advanced only
    }

    public enum HealingSpell : byte
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

    public enum AlchemisticSpell : byte
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
        GhostInferno // Advanced only
    }

    public enum MysticSpell : byte
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
		ShowElements, // Advanced only
		RecognizeWeakPoint, // Advanced only
		SeeWeaknesses, // Advanced only
		KnowledgeOfTheWeakness, // Advanced only
		ForeseeMagic, // Advanced only
		ForeseeAttack, // Advanced only
		MysticDecay, // Advanced only
		ProtectionSphere, // Advanced only
		UnusedMystic26,
		UnusedMystic27,
		UnusedMystic28,
		UnusedMystic29,
		MysticImitation, // Advanced only
	}

    public enum DestructionSpell : byte
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
                    spell == Spell.GhostInferno ||
                    spell == Spell.LPStealer ||
                    spell == Spell.SPStealer ||
					spell == Spell.MysticDecay ||
					spell == Spell.MagicalProjectile ||
                    spell == Spell.MagicalArrows ||
                    (spell >= Spell.Mudsling && spell <= Spell.Iceshower);
        }

        public static bool DealsDamage(this Spell spell)
        {
            // No dissolve spells.
            return spell == Spell.GhostWeapon ||
                    spell == Spell.GhostInferno ||
                   spell == Spell.LPStealer ||
                   spell == Spell.SPStealer ||
				   spell == Spell.MysticDecay ||
				   spell == Spell.MagicalProjectile ||
                   spell == Spell.MagicalArrows ||
                   (spell >= Spell.Mudsling && spell <= Spell.Iceshower);
        }

        public static bool IsCastableByMonster(this Spell spell)
        {
            return spell == Spell.LPStealer ||
                   spell == Spell.SPStealer ||
                   spell == Spell.GhostWeapon ||
                   SpellInfos.Entries[spell].SpellSchool == SpellSchool.Destruction;
        }
    }
}
