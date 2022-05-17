using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    /// <summary>
    /// After the <see cref="SkillNames"/> there are the
    /// attribute names like "Strength", "Dexterity", etc.
    /// </summary>
    public class AttributeNames
    {
        readonly Dictionary<Attribute, string> entries = new Dictionary<Attribute, string>();
        readonly Dictionary<Attribute, string> shortNames = new Dictionary<Attribute, string>();
        public IReadOnlyDictionary<Attribute, string> Entries => entries;
        public IReadOnlyDictionary<Attribute, string> ShortNames => shortNames;

        internal AttributeNames(List<string> names, List<string> shortNames)
        {
            if (names.Count != 9 || shortNames.Count != 8)
                throw new AmbermoonException(ExceptionScope.Data, "Invalid number of attribute names.");

            for (int i = 0; i < names.Count; ++i)
            {
                entries.Add((Attribute)i, names[i]);
                if (i != 8)
                    this.shortNames.Add((Attribute)i, shortNames[i]);
            }

            entries.Add(Attribute.Unknown, "");
            this.shortNames.Add(Attribute.Age, "");
            this.shortNames.Add(Attribute.Unknown, "");
        }

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the attribute names just behind the
        /// ability names.
        /// 
        /// It will be behind the attribute names after this.
        /// </summary>
        internal AttributeNames(IDataReader dataReader)
        {
            foreach (var type in Enum.GetValues<Attribute>())
            {
                if (type != Attribute.Unknown)
                    entries.Add(type, dataReader.ReadNullTerminatedString(AmigaExecutable.Encoding));
            }

            entries.Add(Attribute.Unknown, "");
        }

        internal void AddShortNames(IDataReader dataReader)
        {
            foreach (var type in Enum.GetValues<Attribute>())
            {
                if (type != Attribute.Age && type != Attribute.Unknown)
                    shortNames.Add(type, dataReader.ReadNullTerminatedString(AmigaExecutable.Encoding));
            }

            shortNames.Add(Attribute.Age, "");
            shortNames.Add(Attribute.Unknown, "");
        }
    }
}
