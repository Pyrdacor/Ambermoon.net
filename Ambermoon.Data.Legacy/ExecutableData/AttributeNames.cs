using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    /// <summary>
    /// After the <see cref="AbilityNames"/> there are the
    /// attribute names like "Strength", "Dexterity", etc.
    /// </summary>
    public class AttributeNames
    {
        readonly Dictionary<Attribute, string> entries = new Dictionary<Attribute, string>();
        readonly Dictionary<Attribute, string> shortNames = new Dictionary<Attribute, string>();
        public IReadOnlyDictionary<Attribute, string> Entries => entries;
        public IReadOnlyDictionary<Attribute, string> ShortNames => shortNames;

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
