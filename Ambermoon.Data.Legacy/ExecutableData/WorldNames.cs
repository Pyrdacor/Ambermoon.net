﻿using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    /// <summary>
    /// Directly after the <see cref="FileList"/> there
    /// are the names of the 3 worlds.
    /// 
    /// It starts with 3 longwords which give the absolute
    /// offset inside the second data hunk. Each of the names
    /// is null-terminated.
    /// </summary>
    public class WorldNames
    {
        readonly Dictionary<World, string> entries = new Dictionary<World, string>(3);
        public IReadOnlyDictionary<World, string> Entries => entries;

        internal WorldNames(List<string> names)
        {
            if (names.Count != 3)
                throw new AmbermoonException(ExceptionScope.Data, "Invalid number of world names.");

            for (int i = 0; i < names.Count; ++i)
                entries.Add((World)i, names[i]);
        }

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the world names just behind the
        /// file list.
        /// 
        /// It will be behind the world names after this.
        /// </summary>
        internal WorldNames(IDataReader dataReader)
        {
            var offsets = new uint[3];
            int endOffset = dataReader.Position;

            for (int i = 0; i < 3; ++i)
                offsets[i] = dataReader.ReadDword();

            for (int i = 0; i < 3; ++i)
            {
                dataReader.Position = (int)offsets[i];
                entries.Add((World)i, dataReader.ReadNullTerminatedString(AmigaExecutable.Encoding));

                if (dataReader.Position > endOffset)
                    endOffset = dataReader.Position;
            }

            dataReader.Position = endOffset;
            dataReader.AlignToWord();
        }
    }
}
