using System;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    public enum Option
    {
        Music,
        FastBattleMode,
        TextJustification,
        FloorTexture3D,
        CeilingTexture3D
    }

    /// <summary>
    /// After the <see cref="AutomapNames"/> there are the
    /// original option names like "Music", "Fast battle mode", etc.
    /// </summary>
    public class OptionNames
    {
        public Dictionary<Option, string> Entries { get; } = new Dictionary<Option, string>();

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the option names just behind the
        /// automap names.
        /// 
        /// It will be behind the option names after this.
        /// </summary>
        internal OptionNames(IDataReader dataReader)
        {
            foreach (var type in Enum.GetValues<Option>())
            {
                Entries.Add(type, dataReader.ReadNullTerminatedString());
            }

            dataReader.AlignToWord();
        }
    }
}
