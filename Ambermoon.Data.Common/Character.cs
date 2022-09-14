using System;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data
{
    [Serializable]
    public abstract class Character
    {
        public const uint GoldWeight = 5;
        public static uint FoodWeight { get; set; } = 250;

        public uint Index { get; set; }
        public CharacterType Type { get; }
        public Gender Gender { get; set; }
        public Race Race { get; set; }
        public Class Class { get; set; }
        public SpellTypeMastery SpellMastery { get; set; }
        public byte Level { get; set; }
        public byte NumberOfFreeHands { get; set; }
        public byte NumberOfFreeFingers { get; set; }
        public Language SpokenLanguages { get; set; }
        public bool InventoryInaccessible { get; set; }
        public byte PortraitIndex { get; set; }
        public byte[] UnknownBytes13 { get; set; } // Unknown 2 bytes
        public SpellTypeImmunity SpellTypeImmunity { get; set; }
        public byte AttacksPerRound { get; set; }
        public CharacterElement Element { get; set; }
        public BattleFlags BattleFlags { get; set; }
        public ushort SpellLearningPoints { get; set; }
        public ushort TrainingPoints { get; set; }
        public ushort Gold { get; set; }
        public ushort Food { get; set; }
        /// <summary>
        /// Is used for party members to identify their associated map character.
        /// 0xffff means "use the map character you talked to".
        /// But party members like Selena, Sabine or Valdyn will move to another
        /// location after you met them. This character bit represents the new
        /// location.
        /// </summary>
        public ushort CharacterBitIndex { get; set; }
        public Condition Conditions { get; set; }
        public ushort UnknownWord34 { get; set; }
        public CharacterValueCollection<Attribute> Attributes { get; } = new CharacterValueCollection<Attribute>(10); // 8 attribute + age + a hidden attribute
        public CharacterValueCollection<Skill> Skills { get; } = new CharacterValueCollection<Skill>(10);
        public CharacterValue HitPoints { get; } = new CharacterValue();
        public CharacterValue SpellPoints { get; } = new CharacterValue();
        public short BaseAttack { get; set; }
        public short BaseDefense { get; set; }
        public short VariableAttack { get; set; }
        public short VariableDefense { get; set; }
        public short MagicAttack { get; set; }
        public short MagicDefense { get; set; }
        public ushort AttacksPerRoundIncreaseLevels { get; set; }
        public ushort HitPointsPerLevel { get; set; }
        public ushort SpellPointsPerLevel { get; set; }
        public ushort SpellLearningPointsPerLevel { get; set; }
        public ushort TrainingPointsPerLevel { get; set; }
        public ushort UnknownWord236 { get; set; }
        public uint ExperiencePoints { get; set; }
        public uint LearnedHealingSpells { get; set; }
        public uint LearnedAlchemisticSpells { get; set; }
        public uint LearnedMysticSpells { get; set; }
        public uint LearnedDestructionSpells { get; set; }
        public uint LearnedSpellsType5 { get; set; }
        public uint LearnedSpellsType6 { get; set; }
        public uint LearnedSpellsType7 { get; set; }
        public uint TotalWeight { get; set; }
        public string Name { get; set; }
        public bool Alive =>
            !Conditions.HasFlag(Condition.DeadCorpse) &&
            !Conditions.HasFlag(Condition.DeadAshes) &&
            !Conditions.HasFlag(Condition.DeadDust);
        /// <summary>
        /// Checks if the character is immune to the given
        /// spell.
        /// </summary>
        /// <param name="spell">The spell to check</param>
        /// <param name="silent">If true no "is immune to" message should be shown on cast. This is used for holy spells for example.</param>
        /// <returns></returns>
        public bool IsImmuneToSpell(Spell spell, out bool silent, bool supportElements)
        {
            silent = false;

            // Only monsters can have spell immunities
            if (!(this is Monster monster))
                return false;

            // Note: This only checks for immunities based on monster flags and elements.
            // Other things like condition-dependent immunities or spell type immunities
            // are not checked here.
            bool boss = monster.BattleFlags.HasFlag(BattleFlags.Boss);
            bool undead = monster.BattleFlags.HasFlag(BattleFlags.Undead);
            bool demon = monster.BattleFlags.HasFlag(BattleFlags.Demon);
            bool animal = monster.BattleFlags.HasFlag(BattleFlags.Animal);

            silent = !undead &&
                     (spell == Spell.DispellUndead ||
                     spell == Spell.DestroyUndead ||
                     spell == Spell.HolyWord);

            return spell switch
            {
                Spell.DispellUndead => !undead || boss,
                Spell.DestroyUndead => !undead || boss,
                Spell.HolyWord => !undead || boss,
                Spell.GhostWeapon => Element == CharacterElement.Spirit,
                Spell.LPStealer => Element == CharacterElement.Undead,
                Spell.SPStealer => Element == CharacterElement.Spirit,
                Spell.MonsterKnowledge => Element == CharacterElement.Mental,
                Spell.MagicalProjectile => Element == CharacterElement.Spirit,
                Spell.MagicalArrows => Element == CharacterElement.Spirit,
                Spell.Lame => boss || Element == CharacterElement.Physical,
                Spell.Poison => Element == CharacterElement.Physical,
                Spell.Petrify => boss || Element == CharacterElement.Undead,
                Spell.CauseDisease => Element == CharacterElement.Physical,
                Spell.CauseAging => Element == CharacterElement.Undead,
                Spell.Irritate => boss || Element == CharacterElement.Mental,
                Spell.CauseMadness => boss || Element == CharacterElement.Mental,
                Spell.Sleep => Element == CharacterElement.Mental,
                Spell.Fear => boss || Element == CharacterElement.Mental,
                Spell.Blind => Element == CharacterElement.Spirit,
                Spell.Drug => boss || Element == CharacterElement.Physical,
                Spell.DissolveVictim => boss || Element == CharacterElement.Spirit,
                Spell.Mudsling => supportElements && Element == CharacterElement.Earth,
                Spell.Rockfall => supportElements && Element == CharacterElement.Earth,
                Spell.Earthslide => supportElements && Element == CharacterElement.Earth,
                Spell.Earthquake => supportElements && Element == CharacterElement.Earth,
                Spell.Winddevil => supportElements && Element == CharacterElement.Wind,
                Spell.Windhowler => supportElements && Element == CharacterElement.Wind,
                Spell.Thunderbolt => supportElements && Element == CharacterElement.Wind,
                Spell.Whirlwind => supportElements && Element == CharacterElement.Wind,
                Spell.Firebeam => supportElements && Element == CharacterElement.Fire,
                Spell.Fireball => supportElements && Element == CharacterElement.Fire,
                Spell.Firestorm => supportElements && Element == CharacterElement.Fire,
                Spell.Firepillar => supportElements && Element == CharacterElement.Fire,
                Spell.Waterfall => supportElements && Element == CharacterElement.Water,
                Spell.Iceball => supportElements && Element == CharacterElement.Water,
                Spell.Icestorm => supportElements && Element == CharacterElement.Water,
                Spell.Iceshower => supportElements && Element == CharacterElement.Water,
                _ => false
            };
        }
        public bool HasAnySpell() =>
            LearnedHealingSpells != 0 ||
            LearnedAlchemisticSpells != 0 ||
            LearnedMysticSpells != 0 ||
            LearnedDestructionSpells != 0;

        public bool HasSpell(Spell spell)
        {
            int school = ((int)spell - 1) / 30;

            if (school > 3) // Only spells of the 4 main schools can be learned
                return false;

            int spellIndex = (int)spell - school * 30;
            uint spellBit = 1u << spellIndex;

            switch (school)
            {
                case 0: // healing
                    return (LearnedHealingSpells & spellBit) != 0;
                case 1: // alchemistic
                    return (LearnedAlchemisticSpells & spellBit) != 0;
                case 2: // mystic
                    return (LearnedMysticSpells & spellBit) != 0;
                case 3: // destruction
                    return (LearnedDestructionSpells & spellBit) != 0;
                default:
                    return false;
            }
        }

        public void AddSpell(Spell spell)
        {
            int school = ((int)spell - 1) / 30;

            if (school > 3) // Only spells of the 4 main schools can be learned
                return;

            int spellIndex = (int)spell - school * 30;

            switch (school)
            {
                case 0: // healing
                    LearnedHealingSpells |= 1u << spellIndex;
                    break;
                case 1: // alchemistic
                    LearnedAlchemisticSpells|= 1u << spellIndex;
                    break;
                case 2: // mystic
                    LearnedMysticSpells |= 1u << spellIndex;
                    break;
                case 3: // destruction
                    LearnedDestructionSpells |= 1u << spellIndex;
                    break;
            }
        }

        public List<Spell> LearnedSpells
        {
            get
            {
                var learnedSpells = new List<Spell>();
                if (LearnedHealingSpells != 0)
                {
                    for (int i = 1; i < 31; ++i)
                    {
                        if ((LearnedHealingSpells & (1 << i)) != 0)
                            learnedSpells.Add((Spell)i);
                    }
                }
                if (LearnedAlchemisticSpells != 0)
                {
                    for (int i = 1; i < 31; ++i)
                    {
                        if ((LearnedAlchemisticSpells & (1 << i)) != 0)
                            learnedSpells.Add((Spell)(i + 30));
                    }
                }
                if (LearnedMysticSpells != 0)
                {
                    for (int i = 1; i < 31; ++i)
                    {
                        if ((LearnedMysticSpells & (1 << i)) != 0)
                            learnedSpells.Add((Spell)(i + 60));
                    }
                }
                if (LearnedDestructionSpells != 0)
                {
                    for (int i = 1; i < 31; ++i)
                    {
                        if ((LearnedDestructionSpells & (1 << i)) != 0)
                            learnedSpells.Add((Spell)(i + 90));
                    }
                }
                return learnedSpells;
            }
        }

        public Action<Character> Died;

        public void Die(Condition deathCondition = Condition.DeadCorpse)
        {
            Conditions = deathCondition;
            Died?.Invoke(this);
        }

        public void Damage(uint damage, Condition deathCondition = Condition.DeadCorpse)
            => Damage(damage, deathCondition => Die(deathCondition), deathCondition);

        public void Damage(uint damage, Action<Condition> deathAction, Condition deathCondition = Condition.DeadCorpse)
        {
            HitPoints.CurrentValue = HitPoints.CurrentValue <= damage ? 0 : HitPoints.CurrentValue - damage;

            if (HitPoints.CurrentValue == 0)
                deathAction?.Invoke(deathCondition);
        }

        public void Heal(uint amount)
        {
            HitPoints.CurrentValue = Math.Min(HitPoints.TotalMaxValue, HitPoints.CurrentValue + amount);
        }

        public virtual bool CanMove(bool battle = true)
        {
            return Conditions.CanMove();
        }

        public virtual bool CanFlee()
        {
            return Conditions.CanFlee();
        }

        public Inventory Inventory { get; } = new Inventory();
        public Equipment Equipment { get; } = new Equipment();

        public static readonly List<Condition> PossibleConditions = Enum.GetValues<Condition>()
            .Where(a => a != Condition.None && a != Condition.Unused).ToList();
        public static readonly List<Condition> PossibleVisibleConditions = PossibleConditions
            .Where(a => a != Condition.DeadAshes && a != Condition.DeadDust).ToList();
        public List<Condition> VisibleConditions
        {
            get
            {
                if (!Alive) // When dead, only show the dead condition.
                    return new List<Condition> { Condition.DeadCorpse };
                else
                    return PossibleVisibleConditions.Where(a => Conditions.HasFlag(a)).ToList();
            }
        }

        protected Character(CharacterType type)
        {
            Type = type;
        }
    }
}
