using Ambermoon.Data.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data
{
    public class TextContainer
    {
        public static TextContainer Load(ITextContainerReader reader, IDataReader dataReader, bool processUIPlaceholders)
        {
            var textContainer = new TextContainer();
            reader.ReadTextContainer(textContainer, dataReader, processUIPlaceholders);
            return textContainer;
        }

        public List<string> WorldNames { get; } =  [];
        public List<string> FormatMessages { get; } =  [];
        public List<string> Messages { get; } =  [];
        public List<string> AutomapTypeNames { get; } =  [];
        public List<string> OptionNames { get; } =  [];
        public List<string> MusicNames { get; } =  [];
        public List<string> SpellClassNames { get; } =  [];
        public List<string> SpellNames { get; } =  [];
        public List<string> LanguageNames { get; } =  [];
        public List<string> ClassNames { get; } =  [];
        public List<string> RaceNames { get; } =  [];
        public List<string> SkillNames { get; } =  [];
        public List<string> AttributeNames { get; } =  [];
        public List<string> SkillShortNames { get; } =  [];
        public List<string> AttributeShortNames { get; } =  [];
        public List<string> ItemTypeNames { get; } =  [];
        public List<string> ConditionNames { get; } =  [];
        public List<string> UITexts { get; } =  [];
        public List<int> UITextWithPlaceholderIndices { get; } =  [];
        public string VersionString { get; set; }
        public string DateAndLanguageString { get; set; }
    }
}
