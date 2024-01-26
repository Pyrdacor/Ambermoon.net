using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    /// <summary>
    /// After the <see cref="RaceNames"/> there are the
    /// skill names like "Attack", "Parry", etc.
    /// </summary>
    public class SkillNames
    {
        readonly Dictionary<Skill, string> entries = new Dictionary<Skill, string>();
        readonly Dictionary<Skill, string> shortNames = new Dictionary<Skill, string>();
        public IReadOnlyDictionary<Skill, string> Entries => entries;
        public IReadOnlyDictionary<Skill, string> ShortNames => shortNames;

        internal SkillNames(List<string> names, List<string> shortNames)
        {
            if (names.Count != 10 || shortNames.Count != 10)
                throw new AmbermoonException(ExceptionScope.Data, "Invalid number of skill names.");

            for (int i = 0; i < names.Count; ++i)
            {
                entries.Add((Skill)i, names[i]);
                this.shortNames.Add((Skill)i, shortNames[i]);
            }
        }

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the skill names just behind the
        /// race names.
        /// 
        /// It will be behind the skill names after this.
        /// </summary>
        internal SkillNames(IDataReader dataReader)
        {
            foreach (var type in EnumHelper.GetValues<Skill>())
            {
                entries.Add(type, dataReader.ReadNullTerminatedString(AmigaExecutable.Encoding));
            }
        }

        internal void AddShortNames(IDataReader dataReader)
        {
            foreach (var type in EnumHelper.GetValues<Skill>())
            {
                shortNames.Add(type, dataReader.ReadNullTerminatedString(AmigaExecutable.Encoding));
            }
        }
    }
}
