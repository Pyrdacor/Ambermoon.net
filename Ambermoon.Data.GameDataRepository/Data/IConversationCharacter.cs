namespace Ambermoon.Data.GameDataRepository.Data
{
    internal interface IConversationCharacter
    {
        public uint Index { get; }
        public string Name { get; }
        public CharacterType Type { get; }
        public Gender Gender { get; set; }
        public Class Class { get; set; }
        public Race Race { get; set; }
        public uint Level { get; set; }
        public uint Age { get; set; }
        public uint MaxAge { get; set; }
        public Language SpokenLanguages { get; set; }
        public ExtendedLanguage AdditionalSpokenLanguages { get; set; }
        public uint GraphicIndex { get; set; }
        public uint LookAtCharTextIndex { get; set; }

        // TODO: events
    }
}
