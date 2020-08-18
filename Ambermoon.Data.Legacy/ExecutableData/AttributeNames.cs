using System;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    /// <summary>
    /// After the <see cref="AbilityNames"/> there are the
    /// attribute names like "Strength", "Dexterity", etc.
    /// </summary>
    public class AttributeNames
    {
        public Dictionary<Attribute, string> Entries { get; } = new Dictionary<Attribute, string>();
        public Dictionary<Attribute, string> ShortNames { get; } = new Dictionary<Attribute, string>();

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the attribute names just behind the
        /// ability names.
        /// 
        /// It will be behind the attribute names after this.
        /// </summary>
        internal AttributeNames(IDataReader dataReader)
        {
            foreach (Attribute type in Enum.GetValues(typeof(Attribute)))
            {
                if (type != Attribute.Unknown)
                    Entries.Add(type, dataReader.ReadNullTerminatedString());
            }

            Entries.Add(Attribute.Unknown, "");
        }

        internal void AddShortNames(IDataReader dataReader)
        {
            foreach (Attribute type in Enum.GetValues(typeof(Attribute)))
            {
                if (type != Attribute.Age && type != Attribute.Unknown)
                    ShortNames.Add(type, dataReader.ReadNullTerminatedString());
            }

            ShortNames.Add(Attribute.Age, "");
            ShortNames.Add(Attribute.Unknown, "");
        }
    }
}
