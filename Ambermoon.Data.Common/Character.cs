namespace Ambermoon.Data
{
    public abstract class Character
    {
        public CharacterType Type { get; }
        public Gender Gender { get; set; }
        public Race Race { get; set; }
        public Class Class { get; set; }
        public SpellTypeMastery SpellMastery { get; set; }
        public byte Level { get; set; }
        public Language SpokenLanguages { get; set; }
        public byte AttacksPerRound { get; set; }
        public ushort SpellLearningPoints { get; set; }
        public ushort TrainingPoints { get; set; }
        public ushort Gold { get; set; }
        public ushort Food { get; set; }
        public Ailment Ailments { get; set; }
        public CharacterValueCollection<Attribute> Attributes { get; } = new CharacterValueCollection<Attribute>(10); // 8 attribute + age + a hidden attribute
        public CharacterValueCollection<Ability> Abilities { get; } = new CharacterValueCollection<Ability>(10);
        public CharacterValue HitPoints { get; } = new CharacterValue();
        public CharacterValue SpellPoints { get; } = new CharacterValue();
        public ushort Attack { get; set; }
        public ushort Defense { get; set; }
        public ushort MagicAttack { get; set; }
        public ushort MagicDefense { get; set; }
        public ushort AttacksPerRoundPerLevel { get; set; }
        public ushort HitPointsPerLevel { get; set; }
        public ushort SpellPointsPerLevel { get; set; }
        public ushort SpellLearningPointsPerLevel { get; set; }
        public ushort TrainingPointsPerLevel { get; set; }
        public uint ExperiencePoints { get; set; }
        public uint LearnedHealingSpells { get; set; }
        public uint LearnedAlchemisticSpells { get; set; }
        public uint LearnedMysticSpells { get; set; }
        public uint LearnedDestructionSpells { get; set; }
        public uint TotalWeight { get; set; }
        public ushort PortraitIndex { get; set; }
        public string Name { get; set; }
        public bool Alive =>
            !Ailments.HasFlag(Ailment.DeadCorpse) &&
            !Ailments.HasFlag(Ailment.DeadAshes) &&
            !Ailments.HasFlag(Ailment.DeadDust);

        public Inventory Inventory { get; } = new Inventory();
        public Equipment Equipment { get; } = new Equipment();

        protected Character(CharacterType type)
        {
            Type = type;
        }
    }
}
