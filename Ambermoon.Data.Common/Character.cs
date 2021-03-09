using System;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data
{
    [Serializable]
    public abstract class Character
    {
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
        public ushort PortraitIndex { get; set; }
        public byte[] UnknownBytes13 { get; set; } // Unknown 3 bytes
        public SpellTypeImmunity SpellTypeImmunity { get; set; }
        public byte AttacksPerRound { get; set; }
        public CharacterElement Element { get; set; }
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
        public Ailment Ailments { get; set; }
        public ushort UnknownWord34 { get; set; }
        public CharacterValueCollection<Attribute> Attributes { get; } = new CharacterValueCollection<Attribute>(10); // 8 attribute + age + a hidden attribute
        public CharacterValueCollection<Ability> Abilities { get; } = new CharacterValueCollection<Ability>(10);
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
            !Ailments.HasFlag(Ailment.DeadCorpse) &&
            !Ailments.HasFlag(Ailment.DeadAshes) &&
            !Ailments.HasFlag(Ailment.DeadDust);
        /// <summary>
        /// Checks if the character is immune to the given
        /// spell.
        /// </summary>
        /// <param name="spell">The spell to check</param>
        /// <param name="silent">If true no "is immune to" message should be shown on cast. This is used for holy spells for example.</param>
        /// <returns></returns>
        public bool IsImmuneToSpell(Spell spell, out bool silent)
        {
            silent = false;

            // Only monsters can have spell immunities
            if (!(this is Monster monster))
                return false;

            // Note: This only checks for immunities based on monster flags and elements.
            // Other things like condition-dependent immunities or spell type immunities
            // are not checked here.
            bool boss = monster.MonsterFlags.HasFlag(MonsterFlags.Boss);
            bool undead = monster.MonsterFlags.HasFlag(MonsterFlags.Undead);
            bool demon = monster.MonsterFlags.HasFlag(MonsterFlags.Demon);
            bool animal = monster.MonsterFlags.HasFlag(MonsterFlags.Animal);

            silent = !undead &&
                     (spell == Spell.DispellUndead ||
                     spell == Spell.DestroyUndead ||
                     spell == Spell.HolyWord);

            return spell switch
            {
                Spell.DispellUndead => !undead || boss,
                Spell.DestroyUndead => !undead || boss,
                Spell.HolyWord => !undead || boss,
                Spell.GhostWeapon => Element == CharacterElement.Ghost,
                Spell.LPStealer => Element == CharacterElement.Undead || Element == CharacterElement.Earth,
                Spell.SPStealer => false,
                Spell.MonsterKnowledge => Element == CharacterElement.Psychic,
                Spell.MagicalProjectile => Element == CharacterElement.Ghost,
                Spell.MagicalArrows => Element == CharacterElement.Ghost,
                Spell.Lame => boss || Element == CharacterElement.Earth,
                Spell.Poison => Element == CharacterElement.Earth,
                Spell.Petrify => boss,
                Spell.CauseDisease => Element == CharacterElement.Earth,
                Spell.CauseAging => false,
                Spell.Irritate => boss || Element == CharacterElement.Psychic,
                Spell.CauseMadness => boss || Element == CharacterElement.Psychic,
                Spell.Sleep => Element == CharacterElement.Psychic,
                Spell.Fear => boss || Element == CharacterElement.Psychic,
                Spell.Blind => Element == CharacterElement.Ghost,
                Spell.Drug => boss,
                Spell.DissolveVictim => boss || Element == CharacterElement.Ghost,
                Spell.Mudsling => false,
                Spell.Rockfall => false,
                Spell.Earthslide => false,
                Spell.Earthquake => false,
                Spell.Winddevil => false,
                Spell.Windhowler => false,
                Spell.Thunderbolt => false,
                Spell.Whirlwind => false,
                Spell.Firebeam => false,
                Spell.Fireball => false,
                Spell.Firestorm => false,
                Spell.Firepillar => false,
                Spell.Waterfall => false,
                Spell.Iceball => false,
                Spell.Icestorm => false,
                Spell.Iceshower => false,
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

        public void Die(Ailment deadAilment = Ailment.DeadCorpse)
        {
            Ailments |= deadAilment;
            Died?.Invoke(this);
        }

        public void Damage(uint damage, Ailment deadAilment = Ailment.DeadCorpse)
        {
            HitPoints.CurrentValue = HitPoints.CurrentValue <= damage ? 0 : HitPoints.CurrentValue - damage;

            if (HitPoints.CurrentValue == 0)
                Die(deadAilment);
        }

        public void Heal(uint amount)
        {
            HitPoints.CurrentValue = Math.Min(HitPoints.TotalMaxValue, HitPoints.CurrentValue + amount);
        }

        public virtual bool CanMove(bool battle = true)
        {
            return Ailments.CanMove();
        }

        public virtual bool CanFlee()
        {
            return Ailments.CanFlee();
        }

        public Inventory Inventory { get; } = new Inventory();
        public Equipment Equipment { get; } = new Equipment();

        public static readonly List<Ailment> PossibleAilments = Enum.GetValues<Ailment>()
            .Where(a => a != Ailment.None && a != Ailment.Unused).ToList();
        public static readonly List<Ailment> PossibleVisibleAilments = PossibleAilments
            .Where(a => a != Ailment.DeadAshes && a != Ailment.DeadDust).ToList();
        public List<Ailment> VisibleAilments
        {
            get
            {
                if (!Alive) // When dead, only show the dead condition.
                    return new List<Ailment> { Ailment.DeadCorpse };
                else
                    return PossibleVisibleAilments.Where(a => Ailments.HasFlag(a)).ToList();
            }
        }

        protected Character(CharacterType type)
        {
            Type = type;
        }
    }
}
