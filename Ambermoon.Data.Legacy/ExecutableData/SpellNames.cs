using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    /// <summary>
    /// After the <see cref="SpellTypeNames"/> there are the
    /// spell names like "Magic Projectile", etc.
    /// 
    /// Unused spells are represented by empty entries.
    /// Note that there are 7 spell types with 30 spells
    /// each. The 5th and 6th spell types are unused and
    /// contain 30 empty entries.
    /// </summary>
    public class SpellNames
    {
        readonly Dictionary<Spell, string> entries = new Dictionary<Spell, string>();
        readonly Dictionary<SpellSchool, List<string>> entriesPerType = new Dictionary<SpellSchool, List<string>>();
        public IReadOnlyDictionary<Spell, string> Entries => entries;
        public IReadOnlyDictionary<SpellSchool, List<string>> EntriesPerType => entriesPerType;

        internal SpellNames(List<string> names)
        {
            if (names.Count != 210)
                throw new AmbermoonException(ExceptionScope.Data, "Invalid number of spell names.");

            for (int i = 0; i < 7; ++i)
                entriesPerType.Add((SpellSchool)i, new List<string>());

            for (int i = 0; i < names.Count; ++i)
            {
                entries.Add((Spell)(i + 1), names[i]);
                entriesPerType[(SpellSchool)(i / 30)].Add(names[i]);
            }
        }

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the spell names just behind the
        /// spell type names.
        /// 
        /// It will be behind the spell names after this.
        /// </summary>
        internal SpellNames(IDataReader dataReader)
        {
            entries.Add(Spell.None, "");
            int spellIndex = 1; // we skip Spell.None as it has no text entry

            foreach (var type in Enum.GetValues<SpellSchool>())
            {
                entriesPerType.Add(type, new List<string>(30));

                for (int i = 0; i < 30; ++i)
                {
                    var name = dataReader.ReadNullTerminatedString(AmigaExecutable.Encoding);
                    entries.Add((Spell)spellIndex++, name);
                    entriesPerType[type].Add(name);
                }
            }
        }
    }
}
