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

        public List<string> WorldNames { get; } = new List<string>();
        public List<string> FormatMessages { get; } = new List<string>();
        public List<string> Messages { get; } = new List<string>();
        public List<string> AutomapTypeNames { get; } = new List<string>();
        public List<string> OptionNames { get; } = new List<string>();
        public List<string> MusicNames { get; } = new List<string>();
        public List<string> SpellClassNames { get; } = new List<string>();
        public List<string> SpellNames { get; } = new List<string>();
        public List<string> LanguageNames { get; } = new List<string>();
        public List<string> ClassNames { get; } = new List<string>();
        public List<string> RaceNames { get; } = new List<string>();
        public List<string> SkillNames { get; } = new List<string>();
        public List<string> AttributeNames { get; } = new List<string>();
        public List<string> SkillShortNames { get; } = new List<string>();
        public List<string> AttributeShortNames { get; } = new List<string>();
        public List<string> ItemTypeNames { get; } = new List<string>();
        public List<string> ConditionNames { get; } = new List<string>();
        public List<string> UITexts { get; } = new List<string>();
        public List<int> UITextWithPlaceholderIndices { get; } = new List<int>();
        public string VersionString { get; set; }
        public string DateAndLanguageString { get; set; }
    }
}
