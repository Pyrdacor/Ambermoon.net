namespace Ambermoon.Data
{
    public abstract class Character
    {
        public CharacterType Type { get; }
        public Gender Gender { get; set; }
        public Race Race { get; set; }
        public Class Class { get; set; }
        public byte Level { get; set; }
        public CharacterValueCollection<Attribute> Attributes { get; } = new CharacterValueCollection<Attribute>(10); // 8 attribute + age + a hidden attribute
        public CharacterValueCollection<Ability> Abilities { get; } = new CharacterValueCollection<Ability>(10);
        public ushort PortraitIndex { get; set; }
        public string Name { get; set; }

        protected Character(CharacterType type)
        {
            Type = type;
        }
    }
}
