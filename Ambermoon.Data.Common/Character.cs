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
        public ushort UnknownWord28 { get; set; }
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
        public ushort AttacksPerRoundPerLevel { get; set; }
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

            /*return spell switch
            {
                Spell.DispellUndead => ,
                Spell.DestroyUndead => ,
                Spell.HolyWord => ,
                Spell.GhostWeapon => ,
                Spell.LPStealer => ,
                Spell.SPStealer => ,
                Spell.MonsterKnowledge => ,
                Spell.ShowMonsterLP => ,
                Spell.MagicalProjectile => ,
                Spell.MagicalArrows => ,
                Spell.Lame => ,
                Spell.Poison => ,
                Spell.Petrify => ,
                Spell.CauseDisease => ,
                Spell.CauseAging => ,
                Spell.Irritate => ,
                Spell.CauseMadness => ,
                Spell.Sleep => ,
                Spell.Fear => ,
                Spell.Blind => ,
                Spell.Drug => ,
                Spell.DissolveVictim => ,
                Spell.Mudsling => ,
                Spell.Rockfall => ,
                Spell.Earthslide => ,
                Spell.Earthquake => ,
                Spell.Winddevil => ,
                Spell.Windhowler => ,
                Spell.Thunderbolt => ,
                Spell.Whirlwind => ,
                Spell.Firebeam => ,
                Spell.Fireball => ,
                Spell.Firestorm => ,
                Spell.Firepillar => ,
                Spell.Waterfall => ,
                Spell.Iceball => ,
                Spell.Icestorm => ,
                Spell.Iceshower => ,
                _ => false
            };*/
            // TODO: boss, element, etc
            return false;
        }
        public bool HasAnySpell() =>
            LearnedHealingSpells != 0 ||
            LearnedAlchemisticSpells != 0 ||
            LearnedMysticSpells != 0 ||
            LearnedDestructionSpells != 0;
        public List<Spell> LearnedSpells
        {
            get
            {
                var learnedSpells = new List<Spell>();
                if (LearnedHealingSpells != 0)
                {
                    for (int i = 0; i < 30; ++i)
                    {
                        if ((LearnedHealingSpells & (1 << i)) != 0)
                            learnedSpells.Add((Spell)(i + 1));
                    }
                }
                if (LearnedAlchemisticSpells != 0)
                {
                    for (int i = 0; i < 30; ++i)
                    {
                        if ((LearnedAlchemisticSpells & (1 << i)) != 0)
                            learnedSpells.Add((Spell)(i + 31));
                    }
                }
                if (LearnedMysticSpells != 0)
                {
                    for (int i = 0; i < 30; ++i)
                    {
                        if ((LearnedMysticSpells & (1 << i)) != 0)
                            learnedSpells.Add((Spell)(i + 61));
                    }
                }
                if (LearnedDestructionSpells != 0)
                {
                    for (int i = 0; i < 30; ++i)
                    {
                        if ((LearnedDestructionSpells & (1 << i)) != 0)
                            learnedSpells.Add((Spell)(i + 91));
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
