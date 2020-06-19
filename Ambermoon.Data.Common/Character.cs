namespace Ambermoon.Data
{
    public abstract class Character
    {
        public CharacterType Type { get; }

        protected Character(CharacterType type)
        {
            Type = type;
        }
    }
}
